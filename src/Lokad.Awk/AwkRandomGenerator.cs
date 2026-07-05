using System;

namespace Lokad.Awk;

/// <summary>
/// Implements the gawk random sequence used by <c>rand()</c> and <c>srand()</c>.
/// </summary>
/// <remarks>
/// The additive generator, warm-up, and shuffle stage intentionally mirror gawk so
/// explicit seeds produce stable oracle-compatible values. Runtime state receives
/// the generator through <see cref="AwkRuntimeOptions"/> instead of constructing it
/// at call sites, which keeps deterministic tests and future compatibility modes explicit.
/// </remarks>
internal sealed class AwkRandomGenerator
{
    private const int Degree = 63;
    private const int Separation = 1;
    private const int ShuffleSize = 512;
    private const int ShuffleMask = ShuffleSize - 1;
    private const double Divisor = 2147483648.0;

    private readonly uint[] _state = new uint[Degree];
    private readonly long[] _shuffleBuffer = new long[ShuffleSize];
    private bool _shuffleNeedsInitialization;
    private int _frontIndex;
    private int _rearIndex;
    private long _shuffleState = 0xcafefeedL;
    private AwkRandomSeed _seed = new(1);

    public AwkRandomGenerator()
    {
        Seed(new AwkRandomSeed(1));
    }

    public AwkRandomSeed Seed(AwkRandomSeed seed)
    {
        var previousSeed = _seed;
        _seed = seed;
        SeedState(unchecked((uint)seed.Value));
        return previousSeed;
    }

    public double NextDouble()
    {
        while (true)
        {
            var first = NextInteger();
            var second = NextInteger();
            var value = 0.5 + (((first / Divisor) + second) / Divisor);
            value -= 0.5;
            if (value != 1.0)
                return value;
        }
    }

    private void SeedState(uint seed)
    {
        _shuffleNeedsInitialization = true;
        _state[0] = seed;
        for (var index = 1; index < Degree; index++)
        {
            _state[index] = GoodRand(unchecked((int)_state[index - 1]));
        }

        _frontIndex = Separation;
        _rearIndex = 0;
        for (var index = 0; index < 10 * Degree; index++)
        {
            _ = NextInteger();
        }
    }

    private static uint GoodRand(int value)
    {
        if (value == 0)
            value = 123459876;

        var high = value / 127773;
        var low = value % 127773;
        value = (16807 * low) - (2836 * high);
        if (value < 0)
            value += 0x7fffffff;

        return unchecked((uint)value);
    }

    private long NextInteger()
    {
        if (_shuffleNeedsInitialization)
        {
            for (var index = 0; index < ShuffleSize; index++)
            {
                _shuffleBuffer[index] = NextOldInteger();
            }

            _shuffleState = NextOldInteger();
            _shuffleNeedsInitialization = false;
        }

        var replacement = NextOldInteger();
        var shuffleIndex = (int)(_shuffleState & ShuffleMask);
        _shuffleState = _shuffleBuffer[shuffleIndex];
        _shuffleBuffer[shuffleIndex] = replacement;
        return _shuffleState;
    }

    private long NextOldInteger()
    {
        _state[_frontIndex] = unchecked(_state[_frontIndex] + _state[_rearIndex]);
        var result = (long)((_state[_frontIndex] >> 1) & 0x7fffffff);

        _frontIndex++;
        if (_frontIndex >= Degree)
        {
            _frontIndex = 0;
            _rearIndex++;
        }
        else
        {
            _rearIndex++;
            if (_rearIndex >= Degree)
                _rearIndex = 0;
        }

        return result;
    }
}
