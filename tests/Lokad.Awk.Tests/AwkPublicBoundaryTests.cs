using System;
using System.Collections.Generic;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPublicBoundaryTests
{
    [Fact]
    public void AwkCommandInvocation_SnapshotsCallerOwnedCollections()
    {
        var arguments = new List<string> { "BEGIN{print 1}" };
        var environment = new List<AwkEnvironmentVariable>
        {
            new("PWD", "/work")
        };
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, environment);

        arguments[0] = "BEGIN{print 2}";
        environment[0] = new AwkEnvironmentVariable("PWD", "/changed");

        Assert.Equal("BEGIN{print 1}", Assert.Single(invocation.Arguments));
        Assert.Equal("/work", Assert.Single(invocation.Environment).Value);
        Assert.Equal("/work", invocation.CurrentDirectory.Path);
    }

    [Fact]
    public void AwkCommandInvocation_RejectsNullCollectionElements()
    {
        Assert.Throws<ArgumentException>(() => new AwkEnvironmentVariable(string.Empty, "value"));
        Assert.Throws<ArgumentNullException>(() =>
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", new string[1], []));
        Assert.Throws<ArgumentException>(() =>
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", [], [default]));
    }

    [Fact]
    public void AwkPaths_HandleNullTryInputsAndRejectNullRequiredInputs()
    {
        Assert.False(AwkPath.IsValid(null));
        Assert.False(AwkPathSegment.IsValid(null));
        Assert.False(AwkPathSegment.TryCreate(null, out _));
        Assert.False(AwkPath.Root.TryChild((string?)null, out var nullChild));
        Assert.Null(nullChild);
        Assert.False(AwkPath.Root.TryChild(default(AwkPathSegment), out var defaultChild));
        Assert.Null(defaultChild);

        Assert.True(AwkPath.Root.TryChild("child", out var child));
        Assert.Equal("/child", Assert.IsType<AwkPath>(child).Path);
    }

    [Fact]
    public void Awk_InternalValueObjectsExposeNonNullDefaultViews()
    {
        Assert.Empty(default(AwkAction).Statements);
        Assert.Equal(string.Empty, default(AwkSourceUnit).Text);
        Assert.Empty(default(AwkSourceUnit).Segments);
        Assert.Equal(string.Empty, default(AwkSourceName).Text);
        Assert.Equal(string.Empty, default(AwkSourceIdentity).Text);
        Assert.Equal(string.Empty, default(AwkRegexPattern).Text);
        Assert.Equal(string.Empty, default(AwkRegexLiteralText).Text);
        Assert.Equal(string.Empty, default(AwkErrno).Text);
    }

    [Fact]
    public void AwkRawPaths_CanonicalizeAndEnforceBoundaries()
    {
        var current = new AwkPath("/work/input");

        Assert.Equal("/work/output/data", AwkRawPath.Parse("../output/./data").ToAbsolute(current).Path);
        Assert.Throws<AwkPathException>(() => AwkRawPath.Parse("../output").ToAbsoluteWithin(current));
        Assert.Throws<AwkPathException>(() => AwkRawPath.Parse("../../..").ToAbsolute(current));
    }

    [Fact]
    public void AwkPaths_ValidateWithoutAllocatingSegments()
    {
        const string canonical = "/work/input/data.csv";
        const string raw = "../input/data.csv";
        _ = AwkPath.IsValid(canonical);
        _ = AwkRawPath.IsValid(raw);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            if (!AwkPath.IsValid(canonical) || !AwkRawPath.IsValid(raw))
                throw new InvalidOperationException("A valid path was rejected.");
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1_024, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkOperationError_RequiresTextAndDefaultsToAHostSafeMessage()
    {
        Assert.Throws<ArgumentException>(() => new AwkOperationError(string.Empty));
        Assert.Equal("Operation failed.", default(AwkOperationError).Message);
    }

    [Fact]
    public void AwkFileDescriptor_RejectsNegativeIdentifiers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AwkFileDescriptor(-1));
        Assert.Equal(0, default(AwkFileDescriptor).Id);
    }
}
