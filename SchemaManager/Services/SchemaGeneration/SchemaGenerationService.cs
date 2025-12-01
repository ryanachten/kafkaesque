using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace SchemaManager.Services.SchemaGeneration;

public class SchemaGenerationService : ISchemaGenerationService
{
    private const string SchemasPath = "Schemas";
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchemaGenerationService> _logger;
    private readonly string _outputPath;

    public SchemaGenerationService(
        IConfiguration configuration,
        ILogger<SchemaGenerationService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Output to Common/Generated/ directory
        var solutionDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
        _outputPath = Path.Combine(solutionDir, "Common", "Generated");
    }

    public async Task GenerateCodeFromSchemas(CancellationToken cancellationToken = default)
    {
        var schemasPath = Path.Combine(Directory.GetCurrentDirectory(), SchemasPath);

        if (!Directory.Exists(schemasPath))
        {
            _logger.LogWarning("Schemas directory not found at {Path}. No schemas to generate code from.", schemasPath);
            return;
        }

        var schemaFiles = Directory.GetFiles(schemasPath, "*.avsc");

        if (schemaFiles.Length == 0)
        {
            _logger.LogWarning("No schema files found in {Path}", schemasPath);
            return;
        }

        _logger.LogInformation("Found {Count} schema file(s) to generate code from", schemaFiles.Length);

        // Ensure output directory exists
        Directory.CreateDirectory(_outputPath);

        foreach (var schemaFile in schemaFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GenerateCodeFromSchema(schemaFile, cancellationToken);
        }

        _logger.LogInformation("Code generation complete. Generated code written to {OutputPath}", _outputPath);
    }

    private async Task GenerateCodeFromSchema(string schemaFilePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(schemaFilePath);
        _logger.LogInformation("Generating code from schema: {FileName}", fileName);

        try
        {
            var solutionDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "avrogen",
                Arguments = $"-s \"{schemaFilePath}\" \"{_outputPath}\"",
                WorkingDirectory = solutionDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start avrogen process");
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorOutput = errorBuilder.ToString();
                throw new InvalidOperationException(
                    $"avrogen failed with exit code {process.ExitCode}. Error: {errorOutput}");
            }

            // Convert generated files to PascalCase
            var generatedFiles = Directory.GetFiles(_outputPath, "*.cs", SearchOption.AllDirectories);
            int convertedCount = 0;

            foreach (var generatedFile in generatedFiles)
            {
                if (await ConvertFileToPascalCase(generatedFile))
                {
                    convertedCount++;
                }
            }

            if (convertedCount > 0)
            {
                _logger.LogInformation("Converted {Count} generated file(s) to PascalCase", convertedCount);
            }

            _logger.LogInformation("Successfully generated code from {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate code from schema {FileName}", fileName);
            throw;
        }
    }

    private async Task<bool> ConvertFileToPascalCase(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var originalContent = content;
            var convertedContent = CodeFormatter.ConvertToPascalCase(content);

            if (convertedContent != originalContent)
            {
                await File.WriteAllTextAsync(filePath, convertedContent, Encoding.UTF8);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert file {FilePath} to PascalCase", filePath);
            return false;
        }
    }
}

