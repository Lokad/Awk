using Lokad.Awk;
using ToolAwkMessages = Lokad.Awk.AwkMessages;

namespace Lokad.Awk.Tests;

public sealed class AwkNumericOperationsTests
{
    [Fact]
    public void AwkNumericOperations_ComputesDivideAndModulo()
    {
        Assert.Equal(4, AwkNumericOperations.Divide(8, 2, ToolAwkMessages.ErrorDivisionByZero));
        Assert.Equal(1, AwkNumericOperations.Modulo(7, 3, ToolAwkMessages.ErrorDivisionByZeroInModulo));
    }

    [Fact]
    public void AwkNumericOperations_PreservesCallerSpecificZeroDiagnostics()
    {
        var divide = Assert.Throws<AwkRuntimeException>(
            () => AwkNumericOperations.Divide(1, 0, ToolAwkMessages.ErrorDivisionByZeroInDivide));
        var modulo = Assert.Throws<AwkRuntimeException>(
            () => AwkNumericOperations.Modulo(1, 0, ToolAwkMessages.ErrorDivisionByZeroInModuloAssignment));

        Assert.Equal(
            ToolAwkMessages.ErrorDivisionByZeroInDivide.ToArray(),
            divide.MessageBytes.ToArray());
        Assert.Equal(
            ToolAwkMessages.ErrorDivisionByZeroInModuloAssignment.ToArray(),
            modulo.MessageBytes.ToArray());
    }
}
