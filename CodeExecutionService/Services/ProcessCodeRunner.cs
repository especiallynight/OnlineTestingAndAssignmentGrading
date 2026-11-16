
using System.Diagnostics;
using System.Text;
using CodeExecutionService.DTOs;

namespace CodeExecutionService.Services;

/// <summary>
/// Компилирует и запускает код студента локальным процессом dotnet.
/// Используется в контейнере, где недоступен Docker-демон; изоляцию
/// обеспечивает сам контейнер CodeExecutionService.
/// </summary>
public class ProcessCodeRunner : ICodeRunner
{
    private const int BuildTimeoutSeconds = 60;
    private const int RunTimeoutSeconds = 5;
    private const int MaxOutputLength = 65536;

    private const string ProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
        </Project>
        """;

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

        try
        {
            string emptySource = Path.Combine(workDirectory, "empty-source");
            string publishDirectory = Path.Combine(workDirectory, "publish");
            string objDirectory = Path.Combine(workDirectory, "obj");
            string homeDirectory = Path.Combine(workDirectory, "home");
            string nugetDirectory = Path.Combine(workDirectory, "nuget");

            Directory.CreateDirectory(emptySource);
            Directory.CreateDirectory(homeDirectory);
            Directory.CreateDirectory(nugetDirectory);

            await File.WriteAllTextAsync(
                Path.Combine(workDirectory, "StudentCode.csproj"),
                ProjectFile);

            await File.WriteAllTextAsync(
                Path.Combine(workDirectory, "Program.cs"),
                submission.SourceCode);

            string objPrefix = objDirectory + Path.DirectorySeparatorChar;

            var buildResult = await RunProcessAsync(
                workDirectory,
                homeDirectory,
                nugetDirectory,
                BuildTimeoutSeconds,
                null,
                "build",
                "StudentCode.csproj",
                "--configuration", "Release",
                "--output", publishDirectory,
                "-p:RestoreSources=" + emptySource,
                "-p:NuGetAudit=false",
                "-p:BaseIntermediateOutputPath=" + objPrefix,
                "-p:MSBuildProjectExtensionsPath=" + objPrefix);

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
                    ErrorMessage = JoinOutput(buildResult)
                };
            }

            string assemblyPath = Path.Combine(
                publishDirectory,
                "StudentCode.dll");

            if (!File.Exists(assemblyPath))
            {
                return new CodeExecutionResultDTO
                {
                    Status = "CompilationError",
                    ErrorMessage = "Не удалось собрать программу."
                };
            }

            int passedTests = 0;
            int totalTests = submission.TestCases.Count;

            foreach (var test in submission.TestCases)
            {
                var runResult = await RunProcessAsync(
                    workDirectory,
                    homeDirectory,
                    nugetDirectory,
                    RunTimeoutSeconds,
                    test.Input,
                    assemblyPath);

                if (runResult.ExitCode == -1)
                {
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
                        ErrorMessage = JoinOutput(runResult)
                    };
                }

                if (runResult.Output.Trim() == test.ExpectedOutput.Trim())
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
            if (Directory.Exists(workDirectory))
            {
                try
                {
                    Directory.Delete(workDirectory, true);
                }
                catch
                {
                    // Временную папку удалит следующая очистка.
                }
            }
        }
    }

    private static decimal CalculateScore(int passed, int total)
    {
        return Math.Round(passed * 100m / total, 2);
    }

    private static string JoinOutput(
        (int ExitCode, string Output, string Error) result)
    {
        if (string.IsNullOrWhiteSpace(result.Error))
            return result.Output;

        if (string.IsNullOrWhiteSpace(result.Output))
            return result.Error;

        return result.Output + Environment.NewLine + result.Error;
    }

    private static async Task<(int ExitCode, string Output, string Error)>
        RunProcessAsync(
            string workDirectory,
            string homeDirectory,
            string nugetDirectory,
            int timeoutSeconds,
            string? standardInput,
            params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workDirectory,
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

        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_HOME"] = homeDirectory;
        startInfo.Environment["HOME"] = homeDirectory;
        startInfo.Environment["NUGET_PACKAGES"] = nugetDirectory;

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
