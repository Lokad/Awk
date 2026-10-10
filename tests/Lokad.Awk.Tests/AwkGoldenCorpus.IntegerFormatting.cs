namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> IntegerFormattingCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("integer-minimum",
                ["BEGIN{printf \"[%.5d]\\n\",7;printf \"[%.5i]\\n\",7;printf \"[%.5u]\\n\",7;printf \"[%.5o]\\n\",7;printf \"[%.5x]\\n\",7;printf \"[%.5X]\\n\",7;}"],
                [])
            .WithStdOut("[00007]\n[00007]\n[00007]\n[00007]\n[00007]\n[00007]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-zero",
                ["BEGIN{printf \"[%.0d]\\n\",0;printf \"[%.0i]\\n\",0;printf \"[%.0u]\\n\",0;printf \"[%.0o]\\n\",0;printf \"[%.0x]\\n\",0;printf \"[%.0X]\\n\",0;}"],
                [])
            .WithStdOut("[]\n[]\n[]\n[]\n[]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-fraction-positive",
                ["BEGIN{printf \"[%.0d]\\n\",0.25;printf \"[%.0i]\\n\",0.25;printf \"[%.0u]\\n\",0.25;printf \"[%.0o]\\n\",0.25;printf \"[%.0x]\\n\",0.25;printf \"[%.0X]\\n\",0.25;}"],
                [])
            .WithStdOut("[0]\n[0]\n[0]\n[0]\n[0]\n[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-fraction-negative",
                ["BEGIN{printf \"[%.0d]\\n\",-0.25;printf \"[%.0i]\\n\",-0.25;printf \"[%.0u]\\n\",-0.25;printf \"[%.0o]\\n\",-0.25;printf \"[%.0x]\\n\",-0.25;printf \"[%.0X]\\n\",-0.25;}"],
                [])
            .WithStdOut("[0]\n[0]\n[0]\n[0]\n[0]\n[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-zero-plus",
                ["BEGIN{printf \"[%+.0d]\\n\",0;printf \"[%+.0i]\\n\",0;printf \"[%+.0u]\\n\",0;printf \"[%+.0o]\\n\",0;printf \"[%+.0x]\\n\",0;printf \"[%+.0X]\\n\",0;}"],
                [])
            .WithStdOut("[+]\n[+]\n[]\n[]\n[]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-zero-space",
                ["BEGIN{printf \"[% .0d]\\n\",0;printf \"[% .0i]\\n\",0;printf \"[% .0u]\\n\",0;printf \"[% .0o]\\n\",0;printf \"[% .0x]\\n\",0;printf \"[% .0X]\\n\",0;}"],
                [])
            .WithStdOut("[ ]\n[ ]\n[]\n[]\n[]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-width-zero-ignored",
                ["BEGIN{printf \"[%08.3d]\\n\",7;printf \"[%08.3i]\\n\",7;printf \"[%08.3u]\\n\",7;printf \"[%08.3o]\\n\",7;printf \"[%08.3x]\\n\",7;printf \"[%08.3X]\\n\",7;}"],
                [])
            .WithStdOut("[     007]\n[     007]\n[     007]\n[     007]\n[     007]\n[     007]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-alternate-width",
                ["BEGIN{printf \"[%#08.3d]\\n\",255;printf \"[%#08.3i]\\n\",255;printf \"[%#08.3u]\\n\",255;printf \"[%#08.3o]\\n\",255;printf \"[%#08.3x]\\n\",255;printf \"[%#08.3X]\\n\",255;}"],
                [])
            .WithStdOut("[     255]\n[     255]\n[     255]\n[    0377]\n[   0x0ff]\n[   0X0FF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-alternate-zero",
                ["BEGIN{printf \"[%#.0d]\\n\",0;printf \"[%#.0i]\\n\",0;printf \"[%#.0u]\\n\",0;printf \"[%#.0o]\\n\",0;printf \"[%#.0x]\\n\",0;printf \"[%#.0X]\\n\",0;}"],
                [])
            .WithStdOut("[]\n[]\n[]\n[0]\n[]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-alternate-minimum",
                ["BEGIN{printf \"[%#.5d]\\n\",7;printf \"[%#.5i]\\n\",7;printf \"[%#.5u]\\n\",7;printf \"[%#.5o]\\n\",7;printf \"[%#.5x]\\n\",7;printf \"[%#.5X]\\n\",7;}"],
                [])
            .WithStdOut("[00007]\n[00007]\n[00007]\n[00007]\n[0x00007]\n[0X00007]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-left-justified",
                ["BEGIN{printf \"[%-8.5d]\\n\",-7;printf \"[%-8.5i]\\n\",-7;printf \"[%-8.5u]\\n\",-7;printf \"[%-8.5o]\\n\",-7;printf \"[%-8.5x]\\n\",-7;printf \"[%-8.5X]\\n\",-7;}"],
                [])
            .WithStdOut("[-00007  ]\n[-00007  ]\n[18446744073709551609]\n[1777777777777777777771]\n[fffffffffffffff9]\n[FFFFFFFFFFFFFFF9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-dynamic-minimum",
                ["BEGIN{printf \"[%08.*d]\\n\",5,-7;printf \"[%08.*i]\\n\",5,-7;printf \"[%08.*u]\\n\",5,-7;printf \"[%08.*o]\\n\",5,-7;printf \"[%08.*x]\\n\",5,-7;printf \"[%08.*X]\\n\",5,-7;}"],
                [])
            .WithStdOut("[  -00007]\n[  -00007]\n[18446744073709551609]\n[1777777777777777777771]\n[fffffffffffffff9]\n[FFFFFFFFFFFFFFF9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-dynamic-negative",
                ["BEGIN{printf \"[%08.*d]\\n\",-3,-7;printf \"[%08.*i]\\n\",-3,-7;printf \"[%08.*u]\\n\",-3,-7;printf \"[%08.*o]\\n\",-3,-7;printf \"[%08.*x]\\n\",-3,-7;printf \"[%08.*X]\\n\",-3,-7;}"],
                [])
            .WithStdOut("[-0000007]\n[-0000007]\n[18446744073709551609]\n[1777777777777777777771]\n[fffffffffffffff9]\n[FFFFFFFFFFFFFFF9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-positional-left",
                ["BEGIN{printf \"[%3$#0*1$.*2$d]\\n\",-12,8,255;printf \"[%3$#0*1$.*2$i]\\n\",-12,8,255;printf \"[%3$#0*1$.*2$u]\\n\",-12,8,255;printf \"[%3$#0*1$.*2$o]\\n\",-12,8,255;printf \"[%3$#0*1$.*2$x]\\n\",-12,8,255;printf \"[%3$#0*1$.*2$X]\\n\",-12,8,255;}"],
                [])
            .WithStdOut("[00000255    ]\n[00000255    ]\n[00000255    ]\n[00000377    ]\n[0x000000ff  ]\n[0X000000FF  ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-positional-zero",
                ["BEGIN{printf \"[%3$#0*1$.*2$d]\\n\",12,0,0;printf \"[%3$#0*1$.*2$i]\\n\",12,0,0;printf \"[%3$#0*1$.*2$u]\\n\",12,0,0;printf \"[%3$#0*1$.*2$o]\\n\",12,0,0;printf \"[%3$#0*1$.*2$x]\\n\",12,0,0;printf \"[%3$#0*1$.*2$X]\\n\",12,0,0;}"],
                [])
            .WithStdOut("[            ]\n[            ]\n[            ]\n[           0]\n[            ]\n[            ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-15",
                ["BEGIN{printf \"[%d]\\n\",2^53;printf \"[%i]\\n\",2^53;printf \"[%u]\\n\",2^53;printf \"[%o]\\n\",2^53;printf \"[%x]\\n\",2^53;printf \"[%X]\\n\",2^53;}"],
                [])
            .WithStdOut("[9007199254740992]\n[9007199254740992]\n[9007199254740992]\n[400000000000000000]\n[20000000000000]\n[20000000000000]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-16",
                ["BEGIN{printf \"[%d]\\n\",2^53+2;printf \"[%i]\\n\",2^53+2;printf \"[%u]\\n\",2^53+2;printf \"[%o]\\n\",2^53+2;printf \"[%x]\\n\",2^53+2;printf \"[%X]\\n\",2^53+2;}"],
                [])
            .WithStdOut("[9007199254740994]\n[9007199254740994]\n[9007199254740994]\n[400000000000000002]\n[20000000000002]\n[20000000000002]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-17",
                ["BEGIN{printf \"[%d]\\n\",2^63-1024;printf \"[%i]\\n\",2^63-1024;printf \"[%u]\\n\",2^63-1024;printf \"[%o]\\n\",2^63-1024;printf \"[%x]\\n\",2^63-1024;printf \"[%X]\\n\",2^63-1024;}"],
                [])
            .WithStdOut("[9223372036854774784]\n[9223372036854774784]\n[9223372036854774784]\n[777777777777777776000]\n[7ffffffffffffc00]\n[7FFFFFFFFFFFFC00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-18",
                ["BEGIN{printf \"[%d]\\n\",2^63;printf \"[%i]\\n\",2^63;printf \"[%u]\\n\",2^63;printf \"[%o]\\n\",2^63;printf \"[%x]\\n\",2^63;printf \"[%X]\\n\",2^63;}"],
                [])
            .WithStdOut("[9223372036854775808]\n[9223372036854775808]\n[9223372036854775808]\n[1000000000000000000000]\n[8000000000000000]\n[8000000000000000]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-19",
                ["BEGIN{printf \"[%d]\\n\",2^63+2048;printf \"[%i]\\n\",2^63+2048;printf \"[%u]\\n\",2^63+2048;printf \"[%o]\\n\",2^63+2048;printf \"[%x]\\n\",2^63+2048;printf \"[%X]\\n\",2^63+2048;}"],
                [])
            .WithStdOut("[9223372036854777856]\n[9223372036854777856]\n[9223372036854777856]\n[1000000000000000004000]\n[8000000000000800]\n[8000000000000800]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-20",
                ["BEGIN{printf \"[%d]\\n\",2^64-2048;printf \"[%i]\\n\",2^64-2048;printf \"[%u]\\n\",2^64-2048;printf \"[%o]\\n\",2^64-2048;printf \"[%x]\\n\",2^64-2048;printf \"[%X]\\n\",2^64-2048;}"],
                [])
            .WithStdOut("[18446744073709549568]\n[18446744073709549568]\n[18446744073709549568]\n[1777777777777777774000]\n[fffffffffffff800]\n[FFFFFFFFFFFFF800]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-21",
                ["BEGIN{printf \"[%d]\\n\",2^64;printf \"[%i]\\n\",2^64;printf \"[%u]\\n\",2^64;printf \"[%o]\\n\",2^64;printf \"[%x]\\n\",2^64;printf \"[%X]\\n\",2^64;}"],
                [])
            .WithStdOut("[18446744073709551616]\n[18446744073709551616]\n[1.84467e+19]\n[1.84467e+19]\n[1.84467e+19]\n[1.84467e+19]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-22",
                ["BEGIN{printf \"[%d]\\n\",-2^63;printf \"[%i]\\n\",-2^63;printf \"[%u]\\n\",-2^63;printf \"[%o]\\n\",-2^63;printf \"[%x]\\n\",-2^63;printf \"[%X]\\n\",-2^63;}"],
                [])
            .WithStdOut("[-9223372036854775808]\n[-9223372036854775808]\n[9223372036854775808]\n[1000000000000000000000]\n[8000000000000000]\n[8000000000000000]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-23",
                ["BEGIN{printf \"[%d]\\n\",-2^63-2048;printf \"[%i]\\n\",-2^63-2048;printf \"[%u]\\n\",-2^63-2048;printf \"[%o]\\n\",-2^63-2048;printf \"[%x]\\n\",-2^63-2048;printf \"[%X]\\n\",-2^63-2048;}"],
                [])
            .WithStdOut("[-9223372036854777856]\n[-9223372036854777856]\n[-9.22337e+18]\n[-9.22337e+18]\n[-9.22337e+18]\n[-9.22337e+18]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-24",
                ["BEGIN{printf \"[%d]\\n\",1e20;printf \"[%i]\\n\",1e20;printf \"[%u]\\n\",1e20;printf \"[%o]\\n\",1e20;printf \"[%x]\\n\",1e20;printf \"[%X]\\n\",1e20;}"],
                [])
            .WithStdOut("[100000000000000000000]\n[100000000000000000000]\n[1e+20]\n[1e+20]\n[1e+20]\n[1e+20]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-25",
                ["BEGIN{printf \"[%d]\\n\",-1e20;printf \"[%i]\\n\",-1e20;printf \"[%u]\\n\",-1e20;printf \"[%o]\\n\",-1e20;printf \"[%x]\\n\",-1e20;printf \"[%X]\\n\",-1e20;}"],
                [])
            .WithStdOut("[-100000000000000000000]\n[-100000000000000000000]\n[-1e+20]\n[-1e+20]\n[-1e+20]\n[-1e+20]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-26",
                ["BEGIN{printf \"[%d]\\n\",1e100;printf \"[%i]\\n\",1e100;printf \"[%u]\\n\",1e100;printf \"[%o]\\n\",1e100;printf \"[%x]\\n\",1e100;printf \"[%X]\\n\",1e100;}"],
                [])
            .WithStdOut("[10000000000000000159028911097599180468360808563945281389781327557747838772170381060813469985856815104]\n[10000000000000000159028911097599180468360808563945281389781327557747838772170381060813469985856815104]\n[1e+100]\n[1e+100]\n[1e+100]\n[1e+100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-27",
                ["BEGIN{printf \"[%d]\\n\",-1e100;printf \"[%i]\\n\",-1e100;printf \"[%u]\\n\",-1e100;printf \"[%o]\\n\",-1e100;printf \"[%x]\\n\",-1e100;printf \"[%X]\\n\",-1e100;}"],
                [])
            .WithStdOut("[-10000000000000000159028911097599180468360808563945281389781327557747838772170381060813469985856815104]\n[-10000000000000000159028911097599180468360808563945281389781327557747838772170381060813469985856815104]\n[-1e+100]\n[-1e+100]\n[-1e+100]\n[-1e+100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-range-28",
                ["BEGIN{printf \"[%d]\\n\",1.7976931348623157e308;printf \"[%i]\\n\",1.7976931348623157e308;printf \"[%u]\\n\",1.7976931348623157e308;printf \"[%o]\\n\",1.7976931348623157e308;printf \"[%x]\\n\",1.7976931348623157e308;printf \"[%X]\\n\",1.7976931348623157e308;}"],
                [])
            .WithStdOut("[179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368]\n[179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368]\n[1.79769e+308]\n[1.79769e+308]\n[1.79769e+308]\n[1.79769e+308]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-29",
                ["BEGIN{printf \"[%020d]\\n\",\"+inf\"+0;printf \"[%020i]\\n\",\"+inf\"+0;printf \"[%020u]\\n\",\"+inf\"+0;printf \"[%020o]\\n\",\"+inf\"+0;printf \"[%020x]\\n\",\"+inf\"+0;printf \"[%020X]\\n\",\"+inf\"+0;printf \"[%020a]\\n\",\"+inf\"+0;printf \"[%020A]\\n\",\"+inf\"+0;printf \"[%020f]\\n\",\"+inf\"+0;printf \"[%020F]\\n\",\"+inf\"+0;printf \"[%020g]\\n\",\"+inf\"+0;printf \"[%020G]\\n\",\"+inf\"+0;}"],
                [])
            .WithStdOut("[                +inf]\n[                +inf]\n[                +inf]\n[                +inf]\n[                +inf]\n[                +INF]\n[                +inf]\n[                +INF]\n[                +inf]\n[                +INF]\n[                +inf]\n[                +INF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-30",
                ["BEGIN{printf \"[%020d]\\n\",\"-inf\"+0;printf \"[%020i]\\n\",\"-inf\"+0;printf \"[%020u]\\n\",\"-inf\"+0;printf \"[%020o]\\n\",\"-inf\"+0;printf \"[%020x]\\n\",\"-inf\"+0;printf \"[%020X]\\n\",\"-inf\"+0;printf \"[%020a]\\n\",\"-inf\"+0;printf \"[%020A]\\n\",\"-inf\"+0;printf \"[%020f]\\n\",\"-inf\"+0;printf \"[%020F]\\n\",\"-inf\"+0;printf \"[%020g]\\n\",\"-inf\"+0;printf \"[%020G]\\n\",\"-inf\"+0;}"],
                [])
            .WithStdOut("[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-31",
                ["BEGIN{printf \"[%020d]\\n\",(-1)^0.5;printf \"[%020i]\\n\",(-1)^0.5;printf \"[%020u]\\n\",(-1)^0.5;printf \"[%020o]\\n\",(-1)^0.5;printf \"[%020x]\\n\",(-1)^0.5;printf \"[%020X]\\n\",(-1)^0.5;printf \"[%020a]\\n\",(-1)^0.5;printf \"[%020A]\\n\",(-1)^0.5;printf \"[%020f]\\n\",(-1)^0.5;printf \"[%020F]\\n\",(-1)^0.5;printf \"[%020g]\\n\",(-1)^0.5;printf \"[%020G]\\n\",(-1)^0.5;}"],
                [])
            .WithStdOut("[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-32",
                ["--traditional", "BEGIN{printf \"[%020d]\\n\",\"+inf\"+0;printf \"[%020i]\\n\",\"+inf\"+0;printf \"[%020u]\\n\",\"+inf\"+0;printf \"[%020o]\\n\",\"+inf\"+0;printf \"[%020x]\\n\",\"+inf\"+0;printf \"[%020X]\\n\",\"+inf\"+0;printf \"[%020a]\\n\",\"+inf\"+0;printf \"[%020A]\\n\",\"+inf\"+0;printf \"[%020f]\\n\",\"+inf\"+0;printf \"[%020F]\\n\",\"+inf\"+0;printf \"[%020g]\\n\",\"+inf\"+0;printf \"[%020G]\\n\",\"+inf\"+0;}"],
                [])
            .WithStdOut("[                +inf]\n[                +inf]\n[                +inf]\n[                +inf]\n[                +inf]\n[                +INF]\n[                +inf]\n[                +INF]\n[                +inf]\n[                +INF]\n[                +inf]\n[                +INF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-33",
                ["--traditional", "BEGIN{printf \"[%020d]\\n\",\"-inf\"+0;printf \"[%020i]\\n\",\"-inf\"+0;printf \"[%020u]\\n\",\"-inf\"+0;printf \"[%020o]\\n\",\"-inf\"+0;printf \"[%020x]\\n\",\"-inf\"+0;printf \"[%020X]\\n\",\"-inf\"+0;printf \"[%020a]\\n\",\"-inf\"+0;printf \"[%020A]\\n\",\"-inf\"+0;printf \"[%020f]\\n\",\"-inf\"+0;printf \"[%020F]\\n\",\"-inf\"+0;printf \"[%020g]\\n\",\"-inf\"+0;printf \"[%020G]\\n\",\"-inf\"+0;}"],
                [])
            .WithStdOut("[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-34",
                ["--traditional", "BEGIN{printf \"[%020d]\\n\",(-1)^0.5;printf \"[%020i]\\n\",(-1)^0.5;printf \"[%020u]\\n\",(-1)^0.5;printf \"[%020o]\\n\",(-1)^0.5;printf \"[%020x]\\n\",(-1)^0.5;printf \"[%020X]\\n\",(-1)^0.5;printf \"[%020a]\\n\",(-1)^0.5;printf \"[%020A]\\n\",(-1)^0.5;printf \"[%020f]\\n\",(-1)^0.5;printf \"[%020F]\\n\",(-1)^0.5;printf \"[%020g]\\n\",(-1)^0.5;printf \"[%020G]\\n\",(-1)^0.5;}"],
                [])
            .WithStdOut("[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-35",
                ["--posix", "BEGIN{printf \"[%020d]\\n\",\"+inf\"+0;printf \"[%020i]\\n\",\"+inf\"+0;printf \"[%020u]\\n\",\"+inf\"+0;printf \"[%020o]\\n\",\"+inf\"+0;printf \"[%020x]\\n\",\"+inf\"+0;printf \"[%020X]\\n\",\"+inf\"+0;printf \"[%020a]\\n\",\"+inf\"+0;printf \"[%020A]\\n\",\"+inf\"+0;printf \"[%020f]\\n\",\"+inf\"+0;printf \"[%020F]\\n\",\"+inf\"+0;printf \"[%020g]\\n\",\"+inf\"+0;printf \"[%020G]\\n\",\"+inf\"+0;}"],
                [])
            .WithStdOut("[                 inf]\n[                 inf]\n[                 inf]\n[                 inf]\n[                 inf]\n[                 inf]\n[                 inf]\n[                 INF]\n[                 inf]\n[                 INF]\n[                 inf]\n[                 INF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-36",
                ["--posix", "BEGIN{printf \"[%020d]\\n\",\"-inf\"+0;printf \"[%020i]\\n\",\"-inf\"+0;printf \"[%020u]\\n\",\"-inf\"+0;printf \"[%020o]\\n\",\"-inf\"+0;printf \"[%020x]\\n\",\"-inf\"+0;printf \"[%020X]\\n\",\"-inf\"+0;printf \"[%020a]\\n\",\"-inf\"+0;printf \"[%020A]\\n\",\"-inf\"+0;printf \"[%020f]\\n\",\"-inf\"+0;printf \"[%020F]\\n\",\"-inf\"+0;printf \"[%020g]\\n\",\"-inf\"+0;printf \"[%020G]\\n\",\"-inf\"+0;}"],
                [])
            .WithStdOut("[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n[                -inf]\n[                -INF]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-nonfinite-37",
                ["--posix", "BEGIN{printf \"[%020d]\\n\",(-1)^0.5;printf \"[%020i]\\n\",(-1)^0.5;printf \"[%020u]\\n\",(-1)^0.5;printf \"[%020o]\\n\",(-1)^0.5;printf \"[%020x]\\n\",(-1)^0.5;printf \"[%020X]\\n\",(-1)^0.5;printf \"[%020a]\\n\",(-1)^0.5;printf \"[%020A]\\n\",(-1)^0.5;printf \"[%020f]\\n\",(-1)^0.5;printf \"[%020F]\\n\",(-1)^0.5;printf \"[%020g]\\n\",(-1)^0.5;printf \"[%020G]\\n\",(-1)^0.5;}"],
                [])
            .WithStdOut("[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n[                -nan]\n[                -NAN]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-wide-38",
                ["BEGIN{printf \"[%.308u]\\n\",1.7976931348623157e308;printf \"[%.308o]\\n\",1.7976931348623157e308;printf \"[%.308x]\\n\",1.7976931348623157e308;printf \"[%.308X]\\n\",1.7976931348623157e308;}"],
                [])
            .WithStdOut("[1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n[1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n[1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n[1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-wide-39",
                ["BEGIN{printf \"[%.308u]\\n\",-1.7976931348623157e308;printf \"[%.308o]\\n\",-1.7976931348623157e308;printf \"[%.308x]\\n\",-1.7976931348623157e308;printf \"[%.308X]\\n\",-1.7976931348623157e308;}"],
                [])
            .WithStdOut("[-1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n[-1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n[-1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n[-1.7976931348623157081452742373170435679807056752584499659891747680315726078002853876058955863276687817154045895351438246423432132688946418276846754670353751698604991057655128207624549009038932894407586850845513394230458323690322294816580855933212334827479782620414472316873817718091929988125040402618412485837e+308]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-wide-40",
                ["BEGIN{printf \"[%.1000u]\\n\",1.7976931348623157e308;printf \"[%.1000o]\\n\",1.7976931348623157e308;printf \"[%.1000x]\\n\",1.7976931348623157e308;printf \"[%.1000X]\\n\",1.7976931348623157e308;}"],
                [])
            .WithStdOut("[179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368]\n[179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368]\n[179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368]\n[179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-wide-41",
                ["BEGIN{printf \"[%#.0u]\\n\",1.5e20;printf \"[%#.0o]\\n\",1.5e20;printf \"[%#.0x]\\n\",1.5e20;printf \"[%#.0X]\\n\",1.5e20;}"],
                [])
            .WithStdOut("[2e+20]\n[2.e+20]\n[2.e+20]\n[2.e+20]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("integer-wide-42",
                ["BEGIN{printf \"[%#.1u]\\n\",2.5e20;printf \"[%#.1o]\\n\",2.5e20;printf \"[%#.1x]\\n\",2.5e20;printf \"[%#.1X]\\n\",2.5e20;}"],
                [])
            .WithStdOut("[2e+20]\n[2.e+20]\n[2.e+20]\n[2.e+20]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("original-precision-repro",
                ["BEGIN{printf \"[%.5d] [%.0d] [%08.5d]\\n\",2,0,2}"],
                [])
            .WithStdOut("[00002] [] [   00002]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("original-range-repro",
                ["BEGIN{printf \"%d %u %x\\n\",1e20,1e20,1e20}"],
                [])
            .WithStdOut("100000000000000000000 1e+20 1e+20\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sprintf-precision",
                ["BEGIN{print sprintf(\"[%3$#0*1$.*2$x]\",12,5,255)}"],
                [])
            .WithStdOut("[     0x000ff]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("general-zero-precision",
                ["BEGIN{printf \"[%.0g][%.0G][%#.0g]\\n\",1.25,1.25,1.25}"],
                [])
            .WithStdOut("[1][1][1.]\n")
            .Build();

    }
}
