//
// Managed ERE matcher based on MinRX's structured NFA algorithm.
// Copyright (C) 2023, 2024, 2025, 2026 Michael J. Haertel.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS “AS IS” AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
// OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
// OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
// SUCH DAMAGE.
//

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal sealed class AwkPosixRegex
{
    private enum Kind { Literal, Dot, Class, Assertion, Empty, Sequence, Alternate, Group, Repeat }
    private enum Op { Literal, Dot, Class, Assertion, Jump, Fork, Join, Open, Close, Loop, Next, Skip, SkipEnd, Accept }
    private sealed record Tree(Kind Kind, string Text, List<Tree> Children, int Group = 0,
        int LastGroup = 0, int Minimum = 1, long Maximum = 1);
    private sealed class Node(Op op, int depth)
    {
        internal Op Op = op;
        internal int Depth = depth;
        internal int Next;
        internal int Other;
        internal int Group;
        internal int LastGroup;
        internal int Invert;
        internal string Text = "";
        internal Regex? Predicate;
        internal int[] Branches = [];
    }
    private sealed class Parser(string pattern)
    {
        private static readonly string[] WordAssertions = [@"\b(?=\w)", @"(?<=\w)\b"];
        private int _position;
        internal int Groups;

        private sealed class Frame(int group)
        {
            internal int Group = group;
            internal List<Tree> Terms = [];
            internal List<Tree> Alternatives = [];
            internal void NextAlternative()
            {
                Alternatives.Add(new(Kind.Sequence, "", Terms));
                Terms = [];
            }
            internal Tree Finish()
            {
                NextAlternative();
                return Alternatives.Count == 1 ? Alternatives[0] : new(Kind.Alternate, "", Alternatives);
            }
        }

        internal Tree Parse(Action checkpoint)
        {
            var parents = new Stack<Frame>();
            var frame = new Frame(0);
            while (_position < pattern.Length)
            {
                checkpoint();
                var knownAssertion = pattern.AsSpan(_position).StartsWith(@"(?<=\w)\b", StringComparison.Ordinal);
                if (pattern[_position] == '(' && !knownAssertion)
                {
                    _position++;
                    parents.Push(frame);
                    frame = new Frame(++Groups);
                }
                else if (pattern[_position] == ')')
                {
                    _position++;
                    if (parents.Count == 0) throw new FormatException();
                    var group = new Tree(Kind.Group, "", [frame.Finish()], frame.Group, Groups);
                    frame = parents.Pop();
                    frame.Terms.Add(Quantify(group));
                }
                else if (pattern[_position] == '|')
                {
                    _position++;
                    frame.NextAlternative();
                }
                else frame.Terms.Add(Quantify(Atom()));
            }
            if (parents.Count != 0) throw new FormatException();
            return frame.Finish();
        }

        private Tree Atom()
        {
            foreach (var assertion in WordAssertions)
                if (pattern.AsSpan(_position).StartsWith(assertion, StringComparison.Ordinal))
                {
                    _position += assertion.Length;
                    return new(Kind.Assertion, assertion, []);
                }
            var start = _position++;
            var ch = pattern[start];
            if (ch == '[')
            {
                if (_position < pattern.Length && pattern[_position] == '^') _position++;
                if (_position < pattern.Length && pattern[_position] == ']') _position++;
                while (_position < pattern.Length && pattern[_position] != ']')
                {
                    if (pattern[_position++] == '\\') _position++;
                }
                if (_position == pattern.Length) throw new FormatException(pattern);
                _position++;
                return new(Kind.Class, pattern[start.._position], []);
            }
            if (ch == '\\')
            {
                if (_position == pattern.Length) throw new FormatException(pattern);
                ch = pattern[_position++];
                if (ch is 'A' or 'z' or 'b' or 'B') return new(Kind.Assertion, pattern[start.._position], []);
                if (ch is 'p' or 'P')
                {
                    var close = pattern.IndexOf('}', _position);
                    if (close < 0) throw new FormatException(pattern);
                    _position = close + 1;
                    return new(Kind.Class, pattern[start.._position], []);
                }
                if (ch is 'w' or 'W' or 's' or 'S' or 'd' or 'D')
                    return new(Kind.Class, pattern[start.._position], []);
                if (ch == 'u')
                {
                    _position += 4;
                    var literal = Regex.Unescape(pattern[start.._position]);
                    return new(Kind.Literal, literal, []);
                }
                if (ch is 'n' or 'r' or 't' or 'v' or 'a' or 'f')
                    return new(Kind.Literal, Regex.Unescape(pattern[start.._position]), []);
            }
            else if (ch is '^' or '$')
                return new(Kind.Assertion, ch == '^' ? @"\A" : @"\z", []);
            else if (ch == '.') return new(Kind.Dot, "", []);
            if (char.IsHighSurrogate(ch) && _position < pattern.Length && char.IsLowSurrogate(pattern[_position]))
                return new(Kind.Literal, new string([ch, pattern[_position++]]), []);
            return new(Kind.Literal, ch.ToString(), []);
        }

        private Tree Quantify(Tree body)
        {
            if (_position == pattern.Length) return body;
            var ch = pattern[_position];
            int minimum;
            long maximum;
            if (ch is '*' or '+' or '?')
            {
                _position++;
                minimum = ch == '+' ? 1 : 0;
                maximum = ch == '?' ? 1 : long.MaxValue;
            }
            else if (ch == '{')
            {
                var close = pattern.IndexOf('}', _position);
                if (close < 0) return body;
                var interval = pattern[(_position + 1)..close].Split(',');
                if (!int.TryParse(interval[0], out minimum)) return body;
                maximum = interval.Length == 1 ? minimum : interval[1].Length == 0 ? long.MaxValue : int.Parse(interval[1]);
                _position = close + 1;
            }
            else return body;
            return new(Kind.Repeat, "", [body], Minimum: minimum, Maximum: maximum);
        }
    }

    private readonly List<Node> _nodes = [];
    private readonly int _groups;
    private readonly bool _ignoreCase;
    private readonly Stack<Action> _compileWork = [];
    private readonly Dictionary<string, Regex> _predicates = new(StringComparer.Ordinal);
    private readonly int _maximumDepth;

    internal int GroupCount => _groups;
    internal long CacheBytes { get; }

    private sealed class CompilationContext(AwkMemoryAccounting governor,
        AwkMemoryLease results, Action work, Action prepare)
    {
        public AwkMemoryAccounting Governor { get; } = governor;
        public AwkMemoryLease Results { get; } = results;
        public Action Work { get; } = work;
        public Action Prepare { get; } = prepare;
    }

    private AwkPosixRegex(Tree tree, int groups, bool ignoreCase, string pattern, CompilationContext context)
    {
        _ignoreCase = ignoreCase;
        _groups = groups + 1;
        Schedule(tree, 0, context);
        while (_compileWork.TryPop(out var action))
        {
            context.Work();
            action();
        }
        Add(Op.Accept, 0);
        _maximumDepth = _nodes.Max(node => node.Depth);
        var predicateBytes = 0L;
        foreach (var expression in _predicates.Keys)
            predicateBytes = AwkRuntimeMemory.CheckedAdd(predicateBytes,
                AwkRegexResourceFacts.EstimateCacheBytes(expression, context.Prepare));
        CacheBytes = AwkRuntimeMemory.CheckedAdd(
            AwkRegexResourceFacts.EstimateCacheBytes(pattern, context.Prepare),
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedMultiply(_nodes.Count, 224), predicateBytes));
    }

    internal static bool TryCreate(string pattern, AwkRegexCaseSensitivity sensitivity,
        AwkMemoryAccounting governor, Action recordCompilation, Action checkCancellation,
        [NotNullWhen(true)] out AwkPosixRegex? result, AwkMemoryLease? resultLease = null)
    {
        result = null;
        if (!IsEligible(pattern, checkCancellation)) return false;
        checkCancellation();
        var work = 0;
        void Checkpoint()
        {
            if ((++work & 1023) != 0) return;
            checkCancellation();
        }
        using var ownedResults = resultLease is null ? governor.CreateTransientLease() : AwkMemoryLease.Disabled;
        var retained = resultLease ?? ownedResults;
        var parsingBytes = AwkRuntimeMemory.CheckedAdd(256, AwkRuntimeMemory.CheckedMultiply(pattern.Length, 128));
        using var parsingMemory = governor.ReserveTransient(parsingBytes);
        var parser = new Parser(pattern);
        Tree tree;
        try { tree = parser.Parse(Checkpoint); }
        catch (FormatException) { return false; }
        var nodes = CountNodes(tree, Checkpoint);
        CheckedCount(nodes);
        recordCompilation();
        var compilationBytes = AwkRuntimeMemory.CheckedAdd(256, AwkRuntimeMemory.CheckedMultiply(nodes, 224));
        using var compilationMemory = governor.ReserveTransient(compilationBytes);
        result = new(tree, parser.Groups, sensitivity == AwkRegexCaseSensitivity.IgnoreCase, pattern,
            new(governor, retained, Checkpoint, checkCancellation));
        checkCancellation();
        parsingMemory.MoveTo(retained, parsingBytes);
        compilationMemory.MoveTo(retained, compilationBytes);
        return true;
    }

    private static bool IsEligible(string pattern, Action checkpoint)
    {
        long progress = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            AwkRegexConstruction.CheckProgress(i, ref progress, checkpoint);
            if (pattern.AsSpan(i).StartsWith(@"\b(?=\w)", StringComparison.Ordinal)) { i += 7; continue; }
            if (pattern.AsSpan(i).StartsWith(@"(?<=\w)\b", StringComparison.Ordinal)) { i += 8; continue; }
            if (pattern[i] == '[')
            {
                var end = AwkRegexSelection.BracketEnd(pattern, i, checkpoint);
                if (end < 0 || pattern.AsSpan(i, end - i).Contains("-[", StringComparison.Ordinal)) return false;
                i = end;
            }
            else if (pattern[i] == '\\')
            {
                if (++i == pattern.Length) return false;
                if (pattern[i] is 'G' or 'k' || char.IsAsciiDigit(pattern[i])) return false;
                if (char.IsAsciiLetter(pattern[i]) && pattern[i] is not
                    ('A' or 'z' or 'b' or 'B' or 'w' or 'W' or 's' or 'S' or 'd' or 'D' or
                     'u' or 'p' or 'P' or 'n' or 'r' or 't' or 'v' or 'a' or 'f')) return false;
                if (pattern[i] == 'u')
                {
                    if (i + 4 >= pattern.Length) return false;
                    var literal = Regex.Unescape(pattern[(i - 1)..(i + 5)]);
                    if (literal.Length != 1 || char.IsSurrogate(literal[0])) return false;
                    i += 4;
                }
                else if (pattern[i] is 'p' or 'P')
                {
                    i = AwkRegexConstruction.IndexOf(pattern, '}', i + 1, checkpoint);
                    if (i < 0) return false;
                }
            }
            else if (pattern[i] == '(' && i + 1 < pattern.Length && pattern[i + 1] == '?') return false;
            else if (pattern[i] is '*' or '+' or '?' or '}' && i + 1 < pattern.Length && pattern[i + 1] == '?') return false;
        }
        return true;
    }

    private static long CountNodes(Tree root, Action checkpoint)
    {
        var work = new Stack<(Tree Tree, bool Visited)>();
        var counts = new Dictionary<Tree, long>();
        work.Push((root, false));
        while (work.TryPop(out var task))
        {
            checkpoint();
            if (!task.Visited)
            {
                work.Push((task.Tree, true));
                foreach (var child in task.Tree.Children) work.Push((child, false));
                continue;
            }
            var tree = task.Tree;
            var children = tree.Children.Aggregate(0L, (total, child) => AwkRuntimeMemory.CheckedAdd(total, counts[child]));
            counts[tree] = tree.Kind switch
            {
                Kind.Sequence => children,
                Kind.Alternate => AwkRuntimeMemory.CheckedAdd(children, tree.Children.Count + 2),
                Kind.Group => AwkRuntimeMemory.CheckedAdd(children, 2),
                Kind.Repeat when tree.Maximum == 0 => 0,
                Kind.Repeat when tree.Maximum == long.MaxValue => AwkRuntimeMemory.CheckedAdd(
                    AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedMultiply(children, Math.Max(1, tree.Minimum)),
                        tree.Minimum > 1 ? children : 0), 2),
                Kind.Repeat => AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedMultiply(children, (int)tree.Maximum),
                    AwkRuntimeMemory.CheckedMultiply((int)tree.Maximum - Math.Max(1, tree.Minimum), 2) + (tree.Minimum == 0 ? 2 : 0)),
                _ => 1
            };
        }
        return AwkRuntimeMemory.CheckedAdd(counts[root], 1);
    }

    private int Add(Op op, int depth)
    {
        var index = _nodes.Count;
        _nodes.Add(new(op, depth) { Next = index + 1 });
        return index;
    }

    private void Schedule(Tree tree, int depth, CompilationContext context) => _compileWork.Push(() => Emit(tree, depth, context));

    private void Emit(Tree tree, int depth, CompilationContext context)
    {
        switch (tree.Kind)
        {
            case Kind.Sequence:
                for (var i = tree.Children.Count - 1; i >= 0; i--) Schedule(tree.Children[i], depth, context);
                break;
            case Kind.Alternate:
                var fork = Add(Op.Fork, depth);
                var branches = new int[tree.Children.Count];
                var jumps = new int[tree.Children.Count];
                _compileWork.Push(() =>
                {
                    var join = Add(Op.Join, depth + 1);
                    foreach (var jump in jumps) _nodes[jump].Next = join;
                    _nodes[fork].Branches = branches;
                });
                for (var i = tree.Children.Count - 1; i >= 0; i--)
                {
                    var branch = i;
                    _compileWork.Push(() => jumps[branch] = Add(Op.Jump, depth + 1));
                    Schedule(tree.Children[branch], depth + 1, context);
                    _compileWork.Push(() => branches[branch] = _nodes.Count);
                }
                break;
            case Kind.Group:
                var open = Add(Op.Open, depth);
                _nodes[open].Group = tree.Group;
                _nodes[open].LastGroup = tree.LastGroup;
                _compileWork.Push(() => _nodes[Add(Op.Close, depth + 1)].Group = tree.Group);
                Schedule(tree.Children[0], depth + 1, context);
                break;
            case Kind.Repeat:
                Repeat(tree.Children[0], tree.Minimum, tree.Maximum, depth, context);
                break;
            default:
                var op = tree.Kind switch
                {
                    Kind.Literal => Op.Literal,
                    Kind.Dot => Op.Dot,
                    Kind.Class => Op.Class,
                    Kind.Assertion => Op.Assertion,
                    _ => Op.Jump
                };
                var atom = Add(op, depth);
                _nodes[atom].Text = tree.Text;
                if (op == Op.Class || op == Op.Literal && _ignoreCase || op == Op.Assertion)
                {
                    context.Prepare();
                    using var preparedText = context.Governor.CreateTransientLease();
                    var literalOrPattern = op == Op.Literal
                        ? AwkRegexConstruction.EscapeLiteral(tree.Text, context.Prepare, context.Governor, preparedText)
                        : tree.Text;
                    var scalar = AwkRegexScalarPattern.Translate(literalOrPattern, context.Prepare,
                        context.Governor, preparedText);
                    var prefix = op == Op.Assertion ? @"\G(?:" : @"\A(?:";
                    var suffix = op == Op.Assertion ? ")" : @")\z";
                    var expressionBytes = AwkRuntimeMemory.EstimateUtf16StringBytes(
                        AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedAdd(prefix.Length, scalar.Length), suffix.Length));
                    preparedText.ReserveAdditional(expressionBytes);
                    var expression = prefix + scalar + suffix;
                    context.Prepare();
                    if (!_predicates.TryGetValue(expression, out var predicate))
                    {
                        predicate = new Regex(expression, AwkRegex.ResolveOptions(_ignoreCase
                            ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive), AwkRegex.MatchTimeout);
                        context.Prepare();
                        _predicates.Add(expression, predicate);
                        preparedText.MoveTo(context.Results, expressionBytes);
                    }
                    _nodes[atom].Predicate = predicate;
                }
                break;
        }
    }

    private void Repeat(Tree body, int minimum, long maximum, int depth, CompilationContext context)
    {
        if (maximum == 0) return;
        if (maximum == long.MaxValue && minimum <= 1) { Loop(body, minimum == 0, depth, context); return; }
        if (maximum == 1 && minimum == 0) { Optional(() => Schedule(body, depth + 2, context), depth, 0); return; }
        if (minimum == 0) { Optional(() => _compileWork.Push(() => Repeat(body, 1, maximum, depth + 2, context)), depth, 0); return; }
        void Iteration(int i)
        {
            if (i >= maximum && maximum != long.MaxValue) return;
            if (i < minimum)
            {
                _compileWork.Push(() => Iteration(i + 1));
                Schedule(body, depth, context);
            }
            else if (maximum == long.MaxValue) Loop(body, true, depth, context);
            else
            {
                _compileWork.Push(() => Iteration(i + 1));
                Optional(() => Schedule(body, depth + 2, context), depth, 1);
            }
        }
        _compileWork.Push(() => Iteration(0));
    }

    private void Loop(Tree body, bool optional, int depth, CompilationContext context)
    {
        var begin = Add(Op.Loop, depth);
        _compileWork.Push(() =>
        {
            var end = Add(Op.Next, depth + 3);
            _nodes[begin].Other = optional ? end : -1;
            _nodes[end].Other = begin + 1;
        });
        Schedule(body, depth + 3, context);
    }

    private void Optional(Action schedule, int depth, int invert)
    {
        var skip = Add(Op.Skip, depth);
        _nodes[skip].Invert = invert;
        _compileWork.Push(() => _nodes[skip].Other = Add(Op.SkipEnd, depth + 2));
        schedule();
    }

    internal AwkPosixMatch Match(string text, int startAt, int groupCount,
        AwkMemoryAccounting governor, Action checkCancellation)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(governor);
        ArgumentNullException.ThrowIfNull(checkCancellation);
        AwkRegexScalarBoundary.ThrowIfSplit(text, startAt, 0);
        checkCancellation();
        var count = _nodes.Count;
        var depth = _maximumDepth;
        var capturesAt = 1 + depth;
        var rowWidth = CheckedCount(AwkRuntimeMemory.CheckedAdd(capturesAt, AwkRuntimeMemory.CheckedMultiply(groupCount, 2)));
        var rowCount = CheckedCount(AwkRuntimeMemory.CheckedAdd(count, 2));
        var storageCells = CheckedCount(AwkRuntimeMemory.CheckedMultiply(rowCount, rowWidth));
        var frontierCells = CheckedCount(AwkRuntimeMemory.CheckedMultiply(count, rowWidth));
        var bytes = AwkRuntimeMemory.CheckedAdd(512, AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedMultiply((long)storageCells + frontierCells, sizeof(int)),
            AwkRuntimeMemory.CheckedMultiply(count, 19)));
        var memory = governor.ReserveTransient(bytes);
        try
        {
            var storage = new int[storageCells];
            var frontier = new int[frontierCells];
            var frontierTargets = new int[count];
            var characterNodes = new int[count];
            var seen = new bool[count];
            var queued = new bool[count];
            var characterSeen = new bool[count];
            var queue = new PriorityQueue<int, int>(count);
            var bestRow = count * rowWidth;
            var initialRow = (count + 1) * rowWidth;
            Array.Fill(storage, -1, initialRow + capturesAt, groupCount * 2);
            var best = false;
            var bestEnd = 0;
            var frontierCount = 0;
            var work = 0;
            for (var offset = startAt; offset <= text.Length;)
            {
                Array.Clear(seen);
                Array.Clear(queued);
                Array.Clear(characterSeen);
                queue.Clear();
                var characterCount = 0;
                var width = offset < text.Length ? Rune.GetRuneAt(text, offset).Utf16SequenceLength : 0;
                void Check()
                {
                    if ((++work & 1023) != 0) return;
                    checkCancellation();
                }
                void Offer(int target, int[] source, int sourceRow, int baseDepth,
                    int additions = 0, int one = 0, int two = 0, int three = 0,
                    int clearFrom = -1, int clearThrough = -1, int writeGroup = 0, int writeStart = 0)
                {
                    Check();
                    var destination = target * rowWidth;
                    if (best && source[sourceRow] > storage[bestRow]) return;
                    if (baseDepth + additions != _nodes[target].Depth) throw new InvalidOperationException("Flat stack mismatch");
                    if (seen[target])
                    {
                        var comparison = storage[destination].CompareTo(source[sourceRow]);
                        for (var i = 0; comparison == 0 && i < baseDepth + additions; i++)
                        {
                            var value = i < baseDepth ? source[sourceRow + 1 + i] :
                                i == baseDepth ? one : i == baseDepth + 1 ? two : three;
                            // Unsigned ranks put participation above absence and prefer fewer repeat iterations.
                            comparison = unchecked((uint)value).CompareTo(unchecked((uint)storage[destination + 1 + i]));
                        }
                        if (comparison <= 0) return;
                    }
                    source.AsSpan(sourceRow, rowWidth).CopyTo(storage.AsSpan(destination, rowWidth));
                    if (additions > 0) storage[destination + 1 + baseDepth] = one;
                    if (additions > 1) storage[destination + 2 + baseDepth] = two;
                    if (additions > 2) storage[destination + 3 + baseDepth] = three;
                    clearThrough = Math.Min(clearThrough, groupCount - 1);
                    if (clearFrom >= 0 && clearThrough >= clearFrom)
                        Array.Fill(storage, -1, destination + capturesAt + clearFrom * 2, (clearThrough - clearFrom + 1) * 2);
                    if (writeGroup > 0 && writeGroup < groupCount)
                    {
                        storage[destination + capturesAt + writeGroup * 2] = writeStart;
                        storage[destination + capturesAt + writeGroup * 2 + 1] = offset;
                    }
                    seen[target] = true;
                    if (queued[target]) return;
                    queued[target] = true;
                    queue.Enqueue(target, target);
                }
                for (var i = 0; i < frontierCount; i++)
                    Offer(frontierTargets[i], frontier, i * rowWidth, _nodes[frontierTargets[i]].Depth);
                if (!best)
                {
                    storage[initialRow] = offset;
                    Offer(0, storage, initialRow, 0);
                }
                while (queue.TryDequeue(out var index, out _))
                {
                    Check();
                    queued[index] = false;
                    var row = index * rowWidth;
                    var node = _nodes[index];
                    switch (node.Op)
                    {
                        case Op.Literal:
                        case Op.Dot:
                        case Op.Class:
                            if (!characterSeen[index] && offset < text.Length && Matches(node, text.AsSpan(offset, width)))
                            {
                                characterSeen[index] = true;
                                characterNodes[characterCount++] = index;
                            }
                            break;
                        case Op.Accept:
                            if (!best || storage[row] < storage[bestRow] || storage[row] == storage[bestRow] && offset > bestEnd)
                            {
                                storage.AsSpan(row, rowWidth).CopyTo(storage.AsSpan(bestRow, rowWidth));
                                best = true; bestEnd = offset;
                            }
                            break;
                        case Op.Fork:
                            for (var branch = 0; branch < node.Branches.Length; branch++)
                                Offer(node.Branches[branch], storage, row, node.Depth, 1, -branch - 1);
                            break;
                        case Op.Join:
                            Offer(node.Next, storage, row, node.Depth - 1);
                            break;
                        case Op.Jump:
                            Offer(node.Next, storage, row, node.Depth);
                            break;
                        case Op.Open:
                            // A new parent iteration clears descendants before its chosen path repopulates them.
                            Offer(node.Next, storage, row, node.Depth, 1, offset,
                                clearFrom: node.Group + 1, clearThrough: node.LastGroup);
                            break;
                        case Op.Close:
                            Offer(node.Next, storage, row, node.Depth - 1,
                                writeGroup: node.Group, writeStart: storage[row + node.Depth]);
                            break;
                        case Op.Loop:
                            Offer(node.Next, storage, row, node.Depth, 3, offset, -1, offset);
                            if (node.Other >= 0) Offer(node.Other, storage, row, node.Depth, 3, offset, 0, offset);
                            break;
                        case Op.Next:
                            Offer(node.Next, storage, row, node.Depth - 3);
                            if (offset > storage[row + node.Depth])
                                Offer(node.Other, storage, row, node.Depth - 3, 3,
                                    storage[row + node.Depth - 2], storage[row + node.Depth - 1] - 1, offset);
                            break;
                        case Op.Skip:
                            Offer(node.Next, storage, row, node.Depth, 2, offset, 1 ^ node.Invert);
                            Offer(node.Other, storage, row, node.Depth, 2, offset, node.Invert);
                            break;
                        case Op.SkipEnd:
                            Offer(node.Next, storage, row, node.Depth - 2);
                            break;
                        case Op.Assertion:
                            if ((node.Predicate ?? throw new InvalidOperationException()).IsMatch(text, offset))
                                Offer(node.Next, storage, row, node.Depth);
                            break;
                    }
                }
                frontierCount = characterCount;
                for (var i = 0; i < characterCount; i++)
                {
                    var index = characterNodes[i];
                    storage.AsSpan(index * rowWidth, rowWidth).CopyTo(frontier.AsSpan(i * rowWidth, rowWidth));
                    frontierTargets[i] = _nodes[index].Next;
                }
                if (best && frontierCount == 0 || width == 0) break;
                offset += width;
            }
            var bestStart = best ? storage[bestRow] : 0;
            if (best && groupCount > 0)
            {
                storage[bestRow + capturesAt] = bestStart;
                storage[bestRow + capturesAt + 1] = bestEnd;
            }
            return new AwkPosixMatch(best, bestStart, best ? bestEnd - bestStart : 0,
                storage, bestRow + capturesAt, groupCount, memory);
        }
        catch
        {
            memory.Dispose();
            throw;
        }
    }

    private static int CheckedCount(long value) => value <= Array.MaxLength
        ? (int)value : throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

    private static bool Matches(Node node, ReadOnlySpan<char> value)
    {
        if (node.Op == Op.Dot) return true;
        if (node.Predicate is not null) return node.Predicate.IsMatch(value);
        return value.SequenceEqual(node.Text);
    }
}
