namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> BitwiseCompatibilityCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,7)),(and(0,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,7)),(or(0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,7)),(xor(0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(0)),(compl(0))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(0,1)),(lshift(0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(0,1)),(rshift(0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(-0,7)),(and(-0,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(-0,7)),(or(-0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(-0,7)),(xor(-0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(-0)),(compl(-0))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(-0,1)),(lshift(-0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(-0,1)),(rshift(-0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0.9,7)),(and(0.9,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0.9,7)),(or(0.9,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0.9,7)),(xor(0.9,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(0.9)),(compl(0.9))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(0.9,1)),(lshift(0.9,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(0.9,1)),(rshift(0.9,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(1,7)),(and(1,7))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(1,7)),(or(1,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(1,7)),(xor(1,7))}"], [])
            .WithStdOut("before\n6 [0x1.8p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(1)),(compl(1))}"], [])
            .WithStdOut("before\n18014398509481982 [0x1.fffffffffffffp+53]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(1,1)),(lshift(1,1))}"], [])
            .WithStdOut("before\n2 [0x1p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(1,1)),(rshift(1,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(1.9,7)),(and(1.9,7))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(1.9,7)),(or(1.9,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(1.9,7)),(xor(1.9,7))}"], [])
            .WithStdOut("before\n6 [0x1.8p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(1.9)),(compl(1.9))}"], [])
            .WithStdOut("before\n18014398509481982 [0x1.fffffffffffffp+53]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(1.9,1)),(lshift(1.9,1))}"], [])
            .WithStdOut("before\n2 [0x1p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(1.9,1)),(rshift(1.9,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2,7)),(and(2,7))}"], [])
            .WithStdOut("before\n2 [0x1p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2,7)),(or(2,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2,7)),(xor(2,7))}"], [])
            .WithStdOut("before\n5 [0x1.4p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2)),(compl(2))}"], [])
            .WithStdOut("before\n9007199254740989 [0x1.ffffffffffffdp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2,1)),(lshift(2,1))}"], [])
            .WithStdOut("before\n4 [0x1p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2,1)),(rshift(2,1))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(3,7)),(and(3,7))}"], [])
            .WithStdOut("before\n3 [0x1.8p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(3,7)),(or(3,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(3,7)),(xor(3,7))}"], [])
            .WithStdOut("before\n4 [0x1p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(3)),(compl(3))}"], [])
            .WithStdOut("before\n36028797018963964 [0x1.fffffffffffffp+54]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(3,1)),(lshift(3,1))}"], [])
            .WithStdOut("before\n6 [0x1.8p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(3,1)),(rshift(3,1))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(7,7)),(and(7,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(7,7)),(or(7,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(7,7)),(xor(7,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(7)),(compl(7))}"], [])
            .WithStdOut("before\n72057594037927928 [0x1.fffffffffffffp+55]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(7,1)),(lshift(7,1))}"], [])
            .WithStdOut("before\n14 [0x1.cp+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(7,1)),(rshift(7,1))}"], [])
            .WithStdOut("before\n3 [0x1.8p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(255,7)),(and(255,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(255,7)),(or(255,7))}"], [])
            .WithStdOut("before\n255 [0x1.fep+7]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(255,7)),(xor(255,7))}"], [])
            .WithStdOut("before\n248 [0x1.fp+7]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(255)),(compl(255))}"], [])
            .WithStdOut("before\n2305843009213693696 [0x1.fffffffffffffp+60]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(255,1)),(lshift(255,1))}"], [])
            .WithStdOut("before\n510 [0x1.fep+8]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(255,1)),(rshift(255,1))}"], [])
            .WithStdOut("before\n127 [0x1.fcp+6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^32-1,7)),(and(2^32-1,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^32-1,7)),(or(2^32-1,7))}"], [])
            .WithStdOut("before\n4294967295 [0x1.fffffffep+31]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^32-1,7)),(xor(2^32-1,7))}"], [])
            .WithStdOut("before\n4294967288 [0x1.fffffffp+31]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^32-1)),(compl(2^32-1))}"], [])
            .WithStdOut("before\n18446744069414584320 [0x1.fffffffep+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^32-1,1)),(lshift(2^32-1,1))}"], [])
            .WithStdOut("before\n8589934590 [0x1.fffffffep+32]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^32-1,1)),(rshift(2^32-1,1))}"], [])
            .WithStdOut("before\n2147483647 [0x1.fffffffcp+30]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^32,7)),(and(2^32,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^32,7)),(or(2^32,7))}"], [])
            .WithStdOut("before\n4294967303 [0x1.00000007p+32]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^32,7)),(xor(2^32,7))}"], [])
            .WithStdOut("before\n4294967303 [0x1.00000007p+32]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^32)),(compl(2^32))}"], [])
            .WithStdOut("before\n9007194959773695 [0x1.ffffeffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^32,1)),(lshift(2^32,1))}"], [])
            .WithStdOut("before\n8589934592 [0x1p+33]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^32,1)),(rshift(2^32,1))}"], [])
            .WithStdOut("before\n2147483648 [0x1p+31]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^53-1,7)),(and(2^53-1,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^53-1,7)),(or(2^53-1,7))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^53-1,7)),(xor(2^53-1,7))}"], [])
            .WithStdOut("before\n9007199254740984 [0x1.ffffffffffff8p+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^53-1)),(compl(2^53-1))}"], [])
            .WithStdOut("before\n18437736874454810624 [0x1.ffcp+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^53-1,1)),(lshift(2^53-1,1))}"], [])
            .WithStdOut("before\n18014398509481982 [0x1.fffffffffffffp+53]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^53-1,1)),(rshift(2^53-1,1))}"], [])
            .WithStdOut("before\n4503599627370495 [0x1.ffffffffffffep+51]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^53,7)),(and(2^53,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^53,7)),(or(2^53,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^53,7)),(xor(2^53,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^53)),(compl(2^53))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^53,1)),(lshift(2^53,1))}"], [])
            .WithStdOut("before\n18014398509481984 [0x1p+54]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^53,1)),(rshift(2^53,1))}"], [])
            .WithStdOut("before\n4503599627370496 [0x1p+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^53+2,7)),(and(2^53+2,7))}"], [])
            .WithStdOut("before\n2 [0x1p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^53+2,7)),(or(2^53+2,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^53+2,7)),(xor(2^53+2,7))}"], [])
            .WithStdOut("before\n5 [0x1.4p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^53+2)),(compl(2^53+2))}"], [])
            .WithStdOut("before\n9007199254740989 [0x1.ffffffffffffdp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^53+2,1)),(lshift(2^53+2,1))}"], [])
            .WithStdOut("before\n18014398509481988 [0x1.0000000000001p+54]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^53+2,1)),(rshift(2^53+2,1))}"], [])
            .WithStdOut("before\n4503599627370497 [0x1.0000000000001p+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^54-2,7)),(and(2^54-2,7))}"], [])
            .WithStdOut("before\n6 [0x1.8p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^54-2,7)),(or(2^54-2,7))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^54-2,7)),(xor(2^54-2,7))}"], [])
            .WithStdOut("before\n9007199254740985 [0x1.ffffffffffff9p+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^54-2)),(compl(2^54-2))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^54-2,1)),(lshift(2^54-2,1))}"], [])
            .WithStdOut("before\n36028797018963964 [0x1.fffffffffffffp+54]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^54-2,1)),(rshift(2^54-2,1))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^60,7)),(and(2^60,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^60,7)),(or(2^60,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^60,7)),(xor(2^60,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^60)),(compl(2^60))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^60,1)),(lshift(2^60,1))}"], [])
            .WithStdOut("before\n2305843009213693952 [0x1p+61]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^60,1)),(rshift(2^60,1))}"], [])
            .WithStdOut("before\n576460752303423488 [0x1p+59]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^63,7)),(and(2^63,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^63,7)),(or(2^63,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^63,7)),(xor(2^63,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^63)),(compl(2^63))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63,1)),(lshift(2^63,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63,1)),(rshift(2^63,1))}"], [])
            .WithStdOut("before\n4611686018427387904 [0x1p+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^64-2048,7)),(and(2^64-2048,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^64-2048,7)),(or(2^64-2048,7))}"], [])
            .WithStdOut("before\n9007199254738951 [0x1.ffffffffff807p+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^64-2048,7)),(xor(2^64-2048,7))}"], [])
            .WithStdOut("before\n9007199254738951 [0x1.ffffffffff807p+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^64-2048)),(compl(2^64-2048))}"], [])
            .WithStdOut("before\n2047 [0x1.ffcp+10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^64-2048,1)),(lshift(2^64-2048,1))}"], [])
            .WithStdOut("before\n18446744073709547520 [0x1.ffffffffffffep+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^64-2048,1)),(rshift(2^64-2048,1))}"], [])
            .WithStdOut("before\n9223372036854774784 [0x1.fffffffffffffp+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^64,7)),(and(2^64,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^64,7)),(or(2^64,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^64,7)),(xor(2^64,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^64)),(compl(2^64))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^64,1)),(lshift(2^64,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^64,1)),(rshift(2^64,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^64+4096,7)),(and(2^64+4096,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^64+4096,7)),(or(2^64+4096,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^64+4096,7)),(xor(2^64+4096,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^64+4096)),(compl(2^64+4096))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^64+4096,1)),(lshift(2^64+4096,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^64+4096,1)),(rshift(2^64+4096,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^65-4096,7)),(and(2^65-4096,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^65-4096,7)),(or(2^65-4096,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^65-4096,7)),(xor(2^65-4096,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^65-4096)),(compl(2^65-4096))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^65-4096,1)),(lshift(2^65-4096,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^65-4096,1)),(rshift(2^65-4096,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^65,7)),(and(2^65,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^65,7)),(or(2^65,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^65,7)),(xor(2^65,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^65)),(compl(2^65))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^65,1)),(lshift(2^65,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^65,1)),(rshift(2^65,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^100,7)),(and(2^100,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^100,7)),(or(2^100,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^100,7)),(xor(2^100,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(2^100)),(compl(2^100))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^100,1)),(lshift(2^100,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^100,1)),(rshift(2^100,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(1e308,7)),(and(1e308,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(1e308,7)),(or(1e308,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(1e308,7)),(xor(1e308,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(1e308)),(compl(1e308))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(1e308,1)),(lshift(1e308,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(1e308,1)),(rshift(1e308,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-24",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(\"+inf\"+0,7)),(and(\"+inf\"+0,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-24",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(\"+inf\"+0,7)),(or(\"+inf\"+0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-24",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(\"+inf\"+0,7)),(xor(\"+inf\"+0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-24",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(\"+inf\"+0)),(compl(\"+inf\"+0))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-24",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(\"+inf\"+0,1)),(lshift(\"+inf\"+0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-24",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(\"+inf\"+0,1)),(rshift(\"+inf\"+0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-25",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(\"+nan\"+0,7)),(and(\"+nan\"+0,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-25",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(\"+nan\"+0,7)),(or(\"+nan\"+0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-25",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(\"+nan\"+0,7)),(xor(\"+nan\"+0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-25",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(\"+nan\"+0)),(compl(\"+nan\"+0))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-25",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(\"+nan\"+0,1)),(lshift(\"+nan\"+0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-25",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(\"+nan\"+0,1)),(rshift(\"+nan\"+0,1))}"], [])
            .WithStdOut("before\n4611686018427387904 [0x1p+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-26",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(\"-nan\"+0,7)),(and(\"-nan\"+0,7))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-26",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(\"-nan\"+0,7)),(or(\"-nan\"+0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-26",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(\"-nan\"+0,7)),(xor(\"-nan\"+0,7))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-26",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(\"-nan\"+0)),(compl(\"-nan\"+0))}"], [])
            .WithStdOut("before\n9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-26",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(\"-nan\"+0,1)),(lshift(\"-nan\"+0,1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-26",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(\"-nan\"+0,1)),(rshift(\"-nan\"+0,1))}"], [])
            .WithStdOut("before\n4611686018427387904 [0x1p+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-27",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(\"-inf\"+0,7)),(and(\"-inf\"+0,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-27",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(\"-inf\"+0,7)),(or(\"-inf\"+0,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-27",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(\"-inf\"+0,7)),(xor(\"-inf\"+0,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-27",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(\"-inf\"+0)),(compl(\"-inf\"+0))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-27",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(\"-inf\"+0,1)),(lshift(\"-inf\"+0,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-27",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(\"-inf\"+0,1)),(rshift(\"-inf\"+0,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-28",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(-0.1,7)),(and(-0.1,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-28",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(-0.1,7)),(or(-0.1,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-28",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(-0.1,7)),(xor(-0.1,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-28",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(-0.1)),(compl(-0.1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-28",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(-0.1,1)),(lshift(-0.1,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-28",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(-0.1,1)),(rshift(-0.1,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-29",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(-1,7)),(and(-1,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-29",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(-1,7)),(or(-1,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-29",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(-1,7)),(xor(-1,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-29",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(-1)),(compl(-1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-29",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(-1,1)),(lshift(-1,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-29",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(-1,1)),(rshift(-1,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-and-30",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(-2^63,7)),(and(-2^63,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-or-30",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(-2^63,7)),(or(-2^63,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-xor-30",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(-2^63,7)),(xor(-2^63,7))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-compl-30",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(compl(-2^63)),(compl(-2^63))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-lshift-30",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(-2^63,1)),(lshift(-2^63,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-operand-rshift-30",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(-2^63,1)),(rshift(-2^63,1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,0)),(lshift(2^63+4096,0))}"], [])
            .WithStdOut("before\n9223372036854779904 [0x1.0000000000002p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,0.9)),(lshift(2^63+4096,0.9))}"], [])
            .WithStdOut("before\n9223372036854779904 [0x1.0000000000002p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,1.9)),(lshift(2^63+4096,1.9))}"], [])
            .WithStdOut("before\n8192 [0x1p+13]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,31)),(lshift(2^63+4096,31))}"], [])
            .WithStdOut("before\n8796093022208 [0x1p+43]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,32)),(lshift(2^63+4096,32))}"], [])
            .WithStdOut("before\n17592186044416 [0x1p+44]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,52)),(lshift(2^63+4096,52))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,53)),(lshift(2^63+4096,53))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,62)),(lshift(2^63+4096,62))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,63)),(lshift(2^63+4096,63))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,63.9)),(lshift(2^63+4096,63.9))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,64)),(lshift(2^63+4096,64))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,64.1)),(lshift(2^63+4096,64.1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,65)),(lshift(2^63+4096,65))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,127)),(lshift(2^63+4096,127))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,128)),(lshift(2^63+4096,128))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,2^32)),(lshift(2^63+4096,2^32))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,2^64)),(lshift(2^63+4096,2^64))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,1e308)),(lshift(2^63+4096,1e308))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,\"+inf\"+0)),(lshift(2^63+4096,\"+inf\"+0))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,\"+nan\"+0)),(lshift(2^63+4096,\"+nan\"+0))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,\"-nan\"+0)),(lshift(2^63+4096,\"-nan\"+0))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,-0.1)),(lshift(2^63+4096,-0.1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,-1)),(lshift(2^63+4096,-1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-lshift-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(lshift(2^63+4096,\"-inf\"+0)),(lshift(2^63+4096,\"-inf\"+0))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,0)),(rshift(2^63+4096,0))}"], [])
            .WithStdOut("before\n9223372036854779904 [0x1.0000000000002p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,0.9)),(rshift(2^63+4096,0.9))}"], [])
            .WithStdOut("before\n9223372036854779904 [0x1.0000000000002p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,1.9)),(rshift(2^63+4096,1.9))}"], [])
            .WithStdOut("before\n4611686018427389952 [0x1.0000000000002p+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,31)),(rshift(2^63+4096,31))}"], [])
            .WithStdOut("before\n4294967296 [0x1p+32]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,32)),(rshift(2^63+4096,32))}"], [])
            .WithStdOut("before\n2147483648 [0x1p+31]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,52)),(rshift(2^63+4096,52))}"], [])
            .WithStdOut("before\n2048 [0x1p+11]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,53)),(rshift(2^63+4096,53))}"], [])
            .WithStdOut("before\n1024 [0x1p+10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,62)),(rshift(2^63+4096,62))}"], [])
            .WithStdOut("before\n2 [0x1p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,63)),(rshift(2^63+4096,63))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,63.9)),(rshift(2^63+4096,63.9))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-10",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,64)),(rshift(2^63+4096,64))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-11",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,64.1)),(rshift(2^63+4096,64.1))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-12",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,65)),(rshift(2^63+4096,65))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-13",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,127)),(rshift(2^63+4096,127))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-14",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,128)),(rshift(2^63+4096,128))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-15",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,2^32)),(rshift(2^63+4096,2^32))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-16",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,2^64)),(rshift(2^63+4096,2^64))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-17",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,1e308)),(rshift(2^63+4096,1e308))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-18",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,\"+inf\"+0)),(rshift(2^63+4096,\"+inf\"+0))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-19",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,\"+nan\"+0)),(rshift(2^63+4096,\"+nan\"+0))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-20",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,\"-nan\"+0)),(rshift(2^63+4096,\"-nan\"+0))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-21",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,-0.1)),(rshift(2^63+4096,-0.1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-22",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,-1)),(rshift(2^63+4096,-1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-shift-rshift-23",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(rshift(2^63+4096,\"-inf\"+0)),(rshift(2^63+4096,\"-inf\"+0))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-and-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and()),(and())}"], [])
            .WithStdErr("awk: invalid number of arguments for and\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-and-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0)),(and(0))}"], [])
            .WithStdErr("awk: invalid number of arguments for and\n")
            .ExitsWith(2)
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-and-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,13)),(and(0,13))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-and-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,13,26)),(and(0,13,26))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-and-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,13,26,39)),(and(0,13,26,39))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-and-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,13,26,39,52,65,78,91)),(and(0,13,26,39,52,65,78,91))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(7,3,1)),(and(7,3,1))}"], [])
            .WithStdOut("before\n1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(1,2,4)),(and(1,2,4))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(1,2,3)),(and(1,2,3))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^63,2^60,8)),(and(2^63,2^60,8))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^63,2^60,8,8)),(and(2^63,2^60,8,8))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^63,2^53+2,2)),(and(2^63,2^53+2,2))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,1,-0.1)),(and(0,1,-0.1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(0,1,\"-inf\"+0)),(and(0,1,\"-inf\"+0))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(2^63,8,8)),(and(2^63,8,8))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-and-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(and(8,8,2^63)),(and(8,8,2^63))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-and-0",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=and(f(0),f(1),f(2));print \"result\",x}"], [])
            .WithStdOut("argument 0\nargument 1\nargument 2\nresult 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-and-1",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=and(f(-0.1),f(1),f(2));print \"bad\"}"], [])
            .WithStdOut("argument -0.1\nargument 1\nargument 2\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-and-2",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=and(f(0),f(1),f(-0.1));print \"bad\"}"], [])
            .WithStdOut("argument 0\nargument 1\nargument -0.1\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-and-3",
                ["BEGIN{i=0;print and(i++,i++,i++),i}"], [])
            .WithStdOut("0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-and-4",
                ["BEGIN{a[1]=1;a[2]=2;print and(a[1],a[2],a[1]++),a[1]}"], [])
            .WithStdOut("0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-and-5",
                ["BEGIN{CONVFMT=\"%.1f\";print and(\"12x\",\"7.9\",\"3.1\");print typeof(and(1,2,3))}"], [])
            .WithStdOut("0\nnumber\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-and-0",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print and(s,1,-0.1)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-and-1",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print and(-0.1,1,s)}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-and-2",
                ["--non-decimal-data", "function f(){print \"later\";return 3}BEGIN{print and(-0.1,1,f())}"], [])
            .WithStdOut("later\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-and-3",
                ["--non-decimal-data", "function f(){print \"later\";return 3}BEGIN{print and(\"0x1.8\",1,f())}"], [])
            .WithStdOut("later\n")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-or-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or()),(or())}"], [])
            .WithStdErr("awk: invalid number of arguments for or\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-or-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0)),(or(0))}"], [])
            .WithStdErr("awk: invalid number of arguments for or\n")
            .ExitsWith(2)
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-or-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,13)),(or(0,13))}"], [])
            .WithStdOut("before\n13 [0x1.ap+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-or-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,13,26)),(or(0,13,26))}"], [])
            .WithStdOut("before\n31 [0x1.fp+4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-or-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,13,26,39)),(or(0,13,26,39))}"], [])
            .WithStdOut("before\n63 [0x1.f8p+5]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-or-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,13,26,39,52,65,78,91)),(or(0,13,26,39,52,65,78,91))}"], [])
            .WithStdOut("before\n127 [0x1.fcp+6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(7,3,1)),(or(7,3,1))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(1,2,4)),(or(1,2,4))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(1,2,3)),(or(1,2,3))}"], [])
            .WithStdOut("before\n3 [0x1.8p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^63,2^60,8)),(or(2^63,2^60,8))}"], [])
            .WithStdOut("before\n8 [0x1p+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^63,2^60,8,8)),(or(2^63,2^60,8,8))}"], [])
            .WithStdOut("before\n8 [0x1p+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^63,2^53+2,2)),(or(2^63,2^53+2,2))}"], [])
            .WithStdOut("before\n9007199254740994 [0x1.0000000000001p+53]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,1,-0.1)),(or(0,1,-0.1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(0,1,\"-inf\"+0)),(or(0,1,\"-inf\"+0))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(2^63,8,8)),(or(2^63,8,8))}"], [])
            .WithStdOut("before\n8 [0x1p+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-or-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(or(8,8,2^63)),(or(8,8,2^63))}"], [])
            .WithStdOut("before\n8 [0x1p+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-or-0",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=or(f(0),f(1),f(2));print \"result\",x}"], [])
            .WithStdOut("argument 0\nargument 1\nargument 2\nresult 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-or-1",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=or(f(-0.1),f(1),f(2));print \"bad\"}"], [])
            .WithStdOut("argument -0.1\nargument 1\nargument 2\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-or-2",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=or(f(0),f(1),f(-0.1));print \"bad\"}"], [])
            .WithStdOut("argument 0\nargument 1\nargument -0.1\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-or-3",
                ["BEGIN{i=0;print or(i++,i++,i++),i}"], [])
            .WithStdOut("3 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-or-4",
                ["BEGIN{a[1]=1;a[2]=2;print or(a[1],a[2],a[1]++),a[1]}"], [])
            .WithStdOut("3 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-or-5",
                ["BEGIN{CONVFMT=\"%.1f\";print or(\"12x\",\"7.9\",\"3.1\");print typeof(or(1,2,3))}"], [])
            .WithStdOut("15\nnumber\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-or-0",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print or(s,1,-0.1)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-or-1",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print or(-0.1,1,s)}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-or-2",
                ["--non-decimal-data", "function f(){print \"later\";return 3}BEGIN{print or(-0.1,1,f())}"], [])
            .WithStdOut("later\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-or-3",
                ["--non-decimal-data", "function f(){print \"later\";return 3}BEGIN{print or(\"0x1.8\",1,f())}"], [])
            .WithStdOut("later\n")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-xor-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor()),(xor())}"], [])
            .WithStdErr("awk: invalid number of arguments for xor\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-xor-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0)),(xor(0))}"], [])
            .WithStdErr("awk: invalid number of arguments for xor\n")
            .ExitsWith(2)
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-xor-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,13)),(xor(0,13))}"], [])
            .WithStdOut("before\n13 [0x1.ap+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-xor-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,13,26)),(xor(0,13,26))}"], [])
            .WithStdOut("before\n23 [0x1.7p+4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-xor-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,13,26,39)),(xor(0,13,26,39))}"], [])
            .WithStdOut("before\n48 [0x1.8p+5]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-arity-xor-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,13,26,39,52,65,78,91)),(xor(0,13,26,39,52,65,78,91))}"], [])
            .WithStdOut("before\n80 [0x1.4p+6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-0",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(7,3,1)),(xor(7,3,1))}"], [])
            .WithStdOut("before\n5 [0x1.4p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-1",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(1,2,4)),(xor(1,2,4))}"], [])
            .WithStdOut("before\n7 [0x1.cp+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-2",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(1,2,3)),(xor(1,2,3))}"], [])
            .WithStdOut("before\n0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-3",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^63,2^60,8)),(xor(2^63,2^60,8))}"], [])
            .WithStdOut("before\n8 [0x1p+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-4",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^63,2^60,8,8)),(xor(2^63,2^60,8,8))}"], [])
            .WithStdOut("before\n10376293541461622784 [0x1.2p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-5",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^63,2^53+2,2)),(xor(2^63,2^53+2,2))}"], [])
            .WithStdOut("before\n9232379236109516800 [0x1.004p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-6",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,1,-0.1)),(xor(0,1,-0.1))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-7",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(0,1,\"-inf\"+0)),(xor(0,1,\"-inf\"+0))}"], [])
            .WithStdOut("before\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-8",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(2^63,8,8)),(xor(2^63,8,8))}"], [])
            .WithStdOut("before\n9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-variadic-xor-9",
                ["BEGIN{print \"before\";printf \"%.0f [%a]\\n\",(xor(8,8,2^63)),(xor(8,8,2^63))}"], [])
            .WithStdOut("before\n9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-xor-0",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=xor(f(0),f(1),f(2));print \"result\",x}"], [])
            .WithStdOut("argument 0\nargument 1\nargument 2\nresult 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-xor-1",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=xor(f(-0.1),f(1),f(2));print \"bad\"}"], [])
            .WithStdOut("argument -0.1\nargument 1\nargument 2\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-xor-2",
                ["function f(x){print \"argument\",x;return x}BEGIN{x=xor(f(0),f(1),f(-0.1));print \"bad\"}"], [])
            .WithStdOut("argument 0\nargument 1\nargument -0.1\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-xor-3",
                ["BEGIN{i=0;print xor(i++,i++,i++),i}"], [])
            .WithStdOut("3 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-xor-4",
                ["BEGIN{a[1]=1;a[2]=2;print xor(a[1],a[2],a[1]++),a[1]}"], [])
            .WithStdOut("2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-effects-xor-5",
                ["BEGIN{CONVFMT=\"%.1f\";print xor(\"12x\",\"7.9\",\"3.1\");print typeof(xor(1,2,3))}"], [])
            .WithStdOut("8\nnumber\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-xor-0",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print xor(s,1,-0.1)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-xor-1",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print xor(-0.1,1,s)}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-xor-2",
                ["--non-decimal-data", "function f(){print \"later\";return 3}BEGIN{print xor(-0.1,1,f())}"], [])
            .WithStdOut("later\n")
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-xor-3",
                ["--non-decimal-data", "function f(){print \"later\";return 3}BEGIN{print xor(\"0x1.8\",1,f())}"], [])
            .WithStdOut("later\n")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-compl-0",
                ["BEGIN{print compl()}"], [])
            .WithStdErr("awk: invalid number of arguments for compl\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-compl-2",
                ["BEGIN{print compl(1,1)}"], [])
            .WithStdErr("awk: invalid number of arguments for compl\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-lshift-0",
                ["BEGIN{print lshift()}"], [])
            .WithStdErr("awk: invalid number of arguments for lshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-lshift-1",
                ["BEGIN{print lshift(1)}"], [])
            .WithStdErr("awk: invalid number of arguments for lshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-lshift-3",
                ["BEGIN{print lshift(1,1,1)}"], [])
            .WithStdErr("awk: invalid number of arguments for lshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-rshift-0",
                ["BEGIN{print rshift()}"], [])
            .WithStdErr("awk: invalid number of arguments for rshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-rshift-1",
                ["BEGIN{print rshift(1)}"], [])
            .WithStdErr("awk: invalid number of arguments for rshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-arity-rshift-3",
                ["BEGIN{print rshift(1,1,1)}"], [])
            .WithStdErr("awk: invalid number of arguments for rshift\n")
            .ExitsWith(2)
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-lshift-0",
                ["--non-decimal-data", "BEGIN{print lshift(\"0x1.8\",-1)}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-lshift-1",
                ["--non-decimal-data", "BEGIN{print lshift(-1,\"0x1.8\")}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-lshift-2",
                ["--non-decimal-data", "BEGIN{print lshift(-1,64)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-lshift-3",
                ["--non-decimal-data", "BEGIN{print lshift(-0.1,64)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-lshift-4",
                ["--non-decimal-data", "BEGIN{print lshift(\"+nan\"+0,0)}"], [])
            .WithStdOut("9223372036854775808\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-lshift-5",
                ["--non-decimal-data", "BEGIN{print lshift(\"+inf\"+0,0)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-rshift-0",
                ["--non-decimal-data", "BEGIN{print rshift(\"0x1.8\",-1)}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-rshift-1",
                ["--non-decimal-data", "BEGIN{print rshift(-1,\"0x1.8\")}"], [])
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-rshift-2",
                ["--non-decimal-data", "BEGIN{print rshift(-1,64)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-rshift-3",
                ["--non-decimal-data", "BEGIN{print rshift(-0.1,64)}"], [])
            .WithStdErr("awk: negative bitwise operand\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-rshift-4",
                ["--non-decimal-data", "BEGIN{print rshift(\"+nan\"+0,0)}"], [])
            .WithStdOut("9223372036854775808\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-deferred-rshift-5",
                ["--non-decimal-data", "BEGIN{print rshift(\"+inf\"+0,0)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-posix-and",
                ["--posix", "BEGIN{print and(1,1,1)}"], [])
            .WithStdErr("awk: undefined function: and\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-posix-or",
                ["--posix", "BEGIN{print or(1,1,1)}"], [])
            .WithStdErr("awk: undefined function: or\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-posix-xor",
                ["--posix", "BEGIN{print xor(1,1,1)}"], [])
            .WithStdErr("awk: undefined function: xor\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-posix-compl",
                ["--posix", "BEGIN{print compl(1)}"], [])
            .WithStdErr("awk: undefined function: compl\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-posix-lshift",
                ["--posix", "BEGIN{print lshift(1,1)}"], [])
            .WithStdErr("awk: undefined function: lshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-posix-rshift",
                ["--posix", "BEGIN{print rshift(1,1)}"], [])
            .WithStdErr("awk: undefined function: rshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-traditional-and",
                ["--traditional", "BEGIN{print and(1,1,1)}"], [])
            .WithStdErr("awk: undefined function: and\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-traditional-or",
                ["--traditional", "BEGIN{print or(1,1,1)}"], [])
            .WithStdErr("awk: undefined function: or\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-traditional-xor",
                ["--traditional", "BEGIN{print xor(1,1,1)}"], [])
            .WithStdErr("awk: undefined function: xor\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-traditional-compl",
                ["--traditional", "BEGIN{print compl(1)}"], [])
            .WithStdErr("awk: undefined function: compl\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-traditional-lshift",
                ["--traditional", "BEGIN{print lshift(1,1)}"], [])
            .WithStdErr("awk: undefined function: lshift\n")
            .ExitsWith(2)
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("bitwise-profile-traditional-rshift",
                ["--traditional", "BEGIN{print rshift(1,1)}"], [])
            .WithStdErr("awk: undefined function: rshift\n")
            .ExitsWith(2)
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^0-1)),(compl(2^0-1))}"], [])
            .WithStdOut("9007199254740991 [0x1.fffffffffffffp+52]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^0)),(or(2^63,2^0))}"], [])
            .WithStdOut("1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^1-1)),(compl(2^1-1))}"], [])
            .WithStdOut("18014398509481982 [0x1.fffffffffffffp+53]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^1)),(or(2^63,2^1))}"], [])
            .WithStdOut("2 [0x1p+1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-2",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^2-1)),(compl(2^2-1))}"], [])
            .WithStdOut("36028797018963964 [0x1.fffffffffffffp+54]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-2",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^2)),(or(2^63,2^2))}"], [])
            .WithStdOut("4 [0x1p+2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-3",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^3-1)),(compl(2^3-1))}"], [])
            .WithStdOut("72057594037927928 [0x1.fffffffffffffp+55]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-3",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^3)),(or(2^63,2^3))}"], [])
            .WithStdOut("8 [0x1p+3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-4",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^4-1)),(compl(2^4-1))}"], [])
            .WithStdOut("144115188075855856 [0x1.fffffffffffffp+56]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-4",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^4)),(or(2^63,2^4))}"], [])
            .WithStdOut("16 [0x1p+4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-5",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^5-1)),(compl(2^5-1))}"], [])
            .WithStdOut("288230376151711712 [0x1.fffffffffffffp+57]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-5",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^5)),(or(2^63,2^5))}"], [])
            .WithStdOut("32 [0x1p+5]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-6",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^6-1)),(compl(2^6-1))}"], [])
            .WithStdOut("576460752303423424 [0x1.fffffffffffffp+58]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-6",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^6)),(or(2^63,2^6))}"], [])
            .WithStdOut("64 [0x1p+6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-7",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^7-1)),(compl(2^7-1))}"], [])
            .WithStdOut("1152921504606846848 [0x1.fffffffffffffp+59]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-7",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^7)),(or(2^63,2^7))}"], [])
            .WithStdOut("128 [0x1p+7]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-8",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^8-1)),(compl(2^8-1))}"], [])
            .WithStdOut("2305843009213693696 [0x1.fffffffffffffp+60]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-8",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^8)),(or(2^63,2^8))}"], [])
            .WithStdOut("256 [0x1p+8]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-9",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^9-1)),(compl(2^9-1))}"], [])
            .WithStdOut("4611686018427387392 [0x1.fffffffffffffp+61]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-9",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^9)),(or(2^63,2^9))}"], [])
            .WithStdOut("512 [0x1p+9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-10",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^10-1)),(compl(2^10-1))}"], [])
            .WithStdOut("9223372036854774784 [0x1.fffffffffffffp+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-10",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^10)),(or(2^63,2^10))}"], [])
            .WithStdOut("1024 [0x1p+10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-complement-11",
                ["BEGIN{printf \"%.0f [%a]\\n\",(compl(2^11-1)),(compl(2^11-1))}"], [])
            .WithStdOut("18446744073709549568 [0x1.fffffffffffffp+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-mask-or-11",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(2^63,2^11)),(or(2^63,2^11))}"], [])
            .WithStdOut("9223372036854777856 [0x1.0000000000001p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-and-+nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(and(\"+nan\"+0,0)),(and(\"+nan\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-and-+nan-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(and(\"+nan\"+0,2^63)),(and(\"+nan\"+0,2^63))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-or-+nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(\"+nan\"+0,0)),(or(\"+nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-or-+nan-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(\"+nan\"+0,2^63)),(or(\"+nan\"+0,2^63))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-xor-+nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(xor(\"+nan\"+0,0)),(xor(\"+nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-xor-+nan-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(xor(\"+nan\"+0,2^63)),(xor(\"+nan\"+0,2^63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+nan\"+0,0)),(lshift(\"+nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+nan-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+nan\"+0,1)),(lshift(\"+nan\"+0,1))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+nan-63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+nan\"+0,63)),(lshift(\"+nan\"+0,63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+nan-64",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+nan\"+0,64)),(lshift(\"+nan\"+0,64))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+nan\"+0,0)),(rshift(\"+nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+nan-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+nan\"+0,1)),(rshift(\"+nan\"+0,1))}"], [])
            .WithStdOut("4611686018427387904 [0x1p+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+nan-63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+nan\"+0,63)),(rshift(\"+nan\"+0,63))}"], [])
            .WithStdOut("1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+nan-64",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+nan\"+0,64)),(rshift(\"+nan\"+0,64))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-and--nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(and(\"-nan\"+0,0)),(and(\"-nan\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-and--nan-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(and(\"-nan\"+0,2^63)),(and(\"-nan\"+0,2^63))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-or--nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(\"-nan\"+0,0)),(or(\"-nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-or--nan-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(\"-nan\"+0,2^63)),(or(\"-nan\"+0,2^63))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-xor--nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(xor(\"-nan\"+0,0)),(xor(\"-nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-xor--nan-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(xor(\"-nan\"+0,2^63)),(xor(\"-nan\"+0,2^63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift--nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"-nan\"+0,0)),(lshift(\"-nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift--nan-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"-nan\"+0,1)),(lshift(\"-nan\"+0,1))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift--nan-63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"-nan\"+0,63)),(lshift(\"-nan\"+0,63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift--nan-64",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"-nan\"+0,64)),(lshift(\"-nan\"+0,64))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift--nan-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"-nan\"+0,0)),(rshift(\"-nan\"+0,0))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift--nan-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"-nan\"+0,1)),(rshift(\"-nan\"+0,1))}"], [])
            .WithStdOut("4611686018427387904 [0x1p+62]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift--nan-63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"-nan\"+0,63)),(rshift(\"-nan\"+0,63))}"], [])
            .WithStdOut("1 [0x1p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift--nan-64",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"-nan\"+0,64)),(rshift(\"-nan\"+0,64))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-and-+inf-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(and(\"+inf\"+0,0)),(and(\"+inf\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-and-+inf-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(and(\"+inf\"+0,2^63)),(and(\"+inf\"+0,2^63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-or-+inf-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(\"+inf\"+0,0)),(or(\"+inf\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-or-+inf-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(or(\"+inf\"+0,2^63)),(or(\"+inf\"+0,2^63))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-xor-+inf-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(xor(\"+inf\"+0,0)),(xor(\"+inf\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-xor-+inf-2^63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(xor(\"+inf\"+0,2^63)),(xor(\"+inf\"+0,2^63))}"], [])
            .WithStdOut("9223372036854775808 [0x1p+63]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+inf-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+inf\"+0,0)),(lshift(\"+inf\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+inf-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+inf\"+0,1)),(lshift(\"+inf\"+0,1))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+inf-63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+inf\"+0,63)),(lshift(\"+inf\"+0,63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-lshift-+inf-64",
                ["BEGIN{printf \"%.0f [%a]\\n\",(lshift(\"+inf\"+0,64)),(lshift(\"+inf\"+0,64))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+inf-0",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+inf\"+0,0)),(rshift(\"+inf\"+0,0))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+inf-1",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+inf\"+0,1)),(rshift(\"+inf\"+0,1))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+inf-63",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+inf\"+0,63)),(rshift(\"+inf\"+0,63))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-nonfinite-rshift-+inf-64",
                ["BEGIN{printf \"%.0f [%a]\\n\",(rshift(\"+inf\"+0,64)),(rshift(\"+inf\"+0,64))}"], [])
            .WithStdOut("0 [0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-powers-lshift",
                ["BEGIN{for(i=0;i<=65;i++)printf \"%d:%.0f\\n\",i,lshift(1,i)}"], [])
            .WithStdOut("0:1\n1:2\n2:4\n3:8\n4:16\n5:32\n6:64\n7:128\n8:256\n9:512\n10:1024\n11:2048\n12:4096\n13:8192\n14:16384\n15:32768\n16:65536\n17:131072\n18:262144\n19:524288\n20:1048576\n21:2097152\n22:4194304\n23:8388608\n24:16777216\n25:33554432\n26:67108864\n27:134217728\n28:268435456\n29:536870912\n30:1073741824\n31:2147483648\n32:4294967296\n33:8589934592\n34:17179869184\n35:34359738368\n36:68719476736\n37:137438953472\n38:274877906944\n39:549755813888\n40:1099511627776\n41:2199023255552\n42:4398046511104\n43:8796093022208\n44:17592186044416\n45:35184372088832\n46:70368744177664\n47:140737488355328\n48:281474976710656\n49:562949953421312\n50:1125899906842624\n51:2251799813685248\n52:4503599627370496\n53:9007199254740992\n54:18014398509481984\n55:36028797018963968\n56:72057594037927936\n57:144115188075855872\n58:288230376151711744\n59:576460752303423488\n60:1152921504606846976\n61:2305843009213693952\n62:4611686018427387904\n63:9223372036854775808\n64:0\n65:0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("bitwise-powers-rshift",
                ["BEGIN{for(i=0;i<=65;i++)printf \"%d:%.0f\\n\",i,rshift(2^63,i)}"], [])
            .WithStdOut("0:9223372036854775808\n1:4611686018427387904\n2:2305843009213693952\n3:1152921504606846976\n4:576460752303423488\n5:288230376151711744\n6:144115188075855872\n7:72057594037927936\n8:36028797018963968\n9:18014398509481984\n10:9007199254740992\n11:4503599627370496\n12:2251799813685248\n13:1125899906842624\n14:562949953421312\n15:281474976710656\n16:140737488355328\n17:70368744177664\n18:35184372088832\n19:17592186044416\n20:8796093022208\n21:4398046511104\n22:2199023255552\n23:1099511627776\n24:549755813888\n25:274877906944\n26:137438953472\n27:68719476736\n28:34359738368\n29:17179869184\n30:8589934592\n31:4294967296\n32:2147483648\n33:1073741824\n34:536870912\n35:268435456\n36:134217728\n37:67108864\n38:33554432\n39:16777216\n40:8388608\n41:4194304\n42:2097152\n43:1048576\n44:524288\n45:262144\n46:131072\n47:65536\n48:32768\n49:16384\n50:8192\n51:4096\n52:2048\n53:1024\n54:512\n55:256\n56:128\n57:64\n58:32\n59:16\n60:8\n61:4\n62:2\n63:1\n64:0\n65:0\n")
            .Build();

    }
}
