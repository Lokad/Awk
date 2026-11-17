using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkDifferentialFilesTests
{
    [Fact]
    public void FixturesPreserveBytesAndOwnTheirDirectoryThroughCapture()
    {
        var golden = AwkGoldenCaseBuilder.Create("native-file-fixture", [],
            [new AwkGoldenFile("/nested/input.txt", "caf\u00e9\0\n")])
            .WithInputDirectories(["/empty"])
            .WithExpectedFiles([new AwkGoldenFile("/nested/output.txt", "")]).Build();
        string directory;
        using (var fixture = new AwkDifferentialFiles(golden))
        {
            directory = fixture.WorkingDirectory;
            Assert.Equal(Encoding.UTF8.GetBytes("caf\u00e9\0\n"),
                File.ReadAllBytes(Path.Combine(directory, "nested/input.txt")));
            Assert.True(Directory.Exists(Path.Combine(directory, "empty")));
            Assert.Null(fixture.Capture(golden)["/nested/output.txt"]);
            var bytes = Encoding.UTF8.GetBytes("output\0\u00e9\n");
            File.WriteAllBytes(Path.Combine(directory, "nested/output.txt"), bytes);
            Assert.Equal(bytes, fixture.Capture(golden)["/nested/output.txt"]);
        }
        Assert.False(Directory.Exists(directory));
    }

    [Theory]
    [InlineData("/../outside")]
    [InlineData("/nested\\outside")]
    public void FixturePathsCannotWriteOutsideTheirOwnedDirectory(string path)
    {
        var golden = AwkGoldenCaseBuilder.Create("native-empty-fixture", [], []).Build();
        using var fixture = new AwkDifferentialFiles(golden);
        Assert.Throws<ArgumentException>(() => fixture.Resolve(path));
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.WorkingDirectory));
    }
}
