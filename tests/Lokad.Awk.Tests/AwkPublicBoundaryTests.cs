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
        var invocation = new AwkCommandInvocation("awk", arguments, environment);

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
            new AwkCommandInvocation("awk", new string[1], []));
        Assert.Throws<ArgumentException>(() =>
            new AwkCommandInvocation("awk", [], [default]));
    }

    [Fact]
    public void AwkPaths_HandleNullTryInputsAndRejectNullRequiredInputs()
    {
        Assert.False(AwkPath.IsValid(null));
        Assert.False(AwkPathSegment.IsValid(null));
        Assert.False(AwkPathSegment.TryCreate(null, out _));
        Assert.Null(AwkPath.Root.TryChild(null));
        Assert.Null(AwkPath.Root.TryChild(default(AwkPathSegment)));
    }

    [Fact]
    public void AwkPaths_UseOneCanonicalResolutionPolicy()
    {
        var current = new AwkPath("/work/input");

        Assert.Equal("/work/output/data", AwkRawPath.Parse("../output/./data").ToAbsolute(current).Path);
        Assert.Equal(
            "/work/output/data",
            AwkPathResolution.ResolveAbsolute("../output/./data", current.Path));
        Assert.Throws<AwkPathException>(() => AwkRawPath.Parse("../output").ToAbsoluteWithin(current));
        Assert.Throws<AwkPathException>(() => AwkPathResolution.ResolveAbsolute("../../..", current.Path));
    }

    [Fact]
    public void AwkOperationError_RequiresTextAndDefaultsToAHostSafeMessage()
    {
        Assert.Throws<ArgumentException>(() => new AwkOperationError(string.Empty));
        Assert.Equal("Operation failed.", default(AwkOperationError).Message);
    }
}
