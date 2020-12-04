using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using static System.Console;

if (!Opts.TryParse(args, out var opts))
    return 1;

var sections = Directory.GetDirectories(opts.InputDirectoryPath)
    .Where(x => !Path.GetFileName(x).StartsWith("."))
    .Select(SectionInfo.Load)
    .Where(section => section.Files.Any())
    .ToArray();

SaveIndex(opts.OutputPath, sections);

return 0;

static void SaveIndex(string outputPath, IEnumerable<SectionInfo> sections)
{
    WriteLine($"Generating index at '{outputPath}'");
    outputPath = Path.GetFullPath(outputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

    using var writer = new StreamWriter(outputPath);

    writer.WriteLine("= TIL - Today I Learned");
    writer.WriteLine();
    writer.WriteLine("A collection of small tips and tricks inspired by link:https://simonwillison.net/2020/Apr/20/self-rewriting-readme/[this post in Simon Willison’s Weblog]");
    writer.WriteLine();

    foreach (var section in sections)
    {
        writer.WriteLine($"== {section.Title}");
        writer.WriteLine();

        foreach (var file in section.Files.OrderBy(x => x.ModifiedDate))
        {
            var relativePath = Path.GetRelativePath(Path.GetDirectoryName(outputPath)!, file.Path)
                .Replace("\\", "/");

            writer.WriteLine($"* link:{relativePath}[{file.Title}] - _{file.ModifiedDate.ToString("yyyy-MM-dd")}_");
        }
        writer.WriteLine();
    }
}


record Opts(string InputDirectoryPath, string OutputPath)
{
    public static bool TryParse(string[] args, [NotNullWhen(true)]out Opts? parsed)
    {
        parsed = null;
        if (args.Length != 2)
        {
            Error.WriteLine($"Unexpected number of arguments.");
            Error.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} <INPUTPATH> <OUTPUTPATH>");
            return false;
        }

        var inputPath = args[0];
        var outputPath = args[1];

        if (!Directory.Exists(inputPath))
        {
            Error.WriteLine($"Input path '{inputPath}' does not exist.");
            return false;
        }

        if(String.IsNullOrWhiteSpace(outputPath))
        {
            Error.WriteLine($"Invalid output path '{outputPath}'.");
            return false; 
        }

        parsed = new Opts(inputPath, outputPath);
        return true;
    }
}

record SectionInfo
{
    public string Title { get; init; } = "";

    public IReadOnlyList<ArticleInfo> Files { get; init; } = Array.Empty<ArticleInfo>();

    public static SectionInfo Load(string directoryPath)
    {
        WriteLine($"Loading directory '{directoryPath}'");

        var files = Directory.GetFiles(directoryPath, "*.asc").OrderBy(x => Path.GetFileName(x));

        var directoryInfoPath = Path.Combine(directoryPath, "directory.json");

        var sectionInfo = File.Exists(directoryInfoPath)
            ? JsonConvert.DeserializeObject<SectionInfo>(File.ReadAllText(directoryInfoPath))
            : new SectionInfo();

        if (String.IsNullOrEmpty(sectionInfo.Title))
        {
            sectionInfo = sectionInfo with { Title = Path.GetFileName(directoryPath) };
        }

        var fileInfos = files.Select(ArticleInfo.Load).ToArray();
        sectionInfo = sectionInfo with { Files = fileInfos };

        return sectionInfo;
    }
}

record ArticleInfo(string Path, string Title, DateTime ModifiedDate)
{
    public static ArticleInfo Load(string path)
    {
        WriteLine($"Loading article '{path}'");

        var title = System.IO.Path.GetFileNameWithoutExtension(path);

        var lines = File.ReadAllLines(path);

        var heading1 = lines.Where(x => x.Trim().StartsWith("= "))
            .Select(x => x.Trim().Trim('=').Trim())
            .FirstOrDefault();

        title = heading1 is { Length: > 0 }
            ? heading1
            : title;


        var modifiedDate = Util.GetGitModificationDate(path);
        return new ArticleInfo(Path: path, Title: title, ModifiedDate: modifiedDate);
    }
}

static class Util
{
    static string Exec(string fileName, string args)
    {
        var startInfo = new ProcessStartInfo()
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        var process = Process.Start(startInfo);
        if (process is null)
            throw new Exception($"Failed to start '{fileName}' with arguments '{args}'");

        var stdOutBuilder = new StringBuilder();
        process.OutputDataReceived += (s, e) => stdOutBuilder.AppendLine(e.Data);

        process.BeginOutputReadLine();
        process.WaitForExit();
        process.CancelOutputRead();

        if (process.ExitCode != 0)
            throw new Exception($"'{fileName} {args}' completed with exit code {process.ExitCode}");

        return stdOutBuilder.ToString();
    }

    public static DateTime GetGitModificationDate(string path)
    {
        var output = Exec("git", $"log -1 --pretty=\"format:%cs\" \"{path}\"").Trim();

        var modificationDate = DateTime.ParseExact(output, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return modificationDate;
    }
}
