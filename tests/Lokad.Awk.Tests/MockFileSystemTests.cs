namespace Lokad.Awk.Tests;

public sealed class MockFileSystemTests
{
    [Theory]
    [InlineData(AwkFileOpenMode.WriteTruncate, false)]
    [InlineData(AwkFileOpenMode.WriteTruncate, true)]
    [InlineData(AwkFileOpenMode.WriteAppend, false)]
    [InlineData(AwkFileOpenMode.WriteAppend, true)]
    public async Task SuccessfulOpenPublishesCreationAndTruncation(AwkFileOpenMode mode, bool existing)
    {
        var host = new MockFileSystem();
        if (existing) host.AddFile("/data", "old");
        var opened = await host.OpenFileAsync(new AwkPathRequest("/data", AwkPath.Root), mode, CancellationToken.None);
        Assert.True(opened.Success);
        try
        {
            Assert.True(host.FileExists("/data"));
            var initial = mode == AwkFileOpenMode.WriteAppend && existing ? "old" : "";
            Assert.Equal(initial, host.ReadFile("/data"));
            await host.AppendAsync(opened.FileDescriptor, "new"u8.ToArray(), CancellationToken.None);
            Assert.Equal(initial, host.ReadFile("/data"));
        }
        finally { await host.CloseDescriptorAsync(opened.FileDescriptor, CancellationToken.None); }
        Assert.Equal(mode == AwkFileOpenMode.WriteAppend && existing ? "oldnew" : "new", host.ReadFile("/data"));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(AwkFileOpenMode.WriteTruncate)]
    [InlineData(AwkFileOpenMode.WriteAppend)]
    public async Task FailedOpenHasNoFileEffects(AwkFileOpenMode mode)
    {
        var host = new MockFileSystem();
        host.AddDirectory("/readonly", AwkFileTreePermission.ReadOnly);
        host.AddDirectory("/directory");
        host.AddFile("/protected", "old", AwkFileTreePermission.ReadOnly);
        foreach (var path in new[] { "/readonly/new", "/directory", "/protected", "/missing/new" })
        {
            var opened = await host.OpenFileAsync(new AwkPathRequest(path, AwkPath.Root), mode, CancellationToken.None);
            Assert.False(opened.Success);
        }
        Assert.False(host.FileExists("/readonly/new"));
        Assert.False(host.FileExists("/missing/new"));
        Assert.Equal("old", host.ReadFile("/protected"));
        Assert.Equal(0, host.OpenFileCount);
    }
}
