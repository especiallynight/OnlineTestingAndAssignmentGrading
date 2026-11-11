
using System.Diagnostics;
using System.Text;
using CodeExecutionService.DTOs;

namespace CodeExecutionService.Services;

public class DockerCodeRunner : ICodeRunner
{
    private const string DockerImage = "mcr.microsoft.com/dotnet/sdk:10.0";
    private const int BuildTimeoutSeconds = 30;
    private const int RunTimeoutSeconds = 5;
    private const int MaxOutputLength = 65536;

    public async Task<CodeExecutionResultDTO> ExecuteAsync(
        CodeSubmissionDTO submission)
    {
        if (submission.Language != "csharp")
            throw new ArgumentException("Поддерживается только C#.");

        if (string.IsNullOrWhiteSpace(submission.SourceCode))
            throw new ArgumentException("Исходный код не может быть пустым.");

        if (submission.TestCases == null ||
            submission.TestCases.Count == 0 ||
            submission.TestCases.Count > 50)
        {
            throw new ArgumentException(
                "Количество тестов должно быть от 1 до 50.");
        }

        string workDirectory = Path.Combine(
            Path.GetTempPath(),
            "CodeExecution",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(workDirectory);

        string containerName = "code-runner-" +
                               Guid.NewGuid().ToString("N");

        string projectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """;

        bool containerStarted = false;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(workDirectory, "StudentCode.csproj"),
                projectFile);

            await File.WriteAllTextAsync(
                Path.Combine(workDirectory, "Program.cs"),
                submission.SourceCode);

            var containerResult = await RunDockerAsync(
                null,
                BuildTimeoutSeconds,
                "run",
                "-d",
                "--name", containerName,
                "--network", "none",
                "--memory", "768m",
                "--cpus", "1",
                "--pids-limit", "64",
                "--read-only",
                "--tmpfs",
                "/tmp:rw,nosuid,size=128m,uid=10001,gid=10001,mode=1777",
                "--cap-drop", "ALL",
                "--security-opt", "no-new-privileges",
                "--user", "10001:10001",
                "--mount",
                $"type=bind,source={workDirectory},target=/workspace,readonly",
                "--workdir", "/workspace",
                "--env", "DOTNET_CLI_TELEMETRY_OPTOUT=1",
                "--env", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1",
                "--env", "DOTNET_NOLOGO=1",
                "--env", "DOTNET_CLI_HOME=/tmp/dotnet-home",
                "--env", "HOME=/tmp/home",
                "--env", "NUGET_PACKAGES=/tmp/nuget",
                DockerImage,
                "sleep", "infinity");

            if (containerResult.ExitCode != 0)
            {
                return new CodeExecutionResultDTO
                {
                    Status = "InfrastructureError",
                    ErrorMessage = containerResult.Output
                };
            }

            containerStarted = true;

            const string buildCommand =
                "mkdir -p /tmp/empty /tmp/dotnet-home /tmp/home /tmp/nuget " +
                "&& dotnet build StudentCode.csproj " +
                "--configuration Release " +
                "--output /tmp/publish " +
                "-p:RestoreSources=/tmp/empty " +
                "-p:NuGetAudit=false " +
                "-p:BaseIntermediateOutputPath=/tmp/obj/ " +
                "-p:MSBuildProjectExtensionsPath=/tmp/obj/";

            var buildResult = await RunDockerAsync(
                null,
                BuildTimeoutSeconds,
                "exec",
                containerName,
                "sh", "-c", buildCommand);

            if (buildResult.ExitCode == -1)
            {
                return new CodeExecutionResultDTO
                {
                    Status = "CompilationTimeout",
                    ErrorMessage = "Превышено время компиляции."
                };
            }

            if (buildResult.ExitCode != 0)
            {
                return new CodeExecutionResultDTO
                {
                    Status = "CompilationError",
                    ErrorMessage = buildResult.Output
                };
            }

            int passedTests = 0;
            int totalTests = submission.TestCases.Count;

            foreach (var test in submission.TestCases)
            {
                var runResult = await RunDockerAsync(
                    test.Input,
                    RunTimeoutSeconds,
                    "exec",
                    "-i",
                    containerName,
                    "dotnet",
                    "/tmp/publish/StudentCode.dll");

                if (runResult.ExitCode == -1)
                {
                    await RunDockerAsync(
                        null,
                        5,
                        "kill",
                        containerName);

                    return new CodeExecutionResultDTO
                    {
                        Status = "TimeLimitExceeded",
                        PassedTests = passedTests,
                        TotalTests = totalTests,
                        Score = CalculateScore(passedTests, totalTests),
                        ErrorMessage = "Превышено время выполнения программы."
                    };
                }

                if (runResult.ExitCode != 0)
                {
                    return new CodeExecutionResultDTO
                    {
                        Status = "RuntimeError",
                        PassedTests = passedTests,
                        TotalTests = totalTests,
                        Score = CalculateScore(passedTests, totalTests),
                        ErrorMessage = runResult.Error
                    };
                }

                string actualOutput = runResult.Output.Trim();
                string expectedOutput = test.ExpectedOutput.Trim();

                if (actualOutput == expectedOutput)
                {
                    passedTests++;
                }
            }

            return new CodeExecutionResultDTO
            {
                Status = passedTests == totalTests
                    ? "Accepted"
                    : "WrongAnswer",
                PassedTests = passedTests,
                TotalTests = totalTests,
                Score = CalculateScore(passedTests, totalTests)
            };
        }
        finally
        {
            if (containerStarted)
            {
                await RunDockerAsync(
                    null,
                    10,
                    "rm", "-f", containerName);
            }

            if (Directory.Exists(workDirectory))
            {
                Directory.Delete(workDirectory, true);
            }
        }
    }

    private static decimal CalculateScore(int passed, int total)
    {
        return Math.Round(passed * 100m / total, 2);
    }

    private static async Task<(int ExitCode, string Output, string Error)>
        RunDockerAsync(
            string? standardInput,
            int timeoutSeconds,
            params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        Task<string> outputTask =
            ReadLimitedAsync(process.StandardOutput);

        Task<string> errorTask =
            ReadLimitedAsync(process.StandardError);

        if (standardInput != null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token);

            string output = await outputTask;
            string error = await errorTask;

            return (process.ExitCode, output, error);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(true);
                await process.WaitForExitAsync();
            }
            catch
            {
                // Процесс мог завершиться самостоятельно.
            }

            return (-1, string.Empty, "Превышено время выполнения.");
        }
    }

    private static async Task<string> ReadLimitedAsync(
        StreamReader reader)
    {
        var result = new StringBuilder();
        char[] buffer = new char[4096];

        int read;

        while ((read = await reader.ReadAsync(buffer.AsMemory())) > 0)
        {
            int remaining = MaxOutputLength - result.Length;

            if (remaining > 0)
            {
                result.Append(buffer, 0, Math.Min(read, remaining));
            }
        }

        return result.ToString();
    }
}
