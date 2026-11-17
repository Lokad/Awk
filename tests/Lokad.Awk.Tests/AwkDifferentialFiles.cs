using System.Text;
using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

// A native invocation owns its working directory. Golden virtual paths map
// below it; command arguments and source text retain their original spelling.
internal sealed class AwkDifferentialFiles : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("lokad-awk-oracle-");

    internal string WorkingDirectory => _directory.FullName;

    internal AwkDifferentialFiles(AwkGoldenCase golden)
    {
        try
        {
            foreach (var directory in golden.InputDirectories)
                Directory.CreateDirectory(Resolve(directory.Path));
            foreach (var file in golden.InputFiles)
            {
                if (file.Permission != AwkFileTreePermission.ReadWrite)
                    throw new ArgumentException("Native fixture permissions need a separate differential profile.");
                var path = Resolve(file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? WorkingDirectory);
                File.WriteAllBytes(path, Encoding.UTF8.GetBytes(file.Content));
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal string Resolve(string virtualPath)
    {
        if (!virtualPath.StartsWith('/') || virtualPath.Contains('\\'))
            throw new ArgumentException("Native fixture paths must be rooted virtual paths with forward slashes.");
        var path = Path.GetFullPath(Path.Combine(WorkingDirectory, virtualPath[1..]));
        if (!path.StartsWith(WorkingDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Native fixture paths must remain inside their working directory.");
        return path;
    }

    internal IReadOnlyDictionary<string, byte[]?> Capture(AwkGoldenCase golden)
    {
        var files = new Dictionary<string, byte[]?>(StringComparer.Ordinal);
        foreach (var file in golden.ExpectedFiles)
        {
            var path = Resolve(file.Path);
            if (!File.Exists(path))
            {
                files.Add(file.Path, null);
                continue;
            }
            using var stream = File.OpenRead(path);
            using var output = new ReferenceOutput(capture: true, 64L << 20);
            var buffer = new byte[16 << 10];
            int count;
            while ((count = stream.Read(buffer)) != 0) output.Append(buffer.AsSpan(0, count));
            files.Add(file.Path, output.Finish().Bytes);
        }
        return files;
    }

    public void Dispose()
    {
        if (Directory.Exists(WorkingDirectory)) Directory.Delete(WorkingDirectory, recursive: true);
    }
}
