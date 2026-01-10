using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Razor;

namespace Fazor.Build;

/// <summary>
/// MSBuild task to transpile Razor files to C#
/// </summary>
public class TranspileRazorTask : Task
{
    /// <summary>
    /// Razor files to transpile
    /// </summary>
    [Required]
    public ITaskItem[] RazorFiles { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Root namespace for the project
    /// </summary>
    public string? RootNamespace { get; set; }

    /// <summary>
    /// Output directory for generated files
    /// </summary>
    [Required]
    public string? OutputPath { get; set; }

    /// <summary>
    /// Whether to use folder-based namespacing
    /// </summary>
    public bool UseFolderNamespacing { get; set; } = true;

    /// <summary>
    /// Generated C# files (output)
    /// </summary>
    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            // Early validation to provide better error messages
            if (RazorFiles == null || RazorFiles.Length == 0)
            {
                return true; // Nothing to do
            }

            Log.LogMessage(MessageImportance.High, $"Transpiling {RazorFiles.Length} Razor file(s)...");

            if (string.IsNullOrEmpty(OutputPath))
            {
                Log.LogError("OutputPath is required");
                return false;
            }

            // Ensure output directory exists
            Directory.CreateDirectory(OutputPath);

            var generatedFiles = new List<ITaskItem>();

            foreach (var razorFile in RazorFiles)
            {
                try
                {
                    var inputPath = razorFile.ItemSpec;
                    // Ensure we have an absolute path for #line directives
                    if (!Path.IsPathRooted(inputPath))
                    {
                        inputPath = Path.GetFullPath(inputPath);
                    }
                    var filename = Path.GetFileName(inputPath);
                    var relativePath = razorFile.GetMetadata("RelativeDir") ?? string.Empty;

                    Log.LogMessage(MessageImportance.Normal, $"Processing {inputPath}");

                    // Read the Razor file
                    var razorContent = File.ReadAllText(inputPath);

                    // Generate C# code
                    // Use absolute path so #line directives reference the correct file location
                    var csharpCode = RazorProcessor.GenerateFromSource(
                        razorContent,
                        inputPath,
                        RootNamespace,
                        UseFolderNamespacing
                    );

                    // Create output file path
                    var hash = GetFileHash(inputPath);
                    var outputFileName = $"{Path.GetFileNameWithoutExtension(filename)}.razor.g.cs";
                    var outputFilePath = Path.Combine(OutputPath, outputFileName);

                    // Write generated C# file
                    File.WriteAllText(outputFilePath, csharpCode);

                    var generatedItem = new TaskItem(outputFilePath);
                    generatedItem.SetMetadata("AutoGen", "true");
                    generatedFiles.Add(generatedItem);

                    Log.LogMessage(MessageImportance.Normal, $"Generated {outputFilePath}");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Error transpiling {razorFile.ItemSpec}: {ex.Message}");
                    Log.LogError($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Log.LogError($"Inner exception: {ex.InnerException.Message}");
                        Log.LogError($"Inner stack trace: {ex.InnerException.StackTrace}");
                    }
                    return false;
                }
            }

            GeneratedFiles = generatedFiles.ToArray();

            Log.LogMessage(MessageImportance.High, $"Successfully transpiled {GeneratedFiles.Length} Razor file(s)");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }

    private static string GetFileHash(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 8);
    }
}
