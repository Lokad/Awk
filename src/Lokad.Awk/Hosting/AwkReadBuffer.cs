namespace Lokad.Awk;

/// <summary>Owns one immutable host read borrow until its consumed prefix is committed on disposal.</summary>
/// <remarks>
/// Use the borrow serially. Dispose it before another operation on the same descriptor.
/// Disposal is not cancellable and invokes the host's completion callback exactly once.
/// The host keeps the content immutable and funded until then, and retains its unconsumed suffix.
/// The completion callback commits metadata and releases storage; it must not throw or run cancellable I/O.
/// </remarks>
public sealed class AwkReadBuffer : IDisposable
{
    private ReadOnlyMemory<byte> _content;
    private Action<int>? _complete;
    private int _consumed;

    private AwkReadBuffer(ReadOnlyMemory<byte> content, AwkReadCompletion completion,
        AwkOperationError error, Action<int> complete)
    {
        ArgumentNullException.ThrowIfNull(complete);
        _content = content;
        _complete = complete;
        Completion = completion;
        Error = error;
    }

    /// <summary>Gets borrowed bytes. Views must not be retained after disposal.</summary>
    public ReadOnlyMemory<byte> Content
    {
        get
        {
            ObjectDisposedException.ThrowIf(_complete is null, this);
            return _content;
        }
    }

    /// <summary>Gets whether more data, EOF, or a terminal failure follows the supplied bytes.</summary>
    public AwkReadCompletion Completion { get; }

    /// <summary>Gets the structured cause of a terminal failure.</summary>
    public AwkOperationError Error { get; }

    /// <summary>Gets whether this read ended without a transport failure.</summary>
    public bool Success => Completion != AwkReadCompletion.Failure;

    /// <summary>Gets whether a terminal failure follows the usable prefix.</summary>
    public bool Failed => Completion == AwkReadCompletion.Failure;

    /// <summary>Gets whether EOF follows the supplied bytes.</summary>
    public bool ReachedEof => Completion == AwkReadCompletion.EndOfFile;

    /// <summary>Creates an empty EOF borrow without owned host storage.</summary>
    public static AwkReadBuffer EndOfFile =>
        new(default, AwkReadCompletion.EndOfFile, default, static _ => { });

    /// <summary>Creates a successful borrow, committing its consumed prefix through <paramref name="complete"/>.</summary>
    public static AwkReadBuffer FromContent(ReadOnlyMemory<byte> content, AwkReadCompletion completion,
        Action<int> complete)
    {
        if (completion is not AwkReadCompletion.MoreData and not AwkReadCompletion.EndOfFile)
            throw new ArgumentOutOfRangeException(nameof(completion));
        if (content.IsEmpty && completion == AwkReadCompletion.MoreData)
            throw new ArgumentException("A nonterminal successful read must contain bytes.", nameof(content));
        return new(content, completion, default, complete);
    }

    /// <summary>Creates an empty terminal failure without owned host storage.</summary>
    public static AwkReadBuffer Failure(AwkOperationError error) =>
        Failure(error, default, static _ => { });

    /// <summary>Creates a terminal failure with a usable byte prefix and an explicit borrow completion.</summary>
    public static AwkReadBuffer Failure(AwkOperationError error, ReadOnlyMemory<byte> content, Action<int> complete) =>
        new(content, AwkReadCompletion.Failure, error, complete);

    /// <summary>Advances the absolute consumed prefix monotonically within <see cref="Content"/>.</summary>
    public void SetConsumedBytes(int count)
    {
        ObjectDisposedException.ThrowIf(_complete is null, this);
        if (count < _consumed || count > _content.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        _consumed = count;
    }

    /// <summary>Commits consumption once and ends the borrow, independently of cancellation.</summary>
    public void Dispose()
    {
        var complete = Interlocked.Exchange(ref _complete, null);
        if (complete is null) return;
        _content = default;
        complete(_consumed);
    }
}
