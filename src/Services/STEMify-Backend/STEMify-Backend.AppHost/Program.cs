using Microsoft.Extensions.Hosting;
using STEMifyBackend.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var enableDockerEnvVariable = Environment.GetEnvironmentVariable("ENABLE_DOCKER_ENV");
var isRunMode = builder.ExecutionContext.IsRunMode;
var isDevelopment = builder.Environment.IsDevelopment();
var enableDockerCompose = isRunMode && isDevelopment;

// Log for debugging (only in verbose mode or when explicitly enabled)
var debugLogging = Environment.GetEnvironmentVariable("ASPIRE_DEBUG") == "true";
if (debugLogging)
{
    Console.WriteLine($"[AppHost Debug] IsRunMode: {isRunMode}, IsDevelopment: {isDevelopment}");
    Console.WriteLine($"[AppHost Debug] ENABLE_DOCKER_ENV env var: '{enableDockerEnvVariable ?? "null"}'");
    Console.WriteLine($"[AppHost Debug] Initial enableDockerCompose: {enableDockerCompose}");
}

if (!string.IsNullOrWhiteSpace(enableDockerEnvVariable))
{
    enableDockerCompose = string.Equals(enableDockerEnvVariable, "true", StringComparison.OrdinalIgnoreCase);
    if (debugLogging)
    {
        Console.WriteLine($"[AppHost Debug] After checking ENABLE_DOCKER_ENV, enableDockerCompose: {enableDockerCompose}");
    }
}

if (enableDockerCompose)
{
    if (debugLogging)
    {
        Console.WriteLine("[AppHost Debug] Adding Docker Compose environment 'docker-env'");
    }
    builder.AddDockerComposeEnvironment("docker-env");
}
else
{
    if (debugLogging)
    {
        Console.WriteLine("[AppHost Debug] Skipping Docker Compose environment (not in run mode or not development)");
    }
}

builder.AddApplicationServices();

builder.Build().Run();
