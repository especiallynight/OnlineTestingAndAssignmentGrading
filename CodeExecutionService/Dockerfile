FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish CodeExecutionService/CodeExecutionService.csproj \
    -c Release \
    -o /app/publish

# Финальный образ содержит SDK: он нужен, чтобы компилировать код студентов.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final

RUN useradd --create-home --uid 10001 runner

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV HOME=/home/runner
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV CodeRunner=process

EXPOSE 8080
USER runner

ENTRYPOINT ["dotnet", "CodeExecutionService.dll"]
