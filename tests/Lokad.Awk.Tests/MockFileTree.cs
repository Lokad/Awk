using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public enum AwkFileTreePermission
{
    ReadOnly,
    ReadWrite
}

internal sealed class MockFileTree
{
    private readonly DirectoryNode _root = new(AwkFileTreePermission.ReadWrite);
    private readonly Dictionary<AwkPath, AwkOperationError> _readOpenErrors = [];

    public void AddDirectory(string path) =>
        AddDirectory(path, AwkFileTreePermission.ReadWrite);

    public void AddDirectory(string path, AwkFileTreePermission permission)
    {
        EnsureDirectory(ParsePath(path), permission);
    }

    public void AddFile(string path, string content) =>
        AddFile(path, content, AwkFileTreePermission.ReadWrite);

    public void AddFile(
        string path,
        string content,
        AwkFileTreePermission permission) =>
        AddFile(path, Encoding.UTF8.GetBytes(content), permission);

    public void AddFile(string path, byte[] content) =>
        AddFile(path, content, AwkFileTreePermission.ReadWrite);

    public void AddFile(
        string path,
        byte[] content,
        AwkFileTreePermission permission)
    {
        var filePath = ParsePath(path);
        if (!TryGetParentAndName(filePath, out var parent, out var name))
            throw new InvalidOperationException("MockFileSystem requires a file path.");

        var directory = EnsureDirectory(parent, AwkFileTreePermission.ReadWrite);
        directory.Children[name] = new FileNode(content, permission);
    }

    public void AddReadOpenError(string path, string message)
    {
        _readOpenErrors[ParsePath(path)] = new AwkOperationError(message);
    }

    public bool FileExists(string path)
    {
        return TryResolveNode(ParsePath(path), out var node) && node is FileNode;
    }

    public byte[] ReadFileBytes(string path)
    {
        if (!TryResolveNode(ParsePath(path), out var node) || node is not FileNode file)
            throw new InvalidOperationException("File not found.");

        return file.Content.ToArray();
    }

    public string ReadFile(string path) =>
        Encoding.UTF8.GetString(ReadFileBytes(path));

    public MockReadOpenResult OpenForRead(AwkPath path)
    {
        if (_readOpenErrors.TryGetValue(path, out var readError))
            return new MockReadOpenResult.Failure(readError);

        if (!TryResolveNode(path, out var node))
            return MockReadOpenResult.MissingFile.Instance;

        if (node is DirectoryNode)
            return new MockReadOpenResult.Failure(new AwkOperationError("Is a directory"));

        return new MockReadOpenResult.Opened(((FileNode)node).Content);
    }

    public MockWriteOpenResult OpenForWrite(AwkPath path, AwkFileOpenMode mode)
    {
        if (!TryGetParentAndName(path, out var parentPath, out var name))
            return OperationFailedWrite();

        if (!TryResolveNode(parentPath, out var parentNode) || parentNode is not DirectoryNode parent)
            return OperationFailedWrite();

        var hasExisting = parent.Children.TryGetValue(name, out var existing);
        if (hasExisting && existing is DirectoryNode)
            return OperationFailedWrite();

        if (existing is FileNode existingFile && existingFile.Permission == AwkFileTreePermission.ReadOnly)
            return new MockWriteOpenResult.Failure(new AwkOperationError("Permission denied"));

        if (!hasExisting && parent.Permission == AwkFileTreePermission.ReadOnly)
            return new MockWriteOpenResult.Failure(new AwkOperationError("Permission denied"));

        var initialContent = mode == AwkFileOpenMode.WriteAppend && existing is FileNode file
            ? file.Content
            : [];

        return new MockWriteOpenResult.Opened(initialContent, content =>
        {
            parent.Children[name] = new FileNode(content, AwkFileTreePermission.ReadWrite);
        });
    }

    private DirectoryNode EnsureDirectory(
        AwkPath path,
        AwkFileTreePermission permission)
    {
        var current = _root;
        foreach (var segment in SplitPath(path))
        {
            if (!current.Children.TryGetValue(segment, out var child))
            {
                var directory = new DirectoryNode(permission);
                current.Children.Add(segment, directory);
                current = directory;
                continue;
            }

            if (child is DirectoryNode directoryNode)
            {
                current = directoryNode;
                continue;
            }

            throw new InvalidOperationException("A file already exists at this path.");
        }

        return current;
    }

    private bool TryResolveNode(AwkPath path, [NotNullWhen(true)] out Node? node)
    {
        var current = (Node)_root;
        foreach (var segment in SplitPath(path))
        {
            if (current is not DirectoryNode directory)
            {
                node = null;
                return false;
            }

            if (!directory.Children.TryGetValue(segment, out var child))
            {
                node = null;
                return false;
            }

            current = child;
        }

        node = current;
        return true;
    }

    private static bool TryGetParentAndName(AwkPath path, out AwkPath parent, out AwkPathSegment name)
    {
        parent = AwkPath.Root;
        name = default;
        if (path.Parent is not { } parentPath || !AwkPathSegment.TryCreate(path.FileName, out name))
            return false;

        parent = parentPath;
        return true;
    }

    private static AwkPathSegment[] SplitPath(AwkPath path)
    {
        if (path.IsRoot)
            return [];

        var parts = path.Path[1..].Split('/');
        var segments = new AwkPathSegment[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            segments[index] = new AwkPathSegment(parts[index]);
        }

        return segments;
    }

    private static AwkPath ParsePath(string path)
    {
        if (!TryParsePath(path, out var parsed))
            throw new InvalidOperationException("MockFileSystem requires absolute paths.");

        return parsed;
    }

    private static bool TryParsePath(string path, out AwkPath parsed)
    {
        if (string.IsNullOrWhiteSpace(path) || !AwkPath.IsValid(path))
        {
            parsed = AwkPath.Root;
            return false;
        }

        parsed = new AwkPath(path);
        return true;
    }

    private static MockWriteOpenResult.Failure OperationFailedWrite() =>
        new MockWriteOpenResult.Failure(new AwkOperationError("Operation failed."));

    private abstract class Node;

    private sealed class DirectoryNode(AwkFileTreePermission permission) : Node
    {
        public SortedDictionary<AwkPathSegment, Node> Children { get; } = new(AwkPathSegmentComparer.Instance);

        public AwkFileTreePermission Permission { get; } = permission;
    }

    private sealed class FileNode(byte[] content, AwkFileTreePermission permission) : Node
    {
        public byte[] Content { get; } = content ?? [];

        public AwkFileTreePermission Permission { get; } = permission;
    }

    private sealed class AwkPathSegmentComparer : IComparer<AwkPathSegment>
    {
        public static AwkPathSegmentComparer Instance { get; } = new();

        public int Compare(AwkPathSegment x, AwkPathSegment y) =>
            string.CompareOrdinal(x.Name, y.Name);
    }
}

internal abstract record MockReadOpenResult
{
    public sealed record Opened(byte[] Content) : MockReadOpenResult;

    public sealed record Failure(AwkOperationError Error) : MockReadOpenResult;

    public sealed record MissingFile : MockReadOpenResult
    {
        public static MissingFile Instance { get; } = new();
    }
}

internal abstract record MockWriteOpenResult
{
    public sealed record Opened(ReadOnlyMemory<byte> InitialContent, Action<byte[]> Commit) : MockWriteOpenResult;

    public sealed record Failure(AwkOperationError Error) : MockWriteOpenResult;
}
