using System.Collections.Concurrent;

namespace Lokad.Awk.Tests;

public sealed class AwkMemoryBudgetTests
{
    [Fact]
    public void RefusalPreservesChargesAndShrinkMakesCapacityImmediatelyReusable()
    {
        var budget = AwkMemoryBudget.CreateFixed(100);
        using var scope = budget.CreateScope();
        Assert.True(scope.TryReserve(70, out var first));
        using (first)
        {
            Assert.False(budget.TryReserve(31, out var refused));
            Assert.Null(refused);
            Assert.False(first.TryResize(101));
            Assert.Equal(70, first.Bytes);
            Assert.Equal(70, budget.ReservedBytes);
            Assert.True(budget.TryReserve(30, out var other));
            using (other)
            {
                Assert.True(first.TryResize(20));
                Assert.True(other.TryResize(80));
                Assert.Equal(100, budget.ReservedBytes);
                Assert.True(first.TryResize(0));
                Assert.True(other.TryResize(100));
            }
            Assert.Equal(0, budget.ReservedBytes);
        }
        Assert.Equal(100, budget.LimitBytes);
        Assert.Equal(100, budget.PeakBytes);
    }

    [Theory]
    [InlineData(0)] [InlineData(7)]
    public void ScopeDisposalRefundsItsSubtreeOnceAndInvalidatesDescendants(long bytes)
    {
        var budget = AwkMemoryBudget.CreateFixed(100);
        using var parent = budget.CreateScope();
        using var child = parent.CreateScope();
        using var sibling = budget.CreateScope();
        Assert.True(parent.TryReserve(bytes, out var parentCharge));
        Assert.True(child.TryReserve(bytes, out var childCharge));
        Assert.True(sibling.TryReserve(30, out var other));
        using (parentCharge)
        using (childCharge)
        using (other)
        {
            parent.Dispose();
            Assert.Equal(0, parentCharge.Bytes);
            Assert.Equal(0, childCharge.Bytes);
            Assert.Equal(30, other.Bytes);
            Assert.Equal(30, budget.ReservedBytes);
            Assert.Throws<ObjectDisposedException>(() => parent.TryReserve(0, out _));
            Assert.Throws<ObjectDisposedException>(() => child.TryReserve(0, out _));
            Assert.Throws<ObjectDisposedException>(() => child.CreateScope());
            Assert.Throws<ObjectDisposedException>(() => childCharge.TryResize(0));
            childCharge.Dispose();
            child.Dispose();
            parent.Dispose();
            Assert.Equal(30, budget.ReservedBytes);
            Assert.True(other.TryResize(100));
        }
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Fact]
    public void ZeroAllowanceAndDisposedReservationsFollowThePublicContract()
    {
        var budget = AwkMemoryBudget.CreateFixed(0);
        using var scope = budget.CreateScope();
        Assert.False(scope.TryReserve(1, out var refused));
        Assert.Null(refused);
        Assert.True(scope.TryReserve(0, out var empty));
        Assert.True(empty.TryResize(0));
        Assert.False(empty.TryResize(1));
        empty.Dispose();
        empty.Dispose();
        Assert.Equal(0, empty.Bytes);
        Assert.Throws<ObjectDisposedException>(() => empty.TryResize(0));
        Assert.Equal(0, budget.ReservedBytes);
        Assert.Equal(0, budget.PeakBytes);
    }

    [Fact]
    public void NegativeEstimatesAreRejectedWithoutChangingAdmission()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AwkMemoryBudget.CreateFixed(-1));
        var budget = AwkMemoryBudget.CreateFixed(100);
        using var scope = budget.CreateScope();
        Assert.True(scope.TryReserve(40, out var charge));
        using (charge)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => budget.TryReserve(-1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => scope.TryReserve(-1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => charge.TryResize(-1));
            Assert.Equal(40, charge.Bytes);
            Assert.Equal(40, budget.ReservedBytes);
        }
    }

    [Fact]
    public void LargeEstimatesCannotOverflowTheAllowance()
    {
        var budget = AwkMemoryBudget.CreateFixed(long.MaxValue);
        using var scope = budget.CreateScope();
        Assert.True(scope.TryReserve(long.MaxValue - 2, out var large));
        Assert.True(budget.TryReserve(2, out var small));
        using (large)
        using (small)
        {
            Assert.Equal(long.MaxValue, budget.ReservedBytes);
            Assert.False(large.TryResize(long.MaxValue));
            Assert.False(scope.TryReserve(1, out _));
            Assert.Equal(long.MaxValue - 2, large.Bytes);
            Assert.True(large.TryResize(0));
            Assert.True(small.TryResize(long.MaxValue));
            scope.Dispose();
            Assert.Equal(long.MaxValue, budget.ReservedBytes);
        }
        Assert.Equal(0, budget.ReservedBytes);
        Assert.Equal(long.MaxValue, budget.PeakBytes);
    }

    [Fact]
    public void ConcurrentScopesShareTheSameAllowance()
    {
        var budget = AwkMemoryBudget.CreateFixed(256);
        var admitted = new ConcurrentBag<(IMemoryScope Scope, IMemoryReservation Charge)>();
        Parallel.For(0, 64, _ =>
        {
            var scope = budget.CreateScope();
            if (scope.TryReserve(16, out var charge)) admitted.Add((scope, charge));
            else scope.Dispose();
        });
        Assert.Equal(16, admitted.Count);
        Assert.Equal(256, budget.ReservedBytes);
        Parallel.ForEach(admitted, owner =>
        {
            Assert.True(owner.Charge.TryResize(8));
            owner.Scope.Dispose();
            Assert.Equal(0, owner.Charge.Bytes);
            owner.Charge.Dispose();
        });
        Assert.Equal(0, budget.ReservedBytes);
        Assert.Equal(256, budget.PeakBytes);
        Assert.True(budget.TryReserve(256, out var reusable));
        reusable.Dispose();
    }

    [Fact]
    public void RestrictedPolicyBorrowsItsRequiredBudgetAndDefaultRemainsUncapped()
    {
        var budget = AwkMemoryBudget.CreateFixed(1024);
        var policy = AwkExecutionPolicy.CreateRestricted(budget);
        Assert.Same(budget, policy.MemoryBudget);
        Assert.Same(budget, policy.WithCommandPipes(true).MemoryBudget);
        Assert.Null(AwkExecutionPolicy.CreateDefault().MemoryBudget);
    }

#pragma warning disable CS8625 // Deliberately exercise the public argument boundary.
    [Fact]
    public void RestrictedPolicyRejectsNullBudget() =>
        Assert.Throws<ArgumentNullException>(() => AwkExecutionPolicy.CreateRestricted(null));
#pragma warning restore CS8625
}
