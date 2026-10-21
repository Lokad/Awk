using System;
using System.Collections.Generic;

namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    public static IReadOnlyList<AwkGoldenCase> DirectCases { get; }

    static AwkGoldenCorpus()
    {
        DirectCases =
        [
        .. Tagged(VariableCases(), AwkGoldenTag.Variable),

        .. Tagged(FieldCases(), AwkGoldenTag.Field),
        .. Tagged(FieldLengthCases(), AwkGoldenTag.Field, AwkGoldenTag.Expression),
        .. Tagged(FieldProjectionCases(), AwkGoldenTag.Field, AwkGoldenTag.Expression, AwkGoldenTag.Io),
        .. Tagged(FieldLiteralComparisonCases(), AwkGoldenTag.Field, AwkGoldenTag.Expression),
        .. Tagged(FieldValueComparisonCases(), AwkGoldenTag.Field, AwkGoldenTag.Expression),
        .. Tagged(BinaryNumericCases(), AwkGoldenTag.Field, AwkGoldenTag.Expression),
        .. Tagged(NumericFieldConsumptionCases(), AwkGoldenTag.Field, AwkGoldenTag.Expression),

        .. Tagged(FieldCompatibilityRepairs(), AwkGoldenTag.Field),
        .. Tagged(RecordOwnershipCases(), AwkGoldenTag.Field, AwkGoldenTag.Record),

        .. Tagged(FieldCountValueCompatibilityRepairs(), AwkGoldenTag.Field, AwkGoldenTag.Variable),

        .. Tagged(RecordSplitCompatibilityRepairs(), AwkGoldenTag.Field, AwkGoldenTag.Record, AwkGoldenTag.Regex),

        .. Tagged(RecordSchedulingCases(), AwkGoldenTag.Record),

        .. Tagged(InputMetadataCompatibilityRepairs(), AwkGoldenTag.Record),

        .. Tagged(RecordSeparatorCompatibilityRepairs(), AwkGoldenTag.Record),

        .. Tagged(CommandLineRuntimeCases(), AwkGoldenTag.CommandLine),

        .. Tagged(CompatibilityContractCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Io, AwkGoldenTag.Record),

        .. Tagged(OptionCompatibilityCases(), AwkGoldenTag.CommandLine),

        .. Tagged(ArgumentSchedulingCompatibilityRepairs(), AwkGoldenTag.CommandLine, AwkGoldenTag.Record),

        .. Tagged(PatternCases(), AwkGoldenTag.Pattern),

        .. Tagged(RegexSubstitutionCases(), AwkGoldenTag.Regex),

        .. Tagged(ExpressionCases(), AwkGoldenTag.Expression),

        .. Tagged(BenchmarkCoreCases(), AwkGoldenTag.Expression, AwkGoldenTag.Function, AwkGoldenTag.Regex),

        .. Tagged(ExpressionCompatibilityRepairs(), AwkGoldenTag.Expression),

        .. Tagged(FrontendWarningCases(), AwkGoldenTag.Expression),

        .. Tagged(StringEscapeWarningCases(), AwkGoldenTag.Expression, AwkGoldenTag.CommandLine),

        .. Tagged(UnicodeStringCases(), AwkGoldenTag.Expression),

        .. Tagged(UnicodeRegexCases(), AwkGoldenTag.Expression, AwkGoldenTag.Regex),
        .. Tagged(SpecialRegexAssignmentCases(), AwkGoldenTag.Variable, AwkGoldenTag.CommandLine, AwkGoldenTag.Regex),
        .. Tagged(UnicodeCallerCases(), AwkGoldenTag.CommandLine),

        .. Tagged(AssignmentOperandCompatibilityRepairs(), AwkGoldenTag.Expression),

        .. Tagged(ExpressionRuntimeCases(), AwkGoldenTag.Expression),

        .. Tagged(
            CompilerDiagnostics(
                DiagnosticCases(),
                "invalid-delete-scalar-fails",
                "invalid-delete-scalar-element-fails",
                "builtin-scalar-as-array-fails",
                "bindtextdomain-function-unsupported",
                "dcgettext-function-unsupported",
                "dcngettext-function-unsupported",
                "internationalized-string-marker-in-string",
                "ambiguous-string-escape-warnings"),
            AwkGoldenTag.Diagnostic),

        .. Tagged(BuiltinFunctionCases(), AwkGoldenTag.Function),
        .. Tagged(StringResultCases(), AwkGoldenTag.Function, AwkGoldenTag.Expression),

        .. Tagged(IoCases(), AwkGoldenTag.Io),

        .. Tagged(FlushCompatibilityCases(), AwkGoldenTag.Io, AwkGoldenTag.Function),

        .. Tagged(ProfileNameCompatibilityCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Function, AwkGoldenTag.Variable),

        .. Tagged(FieldModeCompatibilityCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Variable, AwkGoldenTag.Regex),
        .. Tagged(RegexProfileCompatibilityCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Regex),
        .. Tagged(RegexEscapeWarningCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Regex),
        .. Tagged(RegexEscapeWarningSurveyCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Regex),
        .. Tagged(RegexBracketClassCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Regex),
        .. Tagged(NumericByteEscapeCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Regex, AwkGoldenTag.Variable),
        .. Tagged(UnicodePrintClassCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.Regex),
        .. Tagged(RangeParagraphCompatibilityCases(), AwkGoldenTag.Pattern, AwkGoldenTag.Record, AwkGoldenTag.Field, AwkGoldenTag.Regex),
        .. Tagged(ParagraphEofCompatibilityCases(), AwkGoldenTag.Record, AwkGoldenTag.Field, AwkGoldenTag.Regex),
        .. Tagged(GetlineExpressionCompatibilityCases(), AwkGoldenTag.Io, AwkGoldenTag.Expression, AwkGoldenTag.Record, AwkGoldenTag.Field),
        .. Tagged(GetlineDestinationCompatibilityCases(), AwkGoldenTag.Io, AwkGoldenTag.Expression, AwkGoldenTag.Record, AwkGoldenTag.Field),
        .. Tagged(GetlinePostfixCompatibilityCases(), AwkGoldenTag.Io, AwkGoldenTag.Expression, AwkGoldenTag.Record, AwkGoldenTag.Field),
        .. Tagged(GetlineFailureCompatibilityCases(), AwkGoldenTag.Io, AwkGoldenTag.Expression, AwkGoldenTag.Record, AwkGoldenTag.Field),
        .. Tagged(ParagraphPipeEofCompatibilityCases(), AwkGoldenTag.Io, AwkGoldenTag.Expression, AwkGoldenTag.Record, AwkGoldenTag.Field),

        .. Tagged(HostPolicyCases(), AwkGoldenTag.HostPolicy),

        .. Tagged(RegexCases(), AwkGoldenTag.Regex),

        .. Tagged(RegexCompatibilityRepairs(), AwkGoldenTag.Regex),

        .. Tagged(LongestRegexCompatibilityRepairs(), AwkGoldenTag.Regex),

        .. Tagged(OutputCases(), AwkGoldenTag.Output),

        .. Tagged(NumericOutputCompatibilityRepairs(), AwkGoldenTag.Output),

        .. Tagged(ArrayRuntimeCases(), AwkGoldenTag.Array),

        .. Tagged(CompilerDiagnostics(StatementControlCases().Where(testCase =>
            testCase.Name is "posix-switch-is-gnu-extension" or "traditional-switch-is-gnu-extension")), AwkGoldenTag.Statement),
        .. Tagged(StatementControlCases().Where(testCase =>
            testCase.Name is not ("posix-switch-is-gnu-extension" or "traditional-switch-is-gnu-extension")), AwkGoldenTag.Statement),

        .. Tagged(StatementCompatibilityRepairs(), AwkGoldenTag.Statement),

        .. Tagged(FunctionFlowCompatibilityRepairs(), AwkGoldenTag.Function, AwkGoldenTag.Statement, AwkGoldenTag.Record, AwkGoldenTag.Regex),

        .. Tagged(ExponentiationAliasCases(), AwkGoldenTag.Expression, AwkGoldenTag.Statement),

        .. Tagged(UnaryPrefixCases(), AwkGoldenTag.Expression, AwkGoldenTag.Statement),

        .. Tagged(NumericSemanticsCases(), AwkGoldenTag.Expression, AwkGoldenTag.Function, AwkGoldenTag.Field),

        .. Tagged(StrtonumConversionCases(), AwkGoldenTag.Expression, AwkGoldenTag.Function, AwkGoldenTag.Field),
        .. Tagged(BitwiseCompatibilityCases(), AwkGoldenTag.Expression, AwkGoldenTag.Function),
        .. Tagged(SortingCompatibilityCases(), AwkGoldenTag.Array, AwkGoldenTag.Function),
        .. Tagged(IgnoreCaseCompatibilityCases(), AwkGoldenTag.Array, AwkGoldenTag.Function),
        .. Tagged(GrammarBoundaryCases(), AwkGoldenTag.Expression, AwkGoldenTag.Statement),
        .. Tagged(SwitchLabelCases(), AwkGoldenTag.Expression, AwkGoldenTag.Statement),
        .. Tagged(NumericFieldConversionCases(), AwkGoldenTag.Field, AwkGoldenTag.Variable),
        .. Tagged(SplitSeparatorCaptureCases(), AwkGoldenTag.Field, AwkGoldenTag.Array, AwkGoldenTag.Function),
        .. Tagged(SpecialArrayReadCases(), AwkGoldenTag.Array, AwkGoldenTag.Function),
        .. Tagged(UntypedStateCases(), AwkGoldenTag.Array, AwkGoldenTag.Function),
        .. Tagged(CalendarNormalizationCases(), AwkGoldenTag.Function),

        .. Tagged(FormattingSyntaxCases(), AwkGoldenTag.Expression, AwkGoldenTag.Statement),
        .. Tagged(PrintfByteTextCases(), AwkGoldenTag.Expression, AwkGoldenTag.Statement),

        .. Tagged(IntegerFormattingCases(), AwkGoldenTag.Output, AwkGoldenTag.Expression),

        .. Tagged(StatementSeparatorCases(), AwkGoldenTag.Statement),

        .. Tagged(StatementRuntimeCases(), AwkGoldenTag.Statement),

        .. Tagged(AgentWorkflowCases(), AwkGoldenTag.AgentWorkflow),

        .. Tagged(ArrayCases(), AwkGoldenTag.Array),

        .. Tagged(CommandLineSourceLoadingCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.SourceLoading),

        .. Tagged(CompatibilityModeCases(), AwkGoldenTag.CompatibilityMode),

        .. Tagged(UserFunctionCases(), AwkGoldenTag.UserFunction, AwkGoldenTag.Function),

        .. Tagged(FunctionSyntaxCases(), AwkGoldenTag.UserFunction, AwkGoldenTag.Function),

        .. Tagged(MainInputIoCases(), AwkGoldenTag.Io, AwkGoldenTag.Record),

        .. Tagged(
            CompilerDiagnostics(
                SyntaxDiagnosticCases(),
                "trailing-print-redirection-token-fails"),
            AwkGoldenTag.Diagnostic)
        ];
        static AwkGoldenCase[] AgentWorkflowCases() =>
        [
            Passing(
            "inline-escaped-field-separator-header-map",
            ["-F\\t", "NR==1{for(i=1;i<=NF;i++)h[$i]=i;next}{status=$(h[\"Status\"]);cnt[status]++} END{print cnt[\"OK\"], cnt[\"KO\"]}", "/file.tsv"],
            [File("/file.tsv", "Status\tType\nOK\tA\nKO\tB\nOK\tC\n")],
            "2 1\n"),

        Passing(
            "subsep-split-asorti-newline-statements",
            ["-F\\t", """
                {
                  key=$2
                  seen[$1 SUBSEP key]=1
                  keys[key]=1
                  arr[++n]=$3
                }
                END{
                  # mirrors production scripts that use comments inside multiline awk blocks
                  cross=0
                  for(k in keys){
                    count=0
                    for(p in seen){ split(p,a,SUBSEP); if(a[2]==k) count++ }
                    if(count>1) cross++
                  }
                  asorti(arr, sorted, "@val_num_asc")
                  print "cross=" cross
                  print arr[sorted[1]], arr[sorted[n]]
                }
                """, "/file.tsv"],
            [File("/file.tsv", "A\tK\t3\nB\tK\t1\nA\tL\t2\n")],
            "cross=1\n1 3\n")
        ];
        static AwkGoldenCase[] ArrayRuntimeCases() =>
        [
            Passing(
            "associative-array-count",
            ["{ext=\"(noext)\"; if(match($0,/\\.([^.]+)$/,m)){ext=m[1]} counts[ext]++} END{print counts[\"csv\"], counts[\"(noext)\"], counts[\"gz\"]}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "1 1 1\n"),

        PassingUnorderedStdOut(
            "associative-array-iteration",
            ["{ext=\"(noext)\"; if(match($0,/\\.([^.]+)$/,m)){ext=m[1]} counts[ext]++} END{for(ext in counts){print ext, counts[ext]}}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "csv 1\n(noext) 1\ngz 1\n"),

        Passing(
            "procinfo-sorted-in-index-order",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=10; PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}"],
            [],
            "a 10\nb 2\n"),

        Passing(
            "procinfo-sorted-in-value-order",
            ["BEGIN{a[\"x\"]=2; a[\"y\"]=10; PROCINFO[\"sorted_in\"]=\"@val_num_desc\"; for(k in a) print k, a[k]}"],
            [],
            "y 10\nx 2\n"),

        Failing(
            "procinfo-sorted-in-unknown-mode-fails",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; PROCINFO[\"sorted_in\"]=\"@bogus\"; for(k in a) print k}"],
            [],
            2,
            string.Empty,
            "awk: sort comparison function is not defined: @bogus\n"),

        Passing(
            "array-membership-condition",
            ["{if($1 in seen){next}else{seen[$1]=1; print $1}}", "/file.txt"],
            [File("/file.txt", "a 1\nb 2\na 3\n")],
            "a\nb\n"),

        Passing(
            "multidimensional-array-subsep",
            ["BEGIN{a[\"x\",\"y\"]=7; print a[\"x\",\"y\"]; if((\"x\",\"y\") in a) print \"present\"; SUBSEP=\"|\"; a[\"x\",\"y\"]=9; print a[\"x|y\"]; delete a[\"x\",\"y\"]; if((\"x\",\"y\") in a) print \"bad\"; else print \"deleted\"}"],
            [],
            "7\npresent\n9\ndeleted\n"),

        Passing(
            "valid-utf8-array-keys-and-subsep-composition",
            ["BEGIN{a[\"café\"]=7; print (\"café\" in a), a[\"café\"]; SUBSEP=\"é\"; a[\"caf\",\"\"]=9; print a[\"café\"]}"],
            [],
            "1 7\n9\n"),

        Passing(
            "split-array-values-preserve-numeric-string-comparison",
            ["BEGIN{split(\"00\", a); print typeof(a[1]), (a[1]==0), (a[1]==\"00\"), (a[1]?1:0)}"],
            [],
            "strnum 1 1 0\n"),

        CompilerDiagnostic(Failing(
            "arrays-of-arrays-unsupported",
            ["BEGIN{a[1][2]=3}"],
            [],
            2,
            string.Empty,
            "awk: arrays of arrays are unsupported in Lokad awk\n")),

        Passing(
            "arrays-of-arrays-token-in-string",
            ["BEGIN{print \"a[1][2]\"}"],
            [],
            "a[1][2]\n"),

        Passing(
            "delete-array-element",
            ["{a[$1]=$2} END{delete a[\"x\"]; for(k in a) print k, a[k]}", "/file.txt"],
            [File("/file.txt", "x 1\ny 2\n")],
            "y 2\n"),

        Passing(
            "delete-whole-array",
            ["{a[$1]=$2} END{delete a; for(k in a) print k; print \"done\"}", "/file.txt"],
            [File("/file.txt", "x 1\ny 2\n")],
            "done\n"),

        Passing(
            "asort-values-numeric-ascending",
            ["BEGIN{a[\"x\"]=3; a[\"y\"]=1; a[\"z\"]=2; asort(a,b,\"@val_num_asc\"); print b[1], b[2], b[3]}"],
            [],
            "1 2 3\n"),

        Passing(
            "asort-values-numeric-descending",
            ["BEGIN{a[\"x\"]=3; a[\"y\"]=10; a[\"z\"]=2; asort(a,b,\"@val_num_desc\"); print b[1], b[2], b[3]}"],
            [],
            "10 3 2\n"),

        Passing(
            "asort-preserves-numeric-types-and-precision",
            ["BEGIN{a[\"zero\"]=0; a[\"value\"]=1.23456789012345; asort(a,b,\"@val_num_asc\"); printf \"%s %.15g %s %s\\n\",typeof(b[1]),b[2],typeof(b[2]),b[1]?\"true\":\"false\"}"],
            [],
            "number 1.23456789012345 number false\n"),

        Passing(
            "asort-values-string-descending",
            ["BEGIN{a[\"x\"]=\"aa\"; a[\"y\"]=\"b\"; a[\"z\"]=\"c\"; asort(a,b,\"@val_str_desc\"); print b[1], b[2], b[3]}"],
            [],
            "c b aa\n"),

        Passing(
            "asort-values-string-ascending",
            ["BEGIN{a[\"x\"]=\"aa\"; a[\"y\"]=\"b\"; a[\"z\"]=\"c\"; asort(a,b,\"@val_str_asc\"); print b[1], b[2], b[3]}"],
            [],
            "aa b c\n"),

        Passing(
            "asort-index-numeric-ascending",
            ["BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asort(a,b,\"@ind_num_asc\"); print n, b[1], b[2]}"],
            [],
            "2 y x\n"),

        Passing(
            "asort-expression-in-place-return-value",
            ["BEGIN{a[3]=\"c\"; a[1]=\"a\"; a[2]=\"b\"; n=asort(a); print n, a[1], a[2], a[3]}"],
            [],
            "3 a b c\n"),

        Passing(
            "asorti-expression-destination-return-value",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; n=asorti(a,k); print n, k[1], k[2]}"],
            [],
            "2 a b\n"),

        Passing(
            "asorti-index-numeric-descending",
            ["BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asorti(a,k,\"@ind_num_desc\"); print n, k[1], k[2]}"],
            [],
            "2 10 2\n"),

        Passing(
            "asorti-index-string-descending",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; n=asorti(a,k,\"@ind_str_desc\"); print n, k[1], k[2]}"],
            [],
            "2 b a\n"),

        Passing(
            "asorti-value-string-ascending",
            ["BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asorti(a,k,\"@val_str_asc\"); print n, k[1], k[2]}"],
            [],
            "2 10 2\n"),

        Passing(
            "asorti-expression-in-place-return-value",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; n=asorti(a); print n, a[1], a[2]}"],
            [],
            "2 a b\n"),

        Passing(
            "asort-user-comparator-by-value-length",
            ["function cmp(i1,v1,i2,v2){return length(v1)-length(v2)} BEGIN{a[\"x\"]=\"bbb\"; a[\"y\"]=\"a\"; a[\"z\"]=\"cc\"; n=asort(a,b,\"cmp\"); print n, b[1], b[2], b[3]}"],
            [],
            "3 a cc bbb\n"),

        Passing(
            "asort-user-comparator-receives-precise-values",
            ["function cmp(i1,v1,i2,v2){return v1-v2} BEGIN{a[\"high\"]=1.234574; a[\"low\"]=1.234565; asort(a,b,\"cmp\"); printf \"%.15g %.15g\\n\",b[1],b[2]}"],
            [],
            "1.234565 1.234574\n"),

        Passing(
            "asorti-user-comparator-by-numeric-index",
            ["function cmp(i1,v1,i2,v2){return i2-i1} BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asorti(a,k,\"cmp\"); print n, k[1], k[2]}"],
            [],
            "2 10 2\n"),

        Passing(
            "asort-user-comparator-has-logarithmic-comparison-bound",
            ["function cmp(i1,v1,i2,v2){calls++; return v1-v2} BEGIN{for(i=128;i>=1;i--)a[i]=i; n=asort(a,b,\"cmp\"); if(calls>896) exit 9; print n, b[1], b[n]}"],
            [],
            "128 1 128\n"),

        Passing(
            "asort-comparator-statement-body",
            ["function cmp(i1,v1,i2,v2, d){d=length(v1)-length(v2); if(d) return d; return i1-i2} BEGIN{a[2]=\"bb\"; a[1]=\"a\"; a[3]=\"cc\"; n=asort(a,b,\"cmp\"); print n, b[1], b[2], b[3]}"],
            [],
            "3 a bb cc\n"),

        Failing(
            "asort-unknown-mode-fails",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; asort(a,b,\"@bogus\")}"],
            [],
            2,
            string.Empty,
            "awk: sort comparison function is not defined: @bogus\n"),

        Failing(
            "asort-scalar-source-fails",
            ["BEGIN{x=1; asort(x)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `x' as an array\n"),

        Failing(
            "asort-scalar-destination-fails",
            ["BEGIN{a[1]=1; b=2; asort(a,b)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `b' as an array\n"),

        Failing(
            "asorti-scalar-source-fails",
            ["BEGIN{x=1; asorti(x)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `x' as an array\n"),

        Failing(
            "asort-order-array-fails",
            ["BEGIN{a[1]=1; order[1]=1; asort(a,b,order)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `order' in a scalar context\n")
        ];
        static IEnumerable<AwkGoldenCase> ArrayCases()
        {
            yield return Posix(Passing(
                "split-defaults-to-fs",
                ["BEGIN{FS=\":\"} {split($0,a); print a[1], a[2]}", "/file.txt"],
                [File("/file.txt", "a:b\n")],
                "a b\n"));

            yield return Passing(
                "patsplit-statement",
                ["{patsplit($0,a,\"[0-9]+\"); print a[1], a[2]}", "/file.txt"],
                [File("/file.txt", "a12b345c\n")],
                "12 345\n");

            yield return Passing(
                "patsplit-defaults-to-fpat",
                ["BEGIN{FPAT=\"[A-Z]+|[0-9]+\"; n=patsplit(\"A-12-B\", a); print n, a[1], a[2], a[3]}"],
                [],
                "3 A 12 B\n");

            yield return Passing(
                "patsplit-default-fpat-whitespace",
                ["BEGIN{n=patsplit(\"a b\", a); print n, a[1], a[2]}"],
                [],
                "2 a b\n");

            yield return Posix(Passing(
                "array-membership-expression",
                ["BEGIN{a[\"x\"]=7; key=\"x\"; print (key in a), (\"y\" in a), ((\"x\" in a) ? \"yes\" : \"no\")}"],
                [],
                "1 0 yes\n"));

            yield return Passing(
                "expression-lists-in-subscripts-and-function-arguments",
                ["function sum(a,b,c){return a+b+c} BEGIN{a[1,2,3]=9; print a[1 SUBSEP 2 SUBSEP 3], sum(1,2,3)}"],
                [],
                "9 6\n");

            yield return Passing(
                "array-value-read-autovivifies",
                ["BEGIN{print (\"x\" in a); value=a[\"x\"]; print (\"x\" in a), length(a), \"[\" a[\"x\"] \"]\"}"],
                [],
                "0\n1 1 []\n");

            yield return Passing(
                "array-membership-does-not-autovivify",
                ["BEGIN{print (\"x\" in a); print length(a)}"],
                [],
                "0\n0\n");

            yield return Failing(
                "length-untyped-name-creates-scalar-context",
                ["BEGIN{print length(a); a[1]=1}"],
                [],
                2,
                "0\n",
                "awk: attempt to use scalar `a' as an array\n");

            yield return Passing(
                "length-missing-array-element-autovivifies",
                ["BEGIN{a[1]=1; print length(a[2]); print length(a)}"],
                [],
                "0\n2\n");

            yield return Failing(
                "length-scalar-as-array-element-fails",
                ["BEGIN{a=1; print length(a[1])}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use scalar `a' as an array\n");

            yield return Passing(
                "posix-2024-length-array-supported",
                ["--posix", "BEGIN{a[1]=1; print length(a)}"],
                [],
                "1\n");

            yield return Passing(
                "traditional-length-array-remains-supported",
                ["--traditional", "BEGIN{a[1]=1; print length(a)}"],
                [],
                "1\n");

            yield return Passing(
                "split-expression-return-value-and-regex-separator",
                ["BEGIN{n=split(\"a,b:c\", a, /[,:]/); print n, a[1], a[2], a[3]}"],
                [],
                "3 a b c\n");

            yield return Passing(
                "split-regex-captures-do-not-become-fields",
                ["BEGIN{n=split(\"a1b2c\", a, /([0-9])/); print n; for(i=1;i<=n;i++) print \"[\" a[i] \"]\"}"],
                [],
                "3\n[a]\n[b]\n[c]\n");

            yield return Passing(
                "split-regex-separator-array-return-value",
                ["BEGIN{n=split(\"::a:b::\", a, /:+/, s); print n; for(i=0;i<=n;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=n;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
                [],
                "4\ns[0]=[]\ns[1]=[::]\ns[2]=[:]\ns[3]=[::]\ns[4]=[]\na[1]=[]\na[2]=[a]\na[3]=[b]\na[4]=[]\n");

            yield return Passing(
                "split-default-whitespace-separator-array",
                ["BEGIN{split(\"  a  b \", a, \" \", s); for(i=0;i<=2;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=2;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
                [],
                "s[0]=[  ]\ns[1]=[  ]\ns[2]=[ ]\na[1]=[a]\na[2]=[b]\n");

            yield return Passing(
                "split-empty-separator-array",
                ["BEGIN{n=split(\"abc\", a, \"\", s); print n; for(i=0;i<=n;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=n;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
                [],
                "3\ns[0]=[]\ns[1]=[]\ns[2]=[]\ns[3]=[]\na[1]=[a]\na[2]=[b]\na[3]=[c]\n");

            yield return CompilerDiagnostic(Failing(
                "split-destination-expression-fails",
                ["BEGIN{split(\"abc\", 1+2)}"],
                [],
                2,
                string.Empty,
                "awk: argument 2 of `split' must be an array name.\n"));

            yield return Failing(
                "split-destination-scalar-fails",
                ["BEGIN{a=1; split(\"abc\", a)}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use scalar `a' as an array\n");

            yield return Passing(
                "patsplit-expression-return-value",
                ["BEGIN{n=patsplit(\"a12b345\", a, /[0-9]+/); print n, a[1], a[2]}"],
                [],
                "2 12 345\n");

            yield return Passing(
                "patsplit-separator-array-return-value",
                ["BEGIN{n=patsplit(\"aa-123--bb\", a, /[A-Za-z0-9]+/, s); print n; for(i=0;i<=n;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=n;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
                [],
                "3\ns[0]=[]\ns[1]=[-]\ns[2]=[--]\ns[3]=[]\na[1]=[aa]\na[2]=[123]\na[3]=[bb]\n");

            yield return Failing(
                "patsplit-separator-array-must-be-distinct",
                ["BEGIN{patsplit(\"a12b\", a, /[0-9]+/, a)}"],
                [],
                2,
                string.Empty,
                "awk: split separator array must be distinct from the destination array\n");

            yield return CompilerDiagnostic(Failing(
                "patsplit-destination-expression-fails",
                ["BEGIN{patsplit(\"abc\", 1+2, /[a-z]/)}"],
                [],
                2,
                string.Empty,
                "awk: argument 2 of `patsplit' must be an array name.\n"));

            yield return Failing(
                "patsplit-destination-scalar-fails",
                ["BEGIN{a=1; patsplit(\"abc\", a, /[a-z]/)}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use scalar `a' as an array\n");

            yield return Failing(
                "patsplit-default-fpat-invalid-regex-fails",
                ["BEGIN{FPAT=\"[\"; patsplit(\"abc\", a)}"],
                [],
                2,
                string.Empty,
                "awk: invalid regular expression\n");

            yield return Failing(
                "patsplit-explicit-invalid-regex-fails",
                ["BEGIN{patsplit(\"abc\", a, \"[\")}"],
                [],
                2,
                string.Empty,
                "awk: invalid regular expression\n");

            yield return Failing(
                "patsplit-array-pattern-fails",
                ["BEGIN{p[1]=1; patsplit(\"abc\", a, p)}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use array `p' in a scalar context\n");

            yield return Failing(
                "split-separator-array-must-be-distinct",
                ["BEGIN{split(\"a:b\", a, \":\", a)}"],
                [],
                2,
                string.Empty,
                "awk: split separator array must be distinct from the destination array\n");

            yield return Failing(
                "split-default-fs-invalid-regex-fails",
                ["BEGIN{FS=\"[+\"; split(\"abc\", a)}"],
                [],
                2,
                string.Empty,
                "awk: invalid regular expression\n");

            yield return Failing(
                "split-explicit-invalid-regex-fails",
                ["BEGIN{split(\"abc\", a, \"[+\")}"],
                [],
                2,
                string.Empty,
                "awk: invalid regular expression\n");

            yield return Failing(
                "split-array-separator-fails",
                ["BEGIN{s[1]=1; split(\"abc\", a, s)}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use array `s' in a scalar context\n");

            yield return PassingUnorderedStdOut(
                "single-statement-for-in-without-braces",
                ["{name=$0; ext=\"\"; if (match(name,/\\.([^.]+)$/ ,m)) ext=m[1]; else ext=\"(noext)\"; cnt[ext]++} END{for(e in cnt) print e, cnt[e]}", "/file.txt"],
                [File("/file.txt", "report.csv\nREADME\n")],
                "csv 1\n(noext) 1\n");
        }
        static AwkGoldenCase[] CommandLineRuntimeCases() =>
        [
            Passing(
            "argc-argv-and-argind-for-files",
            ["BEGIN{print ARGC, ARGV[0], ARGV[1], ARGV[2]} {print ARGIND, FILENAME, $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\n"), File("/b.txt", "b1\n")],
            "3 awk /a.txt /b.txt\n1 /a.txt a1\n2 /b.txt b1\n"),

        Passing(
            "argc-argv-and-argind-for-stdin",
            ["BEGIN{print ARGC, ARGV[0], ARGIND} {print ARGIND, FILENAME, $1}"],
            [],
            "1 awk 0\n0 - a\n",
            "a b\n"),

        Passing(
            "explicit-stdin-input-argument",
            ["{print ARGC, ARGV[1], ARGIND, FILENAME, FNR, $1}", "-"],
            [],
            "2 - 1 - 1 a\n2 - 1 - 2 c\n",
            "a b\nc d\n"),

        Passing(
            "repeated-stdin-input-argument",
            ["{print FILENAME, FNR, NR, $1}", "-", "-"],
            [],
            "- 1 1 a\n- 2 2 b\n",
            "a\nb\n"),

        Passing(
            "dev-stdin-input-argument",
            ["{print ARGV[1], FILENAME, FNR, $1}", "/dev/stdin"],
            [],
            "/dev/stdin /dev/stdin 1 a\n",
            "a b\n"),

        Failing(
            "missing-input-file-without-beginfile-fails",
            ["{print FILENAME, FNR, NR, $1}", "/missing.txt", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            2,
            string.Empty,
            "awk: /missing.txt: No such file or directory\n"),

        FailingWithDirectories(
            "input-directory-without-beginfile-fails",
            ["{print FILENAME, FNR, NR, $1}", "/data", "/ok.txt"],
            ["/data"],
            [File("/ok.txt", "ok\n")],
            2,
            string.Empty,
            "awk: /data: Is a directory\n"),

        Passing(
            "argv-excludes-options-and-program-source",
            ["-F", ":", "-v", "prefix=SKU", "BEGIN{print ARGC, ARGV[0], ARGV[1], prefix}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "2 awk /file.txt SKU\n"),

        PassingWithEnvironment(
            "numeric-string-command-line-argv-and-environ",
            ["-v", "x=00", "BEGIN{print typeof(x), (x==0), (x==\"00\"), (x?1:0); print typeof(ARGV[1]), (ARGV[1]==0), (ARGV[1]==\"00\"), (ARGV[1]?1:0); print typeof(ENVIRON[\"N\"]), (ENVIRON[\"N\"]==0), (ENVIRON[\"N\"]==\"00\"), (ENVIRON[\"N\"]?1:0); exit}", "00"],
            [],
            [new AwkEnvironmentVariable("N", "00")],
            "strnum 1 1 0\nstrnum 1 1 0\nstrnum 1 1 0\n"),

        Passing(
            "post-program-assignment-after-begin-before-input",
            ["BEGIN{print ARGC, ARGV[1], ARGV[2]; if(x==\"\") print \"begin-empty\"} {print x, $1}", "x=1", "/file.txt"],
            [File("/file.txt", "a\n")],
            "3 x=1 /file.txt\nbegin-empty\n1 a\n"),

        Passing(
            "post-program-assignment-between-input-files",
            ["{print FILENAME, x, $1}", "x=1", "/a.txt", "x=2", "/b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "/a.txt 1 a\n/b.txt 2 b\n"),

        Passing(
            "delete-argv-skips-input-file",
            ["BEGIN{delete ARGV[1]} {print $0}", "/file.txt"],
            [File("/file.txt", "a\n")],
            string.Empty),

        Passing(
            "empty-argv-skips-input-file",
            ["BEGIN{ARGV[1]=\"\"} {print $0}", "/file.txt"],
            [File("/file.txt", "a\n")],
            string.Empty),

        Passing(
            "delete-argv-skips-command-assignment",
            ["BEGIN{delete ARGV[1]} {print x, $1}", "x=1"],
            [],
            " a\n",
            "a\n"),

        Passing(
            "mutated-argv-replaces-input-file",
            ["BEGIN{ARGV[1]=\"/b.txt\"} {print FILENAME, $1}", "/a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "/b.txt b\n")
        ];
        static AwkGoldenCase[] CommandLineSourceLoadingCases() =>
        [
            Passing(
            "long-options-field-separator-file-assign",
            ["--field-separator", ":", "--assign=prefix=SKU", "--file", "/script.awk", "/file.txt"],
            [File("/script.awk", "BEGIN{print prefix} {print $2}"), File("/file.txt", "a:b\nc:d\n")],
            "SKU\nb\nd\n"),

        Passing(
            "long-option-abbreviations",
            ["--field-sep", ":", "--assi=prefix=SKU", "--fil", "/script.awk", "/file.txt"],
            [File("/script.awk", "BEGIN{print prefix} {print $2}"), File("/file.txt", "a:b\nc:d\n")],
            "SKU\nb\nd\n"),

        Posix(Passing(
            "variable-assignment-option",
            ["-v", "prefix=SKU", "BEGIN{print prefix}"],
            [],
            "SKU\n")),

        Passing(
            "assign-option-repeated-escaped-and-equals",
            ["-v", "x=first", "--assign", "x=line\\nnext", "-v", "y=a=b", "BEGIN{print x; print y}"],
            [],
            "line\nnext\na=b\n"),

        Failing(
            "assign-option-invalid-identifier",
            ["-v", "1x=y", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid variable assignment\n"),

        Failing(
            "long-option-ambiguous-abbreviation",
            ["--p", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid option -- --p\n"),

        Failing(
            "long-option-l-prefix-ambiguous",
            ["--l", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid option -- --l\n"),

        Failing(
            "unknown-short-option",
            ["-Z", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid option -- -Z\n"),

        Failing(
            "unknown-long-option",
            ["--not-real", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid option -- --not-real\n"),

        Failing(
            "missing-field-separator-option-value",
            ["-F"],
            [],
            2,
            string.Empty,
            "awk: missing option value\n"),

        Failing(
            "missing-script-file-option-value",
            ["-f"],
            [],
            2,
            string.Empty,
            "awk: missing option value\n"),

        Failing(
            "missing-long-source-option-value",
            ["--source"],
            [],
            2,
            string.Empty,
            "awk: missing option value\n"),

        Failing(
            "missing-w-option-value",
            ["-W"],
            [],
            2,
            string.Empty,
            "awk: missing option value\n"),

        Passing(
            "help",
            ["--help"],
            [],
            AwkMessages.HelpMessageText),

        Passing(
            "short-help",
            ["-h"],
            [],
            AwkMessages.HelpMessageText),

        Passing(
            "version",
            ["--version"],
            [],
            AwkMessages.VersionMessageText),

        Passing(
            "long-option-version-abbreviation",
            ["--ver"],
            [],
            AwkMessages.VersionMessageText),

        Passing(
            "short-version",
            ["-V"],
            [],
            AwkMessages.VersionMessageText),

        Passing(
            "w-version",
            ["-W", "version"],
            [],
            AwkMessages.VersionMessageText),

        Posix(Passing(
            "script-file",
            ["-f", "/script.awk", "/file.txt"],
            [File("/script.awk", "BEGIN{print \"start\"} {print $2}"), File("/file.txt", "1 one\n2 two\n")],
            "start\none\ntwo\n")),

        Passing(
            "script-file-with-spaces-and-stdin",
            ["--file", "/dir/script one.awk"],
            [File("/dir/script one.awk", "{print $2}")],
            "b\nd\n",
            "a b\nc d\n"),

        Passing(
            "stdin-fallback-field-separator-assign-exit-zero",
            ["-F", ":", "-v", "prefix=SKU", "{print prefix, $2}"],
            [],
            "SKU b\nSKU d\n",
            "a:b\nc:d\n"),

        Passing(
            "empty-inline-script",
            ["", "/file.txt"],
            [File("/file.txt", "1 one\n")],
            string.Empty),

        Passing(
            "empty-script-file",
            ["-f", "/empty.awk", "/file.txt"],
            [File("/empty.awk", string.Empty), File("/file.txt", "1 one\n")],
            string.Empty),

        Passing(
            "repeated-script-files",
            ["-f", "/begin.awk", "-f", "/main.awk", "/file.txt"],
            [File("/begin.awk", "BEGIN{print \"start\"}"), File("/main.awk", "{print $2}"), File("/file.txt", "1 one\n2 two\n")],
            "start\none\ntwo\n"),

        Passing(
            "exec-source-file-stops-option-parsing",
            ["-E", "/program.awk", "-literal"],
            [File("/program.awk", "BEGIN{print ARGC, ARGV[1]; exit}")],
            "2 -literal\n"),

        Passing(
            "long-exec-source-file-stops-option-parsing",
            ["--exec=/program.awk", "-literal"],
            [File("/program.awk", "BEGIN{print ARGC, ARGV[1]; exit}")],
            "2 -literal\n"),

        Passing(
            "repeated-source-options",
            ["-e", "BEGIN{print \"start\"}", "-e", "{print $2}", "/file.txt"],
            [File("/file.txt", "1 one\n2 two\n")],
            "start\none\ntwo\n"),

        Passing(
            "function-only-program",
            ["function helper(){}"],
            [],
            string.Empty),

        Passing(
            "function-call-across-inline-source-units",
            ["-e", "function helper(){return \"ok\"}", "-e", "BEGIN{print helper()}"],
            [],
            "ok\n"),

        Passing(
            "include-library-function-shares-program-namespace",
            ["-i", "/lib.awk", "BEGIN{print helper()}"],
            [File("/lib.awk", "function helper(){return \"library\"}")],
            "library\n"),

        CompilerDiagnostic(Failing(
            "duplicate-function-across-source-units-fails",
            [
                "-e", "function helper(){return 1} BEGIN{print \"unreachable\"}",
                "-e", "function helper(){return 2}"
            ],
            [],
            2,
            string.Empty,
            "awk: <inline:2>:1:1: duplicate function definition `helper'.\n")),

        Passing(
            "long-source-option",
            ["--source=BEGIN{print prefix}", "--assign=prefix=SKU"],
            [],
            "SKU\n"),

        Passing(
            "include-option-source",
            ["-i", "/lib.awk", "BEGIN{print prefix}"],
            [File("/lib.awk", "BEGIN{prefix=\"SKU\"}")],
            "SKU\n"),

        Passing(
            "include-options-excluded-from-argv",
            ["-i", "/lib.awk", "--include", "/other.awk", "BEGIN{print marker; for(i=0;i<ARGC;i++) print i, ARGV[i]; exit}", "/data.txt"],
            [File("/lib.awk", "BEGIN{marker=marker \"L\"}"), File("/other.awk", "BEGIN{marker=marker \"O\"}")],
            "LO\n0 awk\n1 /data.txt\n"),

        Passing(
            "source-include-directive",
            ["@include \"/lib.awk\"\nBEGIN{print value}"],
            [File("/lib.awk", "BEGIN{value=\"ok\"}")],
            "ok\n"),

        Passing(
            "include-directive-once-and-suffix-fallback",
            ["@include \"/lib\"\n@include \"/lib\"\nBEGIN{print count}"],
            [File("/lib.awk", "BEGIN{count++}")],
            "1\n"),

        PassingWithEnvironment(
            "awkpath-file-lookup",
            ["-f", "common.awk"],
            [File("/lib/common.awk", "BEGIN{print \"from\", \"awkpath\"}")],
            [new AwkEnvironmentVariable("AWKPATH", "/lib")],
            "from awkpath\n"),

        PassingWithEnvironment(
            "awkpath-include-lookup",
            ["@include \"common\"\nBEGIN{print value}"],
            [File("/lib/common.awk", "BEGIN{value=\"from-include\"}")],
            [new AwkEnvironmentVariable("AWKPATH", "/lib")],
            "from-include\n"),

        Failing(
            "load-option-unsupported",
            ["-l", "ordchr", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: dynamic extension loading is unsupported in Lokad awk\n"),

        Failing(
            "long-load-option-unsupported",
            ["--load=ordchr", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: dynamic extension loading is unsupported in Lokad awk\n"),

        CompilerDiagnostic(Failing(
            "load-directive-unsupported",
            ["@load \"ordchr\"\nBEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: dynamic extension loading is unsupported in Lokad awk\n")),

        CompilerDiagnostic(Failing(
            "namespace-directive-unsupported",
            ["@namespace \"foo\"\nBEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: namespaces are unsupported in Lokad awk\n")),

        CompilerDiagnostic(Failing(
            "namespace-qualified-identifier-unsupported",
            ["BEGIN{print awk::typeof(1)}"],
            [],
            2,
            string.Empty,
            "awk: namespaces are unsupported in Lokad awk\n")),

        CompilerDiagnostic(Failing(
            "namespace-qualified-function-call-unsupported",
            ["BEGIN{print foo::bar()}"],
            [],
            2,
            string.Empty,
            "awk: namespaces are unsupported in Lokad awk\n")),

        CompilerDiagnostic(Failing(
            "include-containing-namespace-unsupported",
            ["@include \"/lib.awk\"\nBEGIN{print \"unreachable\"}"],
            [File("/lib.awk", "@namespace \"foo\"\nBEGIN{print \"bad\"}")],
            2,
            string.Empty,
            "awk: /lib.awk:1:1: namespaces are unsupported in Lokad awk\n")),

        Passing(
            "namespace-qualifier-in-string-and-regex",
            ["BEGIN{print \"a::b\"; print (\"a::b\" ~ /::/)}"],
            [],
            "a::b\n1\n"),

        CompilerDiagnostic(Failing(
            "strongly-typed-regex-unsupported",
            ["BEGIN{r=@/x/; print \"x\" ~ r}"],
            [],
            2,
            string.Empty,
            "awk: strongly typed regexp constants are unsupported in Lokad awk\n")),

        Passing(
            "strongly-typed-regex-token-in-string-and-regex",
            ["BEGIN{print \"@/x/\"; print (\"@/\" ~ /@\\//)}"],
            [],
            "@/x/\n1\n"),

        CompilerDiagnostic(Failing(
            "indirect-function-call-unsupported",
            ["BEGIN{f=\"length\"; print @f(\"abc\")}"],
            [],
            2,
            string.Empty,
            "awk: indirect function calls are unsupported in Lokad awk\n")),

        Passing(
            "indirect-function-token-in-string-and-regex",
            ["BEGIN{print \"@f(\"; print (\"@f(\" ~ /@f[(]/)}"],
            [],
            "@f(\n1\n"),

        PassingWithStdErr(
            "mode-options-accepted",
            [
                "-b",
                "--characters-as-bytes",
                "-c",
                "--traditional",
                "-P",
                "--posix",
                "-r",
                "--re-interval",
                "-n",
                "--non-decimal-data",
                "-N",
                "--use-lc-numeric",
                "BEGIN{print \"ok\"}"
            ],
            [],
            "ok\n",
            "awk: warning: `--posix' overrides `--characters-as-bytes'\n"),

        Passing(
            "characters-as-bytes-string-functions",
            ["--characters-as-bytes", "BEGIN{print length(\"café\"), index(\"café\",\"é\"), substr(\"café\",4,2)}"],
            [],
            "5 4 é\n"),

        Passing(
            "characters-as-bytes-ascii-regex",
            ["--characters-as-bytes", "BEGIN{s=\"abc\"; print gsub(/./,\"X\",s), s}"],
            [],
            "3 XXX\n"),

        Passing(
            "characters-as-bytes-non-ascii-regex-valid-utf8",
            ["--characters-as-bytes", "BEGIN{s=\"café\"; print gsub(/./,\"X\",s), s}"],
            [],
            "4 XXXX\n"),

        Failing(
            "characters-as-bytes-empty-fs-non-ascii-is-unsupported",
            ["--characters-as-bytes", "BEGIN{FS=\"\"} {print NF}", "/file.txt"],
            [File("/file.txt", "é\n")],
            2,
            string.Empty,
            "awk: byte-mode FS=\"\" over non-ASCII text is unsupported in Lokad awk\n"),

        Passing(
            "use-lc-numeric-pins-c-locale-formatting",
            ["--use-lc-numeric", "BEGIN{printf \"%.2f\\n\", 1.5; print 1.5 + \"2.5\"}"],
            [],
            "1.50\n4\n"),

        Passing(
            "lint-and-optimization-options-accepted",
            ["-Lfatal", "--lint=no-ext", "-t", "--lint-old", "-O", "--optimize", "-s", "--no-optimize", "-S", "--sandbox", "BEGIN{print \"ok\"}"],
            [],
            "ok\n"),

        Failing(
            "sandbox-output-pipe-is-disabled",
            ["--sandbox", "BEGIN{print \"payload\" | \"attacker-command --flag\"}"],
            [],
            2,
            string.Empty,
            "awk: redirections are disabled in sandbox mode\n"),

        Failing(
            "sandbox-output-file-redirection-is-disabled",
            ["--sandbox", "BEGIN{print \"payload\" > \"/out.txt\"}"],
            [],
            2,
            string.Empty,
            "awk: redirections are disabled in sandbox mode\n"),

        Failing(
            "sandbox-input-redirection-is-disabled",
            ["--sandbox", "BEGIN{getline value < \"/input.txt\"}"],
            [File("/input.txt", "secret\n")],
            2,
            string.Empty,
            "awk: redirections are disabled in sandbox mode\n"),

        Passing(
            "w-mode-options-accepted",
            ["-Wposix", "-W", "traditional", "-W", "lint=fatal", "-W", "no-optimize", "BEGIN{print \"ok\"}"],
            [],
            "ok\n"),

        Passing(
            "w-source-assign-and-field-separator",
            ["-W", "assign=prefix=SKU", "-W", "field-separator=:", "-Wsource=BEGIN{print prefix}", "-W", "source", "{print $2}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "SKU\nb\n"),

        PassingWithEnvironment(
            "w-file-include-and-awkpath",
            ["-W", "include=lib.awk", "-Wfile", "main.awk"],
            [File("/lib/lib.awk", "BEGIN{prefix=\"from\"}"), File("/lib/main.awk", "BEGIN{print prefix, \"awkpath\"}")],
            [new AwkEnvironmentVariable("AWKPATH", "/lib")],
            "from awkpath\n"),

        Passing(
            "w-exec-source-file-stops-option-parsing",
            ["-W", "exec=/program.awk", "-v", "x=bad"],
            [File("/program.awk", "BEGIN{print ARGC, ARGV[1], ARGV[2]; exit}")],
            "3 -v x=bad\n"),

        Failing(
            "w-load-option-unsupported",
            ["-W", "load=ordchr", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: dynamic extension loading is unsupported in Lokad awk\n"),

        Failing(
            "lint-option-invalid-mode",
            ["--lint=unknown", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid option -- --lint=unknown\n"),

        Failing(
            "w-option-unsupported",
            ["-W", "profile", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: -W profile\n"),

        Failing(
            "w-option-invalid",
            ["-W", "not-real", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: invalid option -- -W not-real\n"),

        Failing(
            "pretty-print-option-unsupported",
            ["--pretty-print=out.awk", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --pretty-print\n"),

        Failing(
            "profile-option-unsupported",
            ["--profile", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --profile\n"),

        Failing(
            "dump-variables-option-unsupported",
            ["--dump-variables=vars.out", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --dump-variables\n"),

        Failing(
            "debug-option-unsupported",
            ["--debug", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --debug\n"),

        Failing(
            "trace-option-unsupported",
            ["--trace", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --trace\n"),

        Failing(
            "gen-pot-option-unsupported",
            ["--gen-pot", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --gen-pot\n"),

        Failing(
            "copyright-option-unsupported",
            ["--copyright", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --copyright\n"),

        Failing(
            "bignum-option-unsupported",
            ["--bignum", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: --bignum\n"),

        Failing(
            "short-profile-option-unsupported",
            ["-pawkprof.out", "BEGIN{print}"],
            [],
            2,
            string.Empty,
            "awk: GNU option is unsupported in Lokad awk: -p\n"),

        Passing(
            "source-order-between-file-and-inline",
            ["-f", "/begin.awk", "-e", "BEGIN{print \"inline\"}", "/file.txt"],
            [File("/begin.awk", "BEGIN{print \"file\"}"), File("/file.txt", "data\n")],
            "file\ninline\n"),

        Passing(
            "option-terminator-before-program",
            ["--", "{print $1}", "/file.txt"],
            [File("/file.txt", "alpha beta\n")],
            "alpha\n"),

        Passing(
            "option-terminator-leading-dash-file",
            ["{print $1}", "--", "-data.txt"],
            [File("/-data.txt", "alpha beta\n")],
            "alpha\n"),

        Passing(
            "leading-dash-file-after-inline-program",
            ["{print $1}", "-data.txt"],
            [File("/-data.txt", "alpha beta\n")],
            "alpha\n"),

        Passing(
            "leading-dash-argv-after-inline-program",
            ["BEGIN{print ARGV[1]; exit}", "-literal"],
            [],
            "-literal\n"),

        Passing(
            "option-after-script-file-before-first-input",
            ["-f", "/script.awk", "-v", "prefix=SKU", "/file.txt"],
            [File("/script.awk", "{print prefix, $1}"), File("/file.txt", "alpha beta\n")],
            "SKU alpha\n"),

        Passing(
            "option-after-first-input-is-argument",
            ["-f", "/script.awk", "/file.txt", "-v", "prefix=bad"],
            [File("/script.awk", "BEGIN{print ARGC; for(i=0;i<ARGC;i++) print i, ARGV[i]; exit}")],
            "4\n0 awk\n1 /file.txt\n2 -v\n3 prefix=bad\n"),

        Failing(
            "missing-script-file",
            ["-f", "/missing.awk"],
            [],
            2,
            string.Empty,
            "awk: /missing.awk: No such file or directory\n"),
    ];
        static AwkGoldenCase[] CompatibilityModeCases() =>
        [
            CompilerDiagnostic(Failing(
            "posix-gensub-is-undefined",
            ["--posix", "BEGIN{print gensub(\"a\", \"b\", \"g\", \"a\")}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: gensub\n")),

        Passing(
            "gnu-source-numeric-literals",
            ["BEGIN{print 010, 0x10, 09, 010.5, 0010}"],
            [],
            "8 16 9 10.5 8\n"),

        Passing(
            "posix-source-numeric-literals",
            ["--posix", "BEGIN{print 010, 0x10, 09, 010.5, 0010}"],
            [],
            "10 0 9 10.5 10\n"),

        Passing(
            "traditional-source-numeric-literals",
            ["--traditional", "BEGIN{print 010, 0x10, 09, 010.5, 0010}"],
            [],
            "10 0 9 10.5 10\n"),

        Passing(
            "gnu-repeated-source-numeric-literals",
            ["BEGIN{for(i=0;i<3;i++){a+=010;b+=0x10;c+=09;d+=010.5;e+=0010}print a,b,c,d,e}"],
            [],
            "24 48 27 31.5 24\n"),

        Passing(
            "posix-repeated-source-numeric-literals",
            ["--posix", "BEGIN{for(i=0;i<3;i++){a+=010;b+=0x10;c+=09;d+=010.5;e+=0010}print a,b,c,d,e}"],
            [],
            "30 0 27 31.5 30\n"),

        Passing(
            "traditional-repeated-source-numeric-literals",
            ["--traditional", "BEGIN{for(i=0;i<3;i++){a+=010;b+=0x10;c+=09;d+=010.5;e+=0010}print a,b,c,d,e}"],
            [],
            "30 0 27 31.5 30\n"),

        Passing(
            "gnu-field-numeric-literals",
            ["BEGIN{$0=\"a b c d e f g h i j\"; print $010, $08, $0x2}"],
            [],
            "h h b\n"),

        Passing(
            "posix-field-numeric-literals",
            ["--posix", "BEGIN{$0=\"a b c d e f g h i j\"; print $010, $08, $0x2}"],
            [],
            "j h a b c d e f g h i j\n"),

        Passing(
            "traditional-field-numeric-literals",
            ["--traditional", "BEGIN{$0=\"a b c d e f g h i j\"; print $010, $08, $0x2}"],
            [],
            "j h a b c d e f g h i j\n"),

        Passing(
            "default-input-non-decimal-data-is-decimal",
            ["{print $1+0, typeof($1)}"],
            [],
            "10 strnum\n0 string\n",
            "010\n0x10\n"),

        Passing(
            "non-decimal-data-input-values",
            ["--non-decimal-data", "{print $1+0, typeof($1)}"],
            [],
            "8 strnum\n16 strnum\n9 strnum\n10.5 strnum\n",
            "010\n0x10\n09\n010.5\n"),

        Passing(
            "posix-input-hex-values",
            ["--posix", "{print $1+0}"],
            [],
            "10\n16\n",
            "010\n0x10\n"),

        Passing(
            "traditional-input-values-remain-decimal",
            ["--traditional", "{print $1+0}"],
            [],
            "10\n0\n",
            "010\n0x10\n"),

        Passing(
            "non-decimal-data-string-conversion",
            ["--non-decimal-data", "BEGIN{x=\"010\"; y=\"0x10\"; print x+0, typeof(x), y+0, typeof(y)}"],
            [],
            "8 string 16 string\n"),

        Passing(
            "posix-string-hex-conversion",
            ["--posix", "BEGIN{x=\"0x10\"; print x+0}"],
            [],
            "16\n"),

        Passing(
            "traditional-string-hex-remains-decimal-prefix",
            ["--traditional", "BEGIN{x=\"0x10\"; print x+0}"],
            [],
            "0\n"),

        Passing(
            "non-decimal-data-variable-assignment",
            ["--non-decimal-data", "-v", "v=010", "BEGIN{print v+0, typeof(v)}"],
            [],
            "8 strnum\n"),

        CompilerDiagnostic(Failing(
            "traditional-strtonum-is-undefined",
            ["--traditional", "BEGIN{print strtonum(\"10\")}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: strtonum\n")),

        CompilerDiagnostic(Failing(
            "posix-bit-function-is-undefined",
            ["--posix", "BEGIN{print and(1, 1)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: and\n")),

        CompilerDiagnostic(Failing(
            "posix-typeof-is-undefined",
            ["--posix", "BEGIN{print typeof(1)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: typeof\n")),

        CompilerDiagnostic(Failing(
            "traditional-isarray-is-undefined",
            ["--traditional", "BEGIN{print isarray(a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: isarray\n")),

        CompilerDiagnostic(Failing(
            "posix-patsplit-is-undefined",
            ["--posix", "BEGIN{print patsplit(\"a\", a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: patsplit\n")),

        CompilerDiagnostic(Failing(
            "traditional-asort-is-undefined",
            ["--traditional", "BEGIN{a[1]=1; print asort(a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: asort\n")),

        CompilerDiagnostic(Failing(
            "posix-asorti-is-undefined",
            ["--posix", "BEGIN{a[1]=1; print asorti(a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: asorti\n"))
        ];
        static AwkGoldenCase[] SyntaxDiagnosticCases() =>
        [
            Failing(
            "invalid-inline-script",
            ["{print"],
            [],
            2,
            string.Empty,
            "awk: unterminated action block.\n"),

        Failing(
            "invalid-script-file",
            ["-f", "/script.awk"],
            [File("/script.awk", "{print")],
            2,
            string.Empty,
            "awk: /script.awk:1:1: unterminated action block.\n"),

        Failing(
            "unterminated-string-fails",
            ["BEGIN{print \"unterminated\n}"],
            [],
            2,
            string.Empty,
            "awk: unrecognized awk token.\n"),

        Failing(
            "unterminated-regex-fails",
            ["BEGIN{print $0 ~ /unterminated}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n"),

        Failing(
            "unrecognized-lexical-token-fails",
            ["BEGIN{print `bad`}"],
            [],
            2,
            string.Empty,
            "awk: unrecognized awk token.\n"),

        Failing(
            "missing-closing-parenthesis-fails",
            ["BEGIN{print (1 + 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "missing-closing-bracket-fails",
            ["BEGIN{a[1 = 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "missing-statement-separator-fails",
            ["BEGIN{print 1 print 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid `print' statement.\n"),

        Failing(
            "stray-closing-parenthesis-fails",
            ["BEGIN{print 1)}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "extra-closing-brace-fails",
            ["BEGIN{print 1}}"],
            [],
            2,
            string.Empty,
            "awk: invalid pattern.\n"),

        Failing(
            "malformed-if-condition-fails",
            ["BEGIN{if (1 print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "stray-else-fails",
            ["BEGIN{else print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid statement.\n"),

        Failing(
            "malformed-while-condition-fails",
            ["BEGIN{while (1 print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "malformed-do-while-condition-fails",
            ["BEGIN{do print 1 while 1}"],
            [],
            2,
            string.Empty,
            "awk: expected do-while condition.\n"),

        Failing(
            "malformed-for-header-fails",
            ["BEGIN{for (i = 0; i < 3 i++) print i}"],
            [],
            2,
            string.Empty,
            "awk: invalid statement.\n"),

        Failing(
            "malformed-for-in-target-fails",
            ["BEGIN{for (1 in a) print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid statement.\n"),

        Failing(
            "malformed-switch-label-fails",
            ["BEGIN{switch (1) { case 1 print 1 }}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "malformed-switch-default-label-fails",
            ["BEGIN{switch (1) { default print 1 }}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "malformed-delete-missing-target-fails",
            ["BEGIN{delete}"],
            [],
            2,
            string.Empty,
            "awk: expected delete target.\n"),

        Failing(
            "malformed-print-redirection-fails",
            ["BEGIN{print \"x\" >}"],
            [],
            2,
            string.Empty,
            "awk: invalid expression.\n"),

        Failing(
            "trailing-print-redirection-token-fails",
            ["BEGIN{print x > y z}"],
            [],
            2,
            string.Empty,
            "awk: invalid path\n"),

        Failing(
            "malformed-printf-redirection-fails",
            ["BEGIN{printf \"%s\", \"x\" >}"],
            [],
            2,
            string.Empty,
            "awk: invalid expression.\n"),

        Failing(
            "malformed-getline-redirection-fails",
            ["BEGIN{getline x <}"],
            [],
            2,
            string.Empty,
            "awk: input redirection getline requires a source expression.\n"),

        Failing(
            "trailing-close-call-token-fails",
            ["BEGIN{close(\"/tmp\")y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after close statement.\n"),

        Failing(
            "trailing-fflush-call-token-fails",
            ["BEGIN{fflush(\"/tmp\")y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after fflush statement.\n"),

        Failing(
            "trailing-delete-token-fails",
            ["BEGIN{a[1]=1; delete a[1] y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after delete target.\n"),

        Failing(
            "trailing-getline-target-token-fails",
            ["BEGIN{getline x y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after getline target.\n"),

        Failing(
            "trailing-split-call-token-fails",
            ["BEGIN{split(\"a\", a) y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after split statement.\n"),

        Failing(
            "trailing-break-argument-fails",
            ["BEGIN{while (1) {break 1}}"],
            [],
            2,
            string.Empty,
            "awk: `break' does not take an argument.\n"),

        Failing(
            "trailing-continue-argument-fails",
            ["BEGIN{while (1) {continue 1}}"],
            [],
            2,
            string.Empty,
            "awk: `continue' does not take an argument.\n"),

        Failing(
            "trailing-next-argument-fails",
            ["{next 1}"],
            [],
            2,
            string.Empty,
            "awk: `next' does not take an argument.\n"),

        Failing(
            "trailing-nextfile-argument-fails",
            ["{nextfile 1}"],
            [],
            2,
            string.Empty,
            "awk: `nextfile' does not take an argument.\n"),

        Failing(
            "trailing-return-comma-expression-fails",
            ["function f(){return 1, 2} BEGIN{print f()}"],
            [],
            2,
            string.Empty,
            "awk: invalid `return' statement.\n"),

        Failing(
            "malformed-isarray-missing-argument-fails",
            ["BEGIN{print isarray()}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for isarray\n"),

        Failing(
            "malformed-typeof-missing-argument-fails",
            ["BEGIN{print typeof()}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for typeof\n"),

        Failing(
            "malformed-typeof-too-many-arguments-fails",
            ["BEGIN{print typeof(a, b)}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for typeof\n"),

        Failing(
            "unterminated-ternary-fails",
            ["BEGIN{print 1 ? 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid `print' statement.\n"),

        Failing(
            "malformed-function-definition-fails",
            ["function 1bad(){return 1} BEGIN{print 1}"],
            [],
            2,
            string.Empty,
            "awk: expected function name.\n")
        ];
        static AwkGoldenCase[] DiagnosticCases() =>
        [
            Posix(Failing(
            "invalid-delete-scalar-fails",
            ["BEGIN{a=1; delete a}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n")),

        Failing(
            "invalid-delete-scalar-element-fails",
            ["BEGIN{a=1; delete a[1]}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "invalid-delete-field-fails",
            ["BEGIN{delete $1}"],
            [],
            2,
            string.Empty,
            "awk: invalid delete target.\n"),

        Failing(
            "invalid-delete-parenthesized-target-fails",
            ["BEGIN{a[1]=1; delete (a[1])}"],
            [],
            2,
            string.Empty,
            "awk: invalid delete target.\n"),

        Failing(
            "invalid-delete-multiple-targets-fails",
            ["BEGIN{a[1]=1; delete a[1], a[2]}"],
            [],
            2,
            string.Empty,
            "awk: delete expects a single target.\n"),

        Failing(
            "reserved-keyword-variable-fails",
            ["BEGIN{if=1}"],
            [],
            2,
            string.Empty,
            "awk: expected if condition.\n"),

        Failing(
            "reserved-builtin-variable-fails",
            ["BEGIN{length=1}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n"),

        Failing(
            "reserved-builtin-bare-expression-fails",
            ["BEGIN{print rand}"],
            [],
            2,
            string.Empty,
            "awk: function `rand' requires a call.\n"),

        Failing(
            "reserved-keyword-function-name-fails",
            ["function if(){return 1} BEGIN{print if()}"],
            [],
            2,
            string.Empty,
            "awk: expected function name.\n"),

        Failing(
            "reserved-builtin-function-name-fails",
            ["function length(){return 1} BEGIN{print length()}"],
            [],
            2,
            string.Empty,
            "awk: function name `length' is reserved.\n"),

        Failing(
            "duplicate-function-definition-fails",
            ["function f(){return 1} function f(){return 2} BEGIN{print f()}"],
            [],
            2,
            string.Empty,
            "awk: duplicate function definition `f'.\n"),

        Failing(
            "duplicate-function-parameter-fails",
            ["function f(a,a){return a} BEGIN{print f(1)}"],
            [],
            2,
            string.Empty,
            "awk: duplicate function parameter `a'.\n"),

        Failing(
            "reserved-function-parameter-fails",
            ["function f(length){return 1} BEGIN{print f()}"],
            [],
            2,
            string.Empty,
            "awk: function parameter `length' is reserved.\n"),

        Failing(
            "builtin-scalar-as-array-fails",
            ["BEGIN{IGNORECASE[1]=1}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `IGNORECASE' as an array\n"),

        Failing(
            "undefined-function-fails",
            ["BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: missing_function\n"),

        Failing(
            "undefined-function-fails-after-prior-output",
            ["BEGIN{print \"before\"; missing_function(); print \"after\"}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: missing_function\n"),

        Failing(
            "undefined-function-lint-warning",
            ["--lint", "BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: warning: function `missing_function' called but never defined\nawk: undefined function: missing_function\n"),

        Failing(
            "undefined-function-lint-fatal",
            ["--lint=fatal", "BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: fatal: function `missing_function' called but never defined\n"),

        Failing(
            "undefined-function-short-lint-separated-fatal",
            ["-L", "fatal", "BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: fatal: function `missing_function' called but never defined\n"),

        Posix(Failing(
            "bad-lvalue-fails-before-prior-output",
            ["BEGIN{print \"before\"; 1 = 2; print \"after\"}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")),

        Failing(
            "unterminated-action-fails-before-prior-output",
            ["BEGIN{print \"before\"} {print \"record\""],
            [],
            2,
            string.Empty,
            "awk: unterminated action block.\n"),

        Failing(
            "bindtextdomain-function-unsupported",
            ["BEGIN{print bindtextdomain(\"messages\", \"/tmp\")}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Failing(
            "dcgettext-function-unsupported",
            ["BEGIN{print dcgettext(\"messages\", \"hello\", \"LC_MESSAGES\")}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Failing(
            "dcngettext-function-unsupported",
            ["BEGIN{print dcngettext(\"messages\", \"one\", \"many\", 2, \"LC_MESSAGES\")}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Failing(
            "internationalized-string-marker-unsupported",
            ["BEGIN{print _\"hello\"}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Passing(
            "internationalized-string-marker-in-string",
            ["BEGIN{print \"_\\\"hello\\\"\"}"],
            [],
            "_\"hello\"\n"),

        PassingWithStdErr(
            "ambiguous-string-escape-warnings",
            ["BEGIN{print \"a\\qz\"; print \"a\\xz\"; print \"a\\/z\"}"],
            [],
            "aqz\naxz\na/z\n",
            "awk: warning: escape sequence `\\q' treated as plain `q'\n" +
            "awk: warning: no hex digits in `\\x' escape sequence\n" +
            "awk: warning: escape sequence `\\/' treated as plain `/\'\n")
        ];
        static AwkGoldenCase[] ExpressionRuntimeCases() =>
        [
            Passing(
            "compound-assignment-sum",
            ["{sum+=$1} END{print sum}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "35\n"),

        Passing(
            "binary-addition-sum",
            ["{sum=sum+$1} END{print sum}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "35\n"),

        Posix(Passing(
            "concatenated-numeric-text-retains-string-semantics",
            ["BEGIN{x=\"01\" \"\"; print x+1, (x==1), (x==\"01\")}"],
            [],
            "2 0 1\n")),

        Passing(
            "compound-assignment-operators",
            ["BEGIN{x=10; x-=3; y=4; y*=5; z=20; z/=4; r=17; r%=5; print x, y, z, r}"],
            [],
            "7 20 5 2\n"),

        Passing(
            "decrement-operators",
            ["BEGIN{x=3; print --x; x--; a[\"k\"]=5; a[\"k\"]--; print x, a[\"k\"]}"],
            [],
            "2\n1 4\n"),

        Passing(
            "field-post-decrement",
            ["{$2--; print $0}", "/file.txt"],
            [File("/file.txt", "a 3 c\n")],
            "a 2 c\n"),

        Passing(
            "exponentiation-and-assignment",
            ["BEGIN{x=2^3; y=2; y^=4; print x, y, 2^3^2}"],
            [],
            "8 16 512\n"),

        Passing(
            "regex-match-operator",
            ["$2 ~ /^a/ {print $1}", "/file.txt"],
            [File("/file.txt", "10 apple\n5 berry\n20 apricot\n")],
            "10\n20\n"),

        Passing(
            "regex-not-match-operator",
            ["$2 !~ /^a/ {print $1}", "/file.txt"],
            [File("/file.txt", "10 apple\n5 berry\n20 apricot\n")],
            "5\n"),

        Passing(
            "regex-literal-scalar-expression-contexts",
            ["function keep(value){return value} {hit=/foo/; print /foo/, !/foo/, (/foo/ ? \"yes\" : \"no\"), hit, keep(/foo/)}", "/file.txt"],
            [File("/file.txt", "foo\nbar\n")],
            "1 0 yes 1 1\n0 1 no 0 0\n"),

        Passing(
            "regex-literal-direct-and-grouped-match-operands",
            ["{value=\"foo\"; print (value ~ /foo/), (value ~ (/foo/)), (value !~ /bar/)}", "/file.txt"],
            [File("/file.txt", "foo\nbar\n")],
            "1 0 1\n1 0 1\n"),

        Passing(
            "ignorecase-regex-literal-scalar-expression",
            ["BEGIN{IGNORECASE=1} {print /foo/}", "/file.txt"],
            [File("/file.txt", "Foo\nbar\n")],
            "1\n0\n"),

        Passing(
            "ignorecase-regex-matching",
            ["BEGIN{IGNORECASE=1} /foo/{print \"rule\", $0} $0 ~ /BAR/{print \"op\", $0} {if(match($0,/baz/)) print \"match\", RSTART}", "/file.txt"],
            [File("/file.txt", "Foo\nbar\nBAZ\n")],
            "rule Foo\nop bar\nmatch 1\n"),

        Passing(
            "ignorecase-substitution",
            ["BEGIN{IGNORECASE=1} {sub(/foo/,\"x\"); gsub(/bar/,\"y\"); print $0}", "/file.txt"],
            [File("/file.txt", "FOO BAR bar\n")],
            "x y y\n"),

        Passing(
            "ignorecase-split-and-field-splitting",
            ["BEGIN{IGNORECASE=1; split(\"aXb\", parts, \"x\"); print parts[1], parts[2]; FS=\"x\"} {print $1, $2}", "/file.txt"],
            [File("/file.txt", "cXd\n")],
            "aXb \ncXd \n"),

        Passing(
            "field-separator-plan-tracks-ignorecase",
            ["BEGIN{FS=\"[a]\"; IGNORECASE=0} {print NF; IGNORECASE=1}", "/file.txt"],
            [File("/file.txt", "xAy\nxAy\n")],
            "1\n2\n"),

        Passing(
            "parenthesized-arithmetic",
            ["{sum=(sum+$1)} END{print sum}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "35\n"),

        Passing(
            "print-numbers-use-ofmt",
            ["BEGIN{print 1/3; OFMT=\"%.2f\"; print 1/3; print \"1.2345\"}"],
            [],
            "0.333333\n0.33\n1.2345\n"),

        Passing(
            "print-numeric-field-expression-uses-ofmt",
            ["BEGIN{OFMT=\"%.2f\"} {print $1+0, $1}", "/file.txt"],
            [File("/file.txt", "1.2345\n")],
            "1.23 1.2345\n"),

        Passing(
            "print-assigned-numbers-use-ofmt",
            ["BEGIN{OFMT=\"%.2f\"; x=1/3; y=\"1.2345\"; print x; print y}"],
            [],
            "0.33\n1.2345\n"),

        Passing(
            "custom-number-formats-preserve-source-precision",
            ["BEGIN{OFMT=\"%.12g\"; print 1/3; CONVFMT=\"%.12g\"; print (1/3 \"\")}"],
            [],
            "0.333333333333\n0.333333333333\n"),

        Passing(
            "concat-numbers-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; print (1/3 \"\"); print (12 \"\")}"],
            [],
            "0.33\n12\n"),

        Passing(
            "concat-assigned-numbers-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; x=1/3; y=\"1\"; print \"v=\" x; print \"v=\" y}"],
            [],
            "v=0.33\nv=1\n"),

        Passing(
            "multipart-concat-preserves-formatting-order",
            ["function changefmt(){CONVFMT=\"%.2f\"; return \"b\"} BEGIN{CONVFMT=\"%.4f\"; print ((1/3) \"a\" changefmt())}"],
            [],
            "0.3333ab\n"),

        Passing(
            "implicit-string-conversions-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; x=1.2345; print length(x), index(x,\"23\"), substr(x,1,4), (x ~ /^1\\.23$/), match(x,/^1\\.23$/), tolower(x)}"],
            [],
            "4 3 1.23 1 1 1.23\n"),

        Passing(
            "comparison-regex-switch-and-substitution-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; n=1.234567; print (n==\"1.23\"), (n==\"1.23457\"), (\"1.23\" ~ n), (n ~ \"^1.23$\"); switch(n){case /^1\\.23$/: print \"switch\"}; x=n; sub(/23/,\"XX\",x); print x}"],
            [],
            "1 0 1 1\nswitch\n1.XX\n"),

        Passing(
            "special-separators-and-fields-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; n=1.234567; FS=n; $0=\"a1.23b\"; print NF,$1,$2; OFS=n; print \"a\",\"b\"; SUBSEP=n; a[\"x\",\"y\"]=7; print (\"x1.23y\" in a); OFS=\" \"; $0=n; FS=\"\\\\.\"; print NF; FS=\" \"; $0=\"a b\"; $1=n; print \"[\" $0 \"]\"}"],
            [],
            "2 a b\na1.23b\n1\n2\n[1.23 b]\n"),

        Passing(
            "record-and-output-separators-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; RS=1.234567; ORS=1.234567; print \"x\"; ORS=\"\\n\"} {print \"[\" $0 \"]\"}", "/file.txt"],
            [File("/file.txt", "a1.23b")],
            "x1.23[a]\n[b]\n"),

        Passing(
            "comparison-logical-and-ternary-expressions",
            ["BEGIN{x=1; print (\"b\" > \"a\"), (2 <= 2), (2 != 3); print (0 && ++x), x; print (1 || ++x), x; print (x ? \"yes\" : ++y), y+0}"],
            [],
            "1 1 1\n0 1\n1 1\nyes 0\n"),

        Passing(
            "postfix-update-expressions",
            ["BEGIN{x=1; print x++, x; a[\"k\"]=3; print a[\"k\"]++, a[\"k\"]; $0=\"a b\"; print $2++, $2}"],
            [],
            "1 2\n3 4\n0 1\n"),

        Passing(
            "mutation-expression-target-index-evaluates-once",
            ["BEGIN{i=0; a[1]=10; print ++a[++i], i, a[1], a[2]; i=0; a[1]=20; print a[++i]++, i, a[1], a[2]; i=0; a[1]=30; print (a[++i] += 5), i, a[1], a[2]}"],
            [],
            "11 1 11 \n20 1 21 \n35 1 35 \n"),

        Passing(
            "scientific-notation-numeric-constants",
            ["BEGIN{print 1e3, 2.5e2, 1E-1}"],
            [],
            "1000 250 0.1\n"),

        Passing(
            "dot-form-numeric-constants",
            ["BEGIN{print .5, 1., .5e1, 1.e1}"],
            [],
            "0.5 1 5 10\n"),

        Passing(
            "hexadecimal-numeric-constants",
            ["BEGIN{print 0x10, 0Xf}"],
            [],
            "16 15\n"),

        Passing(
            "octal-numeric-constants",
            ["BEGIN{print 010, 07, 00, 08, 09}"],
            [],
            "8 7 0 8 9\n"),

        Passing(
            "else-clause",
            ["{if($1>10){print $2}else{print \"small\"}}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "small\nsmall\nc\n"),

        Passing(
            "length-function",
            ["{print length($0)}", "/file.txt"],
            [File("/file.txt", "alpha\nbb\n")],
            "5\n2\n"),

        Passing(
            "length-without-argument",
            ["BEGIN{print length()} {print length()}", "/file.txt"],
            [File("/file.txt", "alpha\nbb\n")],
            "0\n5\n2\n"),

        Passing(
            "length-keyword-without-parentheses",
            ["{print length}", "/file.txt"],
            [File("/file.txt", "alpha\nbb\n")],
            "5\n2\n"),

        Passing(
            "length-array",
            ["BEGIN{a[\"x\"]=1; a[\"y\"]=2; print length(a), length(\"abc\")}"],
            [],
            "2 3\n"),

        Passing(
            "type-functions",
            ["BEGIN{a[1]=1; x=3; y=\"abc\"; print isarray(a), isarray(x), typeof(a), typeof(x), typeof(y)}"],
            [],
            "1 0 array number string\n"),

        Passing(
            "typeof-uninitialized-does-not-fix-type",
            ["BEGIN{print typeof(a), isarray(a); a[1]=1; print a[1]}"],
            [],
            "untyped 0\n1\n"),

        Passing(
            "typeof-array-element-creates-untyped-entry",
            ["BEGIN{a[2]=1; print typeof(a[1]), (1 in a), (2 in a); print typeof(a[2])}"],
            [],
            "untyped 1 1\nnumber\n"),

        Passing(
            "typeof-array-element-materializes-array-and-element",
            ["BEGIN{print typeof(a[1]), isarray(a), (1 in a)}"],
            [],
            "untyped 1 1\n"),

        Passing(
            "typeof-numeric-string-provenance",
            ["BEGIN{split(\"00\", a); x=a[1]; print typeof(a[1]), typeof(x); x=x \"\"; print typeof(x)}"],
            [],
            "strnum strnum\nstring\n"),

        Passing(
            "uninitialized-scalar-is-empty-string-and-zero",
            ["BEGIN{print \"[\" x \"]\", x+2, (x?1:0), (x==0), (x==\"\"), (x<1); y=\"\"; print (y==0), (y==\"\"), (y<1)}"],
            [],
            "[] 2 0 1 1 1\n0 1 1\n"),

        Passing(
            "uninitialized-array-element-is-empty-string-and-zero",
            ["BEGIN{print \"[\" a[1] \"]\", a[1]+2, (a[1]?1:0), (a[1]==0), (a[1]==\"\"); a[1]=\"\"; print (a[1]==0), (a[1]==\"\")}"],
            [],
            "[] 2 0 1 1\n0 1\n"),

        Passing(
            "numeric-string-input-fields-compare-numerically",
            ["{print typeof($1), ($1==0), ($1==\"00\"), ($1?1:0); x=\"00\"; print typeof(x), (x==0), (x==\"00\"), (x?1:0)}", "/file.txt"],
            [File("/file.txt", "00\n")],
            "strnum 1 1 0\nstring 0 1 1\n"),

        Passing(
            "numeric-string-split-results-compare-numerically",
            ["BEGIN{split(\"00 x\", a); print typeof(a[1]), (a[1]==0), (a[1]==\"00\"), (a[1]?1:0); print typeof(a[2]), (a[2]==0), (a[2]==\"x\"), (a[2]?1:0)}"],
            [],
            "strnum 1 1 0\nstring 0 1 1\n"),

        Failing(
            "scalar-as-array-assignment-fails",
            ["BEGIN{a=1; a[1]=2}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "scalar-as-array-read-fails",
            ["BEGIN{a=1; print a[1]}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "scalar-context-then-array-fails",
            ["BEGIN{x=a; a[1]=2}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "array-as-scalar-assignment-fails",
            ["BEGIN{a[1]=2; a=3}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Failing(
            "array-as-scalar-print-fails",
            ["BEGIN{a[1]=2; print a}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n")
        ];
        static AwkGoldenCase[] ExpressionCases() =>
        [
            Posix(Passing(
            "arithmetic-operators",
            ["BEGIN{print 7+2, 7-2, 7*2, 7/2, 7%2}"],
            [],
            "9 5 14 3.5 1\n")),

        Passing(
            "concat-function-field-unary-precedence",
            ["function f(){return \"B\"} BEGIN{$0=\"x y\"; n=2; print \"A\" f(), $n \"!\", -1 \"x\", \"v=\" 1+2}"],
            [],
            "AB y! -1x v=3\n"),

        Passing(
            "explicit-line-continuation-in-expression-and-string",
            ["BEGIN{print 1 + \\\n2; print \"a\\\nb\"}"],
            [],
            "3\nab\n"),

        Posix(Passing(
            "chained-assignment-expression",
            ["BEGIN{print (a = b = 7), a, b; c += d = 2; print c, d}"],
            [],
            "7 7 7\n2 2\n")),

        Passing(
            "assignment-expression-dynamic-target-order",
            ["BEGIN{i=1; a[i]=i=2; print i, \"[\" a[1] \"]\", a[2]; i=1; $0=\"x y\"; $(i)=i=2; print i, $0, $1, $2}"],
            [],
            "2 [] 2\n2 x 2 x 2\n"),

        Passing(
            "ambiguous-concat-comparison-and-regex-match",
            ["BEGIN{a=10; b=2; c=3; x=\"a\"; y=\"b\"; print (a < b c), (x y ~ /ab/), (x y ~ /a b/)}"],
            [],
            "1 1 0\n"),

        Passing(
            "redirection-looking-tokens-in-expression-contexts",
            ["BEGIN{s=\"a > b |& c >> d\"; print s; print (\"a>b\" ~ />/), (\"|\" ~ /[>|&]/); print index(\"x >> y\", \">>\"); print (1 ? \"x > y\" : \"z |& q\"); switch(\">\") {case \">\": print \"switch\"; default: print \"bad\"} if(1){print \"nested >>\"}}"],
            [],
            "a > b |& c >> d\n1 1\n3\nx > y\nswitch\nbad\nnested >>\n"),

        Posix(Passing(
            "nested-ternary-associativity",
            ["BEGIN{print (0 ? 1 : 0 ? 2 : 3), (1 ? 0 ? 2 : 3 : 4)}"],
            [],
            "3 3\n")),

        Passing(
            "unary-and-exponent-precedence",
            ["BEGIN{print -2^2, (-2)^2, 2^-2, 2^3^2}"],
            [],
            "-4 4 0.25 512\n"),

        Passing(
            "membership-precedence-with-comparison-logical-and-ternary",
            ["BEGIN{a[\"x\"]=1; print (\"x\" in a == 1), ((\"x\" in a)==1), (\"y\" in a || 1), (0 ? \"x\" in a : 7)}"],
            [],
            "1 1 1 7\n")
        ];
        static AwkGoldenCase[] FieldCases() =>
        [
            Posix(Passing(
            "field-separator",
            ["-F", ":", "{print $1}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "a\nc\n")),

        Posix(Passing(
            "regex-field-separator",
            ["-F", "[,:]", "{print $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "a:b,c\n")],
            "a b c\n")),

        Passing(
            "joined-field-separator",
            ["-F:", "{print $2}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "b\nd\n"),

        Passing(
            "empty-option-field-separator",
            ["-F", "", "{print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "abc\n")],
            "3 a b c\n"),

        Posix(Passing(
            "space-option-field-separator-uses-whitespace-semantics",
            ["-F", " ", "{print NF, $1, $2}", "/file.txt"],
            [File("/file.txt", "  a   b  \n")],
            "2 a b\n")),

        Posix(Passing(
            "begin-fs-assignment-controls-fields",
            ["BEGIN{FS=\":\"} {print $2}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "b\nd\n")),

        Passing(
            "variable-option-fs-controls-fields",
            ["-v", "FS=:", "{print $2}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "b\nd\n"),

        Passing(
            "fs-assignment-affects-next-record",
            ["NR==1{FS=\":\"; next} {print $2}", "/file.txt"],
            [File("/file.txt", "a b\nc:d\n")],
            "d\n"),

        Passing(
            "fieldwidths-splitting",
            ["BEGIN{FIELDWIDTHS=\"2 3 3\"} {print $1, $2, $3, NF}", "/file.txt"],
            [File("/file.txt", "AA111BBB\n")],
            "AA 111 BBB 3\n"),

        Passing(
            "fieldwidths-skip-column",
            ["BEGIN{FIELDWIDTHS=\"1:2 2\"} {print $1, $2, NF}", "/file.txt"],
            [File("/file.txt", "ABCDE\n")],
            "BC DE 2\n"),

        Passing(
            "fpat-field-pattern",
            ["BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "A-12-B\n")],
            "3 A 12 B\n"),

        Passing(
            "empty-fs-character-splitting",
            ["BEGIN{FS=\"\"} {print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "abc\n")],
            "3 a b c\n"),

        Passing(
            "literal-fs-preserves-empty-fields",
            ["BEGIN{FS=\",\"} {print NF, \"[\" $1 \"]\", \"[\" $2 \"]\", \"[\" $3 \"]\", \"[\" $4 \"]\"}", "/file.txt"],
            [File("/file.txt", "a,,c,\n")],
            "4 [a] [] [c] []\n"),

        Passing(
            "default-fs-space-tab-newline-groups",
            ["BEGIN{s=\" \\t\\na \\t b\\n \"; n=split(s,a,\" \"); print n; for(i=1;i<=n;i++) print \"[\" a[i] \"]\"}"],
            [],
            "2\n[a]\n[b]\n"),

        Passing(
            "default-fs-keeps-other-control-characters",
            ["BEGIN{s=\"a\\rb\\fc\\vd\"; n=split(s,a,\" \"); print n, index(a[1], \"\\r\"), index(a[1], \"\\f\"), index(a[1], \"\\v\")}"],
            [],
            "1 2 4 6\n"),

        Passing(
            "fs-assignment-switches-back-from-fieldwidths",
            ["BEGIN{FIELDWIDTHS=\"2 2\"; FS=\":\"} {print $1, $2, NF}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "a b 2\n"),

        Passing(
            "utf8-multibyte-characters-count-as-characters",
            ["{print length($0), index($0,\"\u00e9\")}", "/file.txt"],
            [File("/file.txt", "caf\u00e9\n")],
            "4 4\n"),

        Passing(
            "utf8-combining-mark-counts-as-character",
            ["{print length($0), index($0,\"\u0301\"), substr($0,4,2)}", "/file.txt"],
            [File("/file.txt", "cafe\u0301\n")],
            "5 5 e\u0301\n"),

        Passing(
            "nonexistent-field-references",
            ["{i=5; print \"[\" $3 \"]\", \"[\" $(i) \"]\", NF}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "[] [] 2\n"),

        Passing(
            "dynamic-field-expression-reference",
            ["BEGIN{$0=\"a b c\"; print $(1+1), $NF, $0}"],
            [],
            "b c a b c\n"),

        Passing(
            "special-variable-prefix-identifiers",
            ["BEGIN{$0=\"a b c\"; NRx=42; NFx=2; NF_=1; NFIELD=3; print NRx, NR, $NFx, $NF_, $NFIELD, $NF}"],
            [],
            "42 0 b a c c\n"),

        Passing(
            "dynamic-field-post-increment",
            ["BEGIN{$0=\"1 2\"; i=2; print $i++, $2, $0; print $(i+0)}"],
            [],
            "2 3 1 3\n3\n"),

        Posix(Passing(
            "last-field-default-separator",
            ["{print $NF}", "/file.txt"],
            [File("/file.txt", "a b c\none two\n")],
            "c\ntwo\n")),

        Passing(
            "field-assignment-rebuilds-current-record",
            ["{$2=\"X\"; print $0}", "/file.txt"],
            [File("/file.txt", "a b c\n")],
            "a X c\n"),

        Passing(
            "field-assignment-captures-current-output-separator",
            ["BEGIN{$0=\"a b\"; $1=$1; OFS=\"|\"; $2=$2; OFS=\":\"; print $0}"],
            [],
            "a|b\n"),

        Passing(
            "field-assignment-extends-record",
            ["BEGIN{OFS=\"|\"} {i=4; $i=\"d\"; print NF, $0}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "4|a|b||d\n"),

        Passing(
            "field-zero-assignment-resplits-record",
            ["{$0=\"x y\"; print NF, $2}", "/file.txt"],
            [File("/file.txt", "a b c\n")],
            "2 y\n"),

        Passing(
            "nf-assignment-truncates-fields",
            ["{NF=2; print NF, $0}", "/file.txt"],
            [File("/file.txt", "a b c\n")],
            "2 a b\n"),

        Passing(
            "nf-assignment-extends-fields",
            ["{NF=4; print NF, $0}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "4 a b  \n"),

        Failing(
            "negative-field-reference-fails",
            ["BEGIN{print $(-1)}"],
            [],
            2,
            string.Empty,
            "awk: negative field index\n"),

        Failing(
            "negative-field-assignment-fails",
            ["BEGIN{$(-1)=\"x\"}"],
            [],
            2,
            string.Empty,
            "awk: negative field index\n"),

        Failing(
            "negative-nf-assignment-fails",
            ["BEGIN{NF=-1}"],
            [],
            2,
            string.Empty,
            "awk: negative field index\n"),

        Passing(
            "supplementary-utf8-characters-count-as-characters",
            ["BEGIN{s=\"\U0001F600a\"; print length(s), index(s,\"a\"), substr(s,1,1), substr(s,2,1); split(s,a,\"\"); print length(a), a[1], a[2]}"],
            [],
            "2 2 \U0001F600 a\n2 \U0001F600 a\n")
        ];
        static AwkGoldenCase[] FunctionSyntaxCases() =>
        [
            Passing(
            "builtin-function-call-whitespace",
            ["BEGIN{print length (\"abc\")}"],
            [],
            "3\n")
        ];
        static AwkGoldenCase[] BuiltinFunctionCases() =>
        [
            Passing(
            "common-string-and-int-functions",
            ["{print substr($1,2,3), toupper($2), tolower(\"MiX\"), int(3.8), int(-3.8)}", "/file.txt"],
            [File("/file.txt", "abcdef xy\n")],
            "bcd XY mix 3 -3\n"),

        Passing(
            "deterministic-numeric-functions",
            ["BEGIN{print int(sqrt(9)), int(sin(0)), int(cos(0)), int(exp(1)), int(log(exp(1))), int(atan2(0,-1))}"],
            [],
            "3 0 1 2 1 3\n"),

        Passing(
            "rand-default-seed",
            ["BEGIN{print int(rand()*1000000), int(rand()*1000000)}"],
            [],
            "924045 593908\n"),

        Passing(
            "srand-explicit-seed",
            ["BEGIN{old=srand(1); print int(rand()*1000000), int(rand()*1000000); print srand(2); print int(rand()*1000000); print srand(1); print int(rand()*1000000)}"],
            [],
            "924045 593908\n1\n893104\n2\n924045\n"),

        Passing(
            "srand-without-argument-returns-previous-seed",
            ["BEGIN{old=srand(7); print srand()}"],
            [],
            "7\n"),

        Passing(
            "time-functions-explicit-utc",
            ["BEGIN{print mktime(\"1970 01 02 03 04 05\", 1); print strftime(\"%Y-%m-%d %H:%M:%S\", 0, 1); print strftime(\"%s\", 86400, 1)}"],
            [],
            "97445\n1970-01-01 00:00:00\n86400\n"),

        Passing(
            "strftime-out-of-range-timestamp-is-empty",
            ["BEGIN{print \"[\" strftime(\"%Y\", 1e20, 1) \"]\"}"],
            [],
            "[]\n"),

        Passing(
            "strftime-procinfo-default-format",
            ["BEGIN{PROCINFO[\"strftime\"]=\"%Y\"; y=strftime(); if(y ~ /^[0-9][0-9][0-9][0-9]$/) print \"year\"; else print y}"],
            [],
            "year\n"),

        Passing(
            "systime-returns-positive-unix-time",
            ["BEGIN{if(systime()>0) print \"positive\"; else print \"bad\"}"],
            [],
            "positive\n"),

        Passing(
            "bit-functions",
            ["BEGIN{print and(6,3), or(6,3), xor(6,3), lshift(3,2), rshift(8,1), compl(0)}"],
            [],
            "2 7 5 12 4 9007199254740991\n"),

        Failing(
            "bit-function-negative-operand-fails",
            ["BEGIN{print and(-1, 1)}"],
            [],
            2,
            string.Empty,
            "awk: negative bitwise operand\n"),

        Failing(
            "bit-function-negative-shift-fails",
            ["BEGIN{print lshift(1, -1)}"],
            [],
            2,
            string.Empty,
            "awk: negative bitwise operand\n"),

        Passing(
            "sprintf-function",
            ["BEGIN{msg=sprintf(\"%5d:%s\", 7, \"sku\"); print msg}"],
            [],
            "    7:sku\n"),

        Passing(
            "printf-common-conversions",
            ["BEGIN{printf \"%04d %.2f %x %o %c %-5s!\\n\", 7, 3.126, 255, 9, 65, \"x\"}"],
            [],
            "0007 3.13 ff 11 A x    !\n"),

        Passing(
            "printf-preserves-number-precision-and-character-type",
            ["BEGIN{printf \"%.12g %.12g [%c] [%c]\\n\", 1/3, 1.23456789012, \"65\", 65}"],
            [],
            "0.333333333333 1.23456789012 [6] [A]\n"),

        Passing(
            "printf-counts-unicode-scalar-values",
            ["BEGIN{printf \"[%3s][%-3s][%.1s][%c][%c]\\n\", \"😀\", \"😀\", \"😀x\", \"😀x\", 128512}"],
            [],
            "[  😀][😀  ][😀][😀][😀]\n"),

        Passing(
            "printf-empty-and-numeric-string-conversions",
            ["BEGIN{printf \"[%d][%g][%s]\\n\", \"\", \"\", \"\"; split(\"00 1.5 x\", a); printf \"[%d][%.1f][%d][%s]\\n\", a[1], a[2], a[3], a[3]}"],
            [],
            "[0][0][]\n[0][1.5][0][x]\n"),

        Failing(
            "printf-too-few-arguments-fails",
            ["BEGIN{printf \"[%d][%s][%g]\\n\", 1}"],
            [],
            2,
            string.Empty,
            "awk: not enough arguments to satisfy format string\n"),

        Failing(
            "sprintf-too-few-arguments-fails",
            ["BEGIN{print sprintf(\"[%d][%s]\", 1)}"],
            [],
            2,
            string.Empty,
            "awk: not enough arguments to satisfy format string\n"),

        Failing(
            "printf-literal-width-overflow-fails",
            ["BEGIN{printf \"%999999999999s\", \"x\"}"],
            [],
            2,
            string.Empty,
            "awk: memory limit exceeded\n"),

        Failing(
            "printf-dynamic-precision-overflow-fails",
            ["BEGIN{printf \"%.*f\", 1e100, 1}"],
            [],
            2,
            string.Empty,
            "awk: memory limit exceeded\n"),

        Passing(
            "sprintf-width-and-precision",
            ["BEGIN{print sprintf(\"%.3s:%06.2f\", \"abcdef\", 12.3)}"],
            [],
            "abc:012.30\n"),

        Passing(
            "sprintf-dynamic-width-precision-and-alternate",
            ["BEGIN{print sprintf(\"[%*.*f] [%#x] [%#o] [%+d] [% d]\", 8, 2, 3.5, 255, 8, 7, 7)}"],
            [],
            "[    3.50] [0xff] [010] [+7] [ 7]\n"),

        Passing(
            "printf-dynamic-width-precision-and-alternate",
            ["BEGIN{printf \"%*.*f|%#x|%#o|%*s!\\n\", 6, 2, 3.5, 15, 15, -6, \"hi\"}"],
            [],
            "  3.50|0xf|017|hi    !\n"),

        Passing(
            "printf-alternate-form-edge-flags",
            ["BEGIN{printf \"[%#.0o][%#.0x][%#.0f][%#.0g][%#8.0x][%-#8x][%08x][%-08x]\\n\", 0, 0, 3, 3, 31, 31, 31, 31}"],
            [],
            "[0][][3.][3.][    0x1f][0x1f    ][0000001f][1f      ]\n"),

        Passing(
            "sprintf-sign-padding-string-precision-and-unknown-specifier",
            ["BEGIN{print sprintf(\"[%#.3g][%#.3G][%+08.2f][% 08.2f][%*.s][%.*s][%q]\", 12, 12, 3.5, 3.5, 5, \"abcdef\", 3, \"abcdef\", \"x\")}"],
            [],
            "[12.0][12.0][+0003.50][ 0003.50][     ][abc][%q]\n"),

        Passing(
            "printf-scientific-and-general-conversions",
            ["BEGIN{printf \"%.2e|%.2E|%.3g|%.3G|%s\\n\", 1234.5, 1234.5, 1234.5, 0.0012345, sprintf(\"%.2e\", 12.26)}"],
            [],
            "1.23e+03|1.23E+03|1.23e+03|0.00123|1.23e+01\n"),

        Passing(
            "strtonum-function",
            ["BEGIN{print strtonum(\"010\"), strtonum(\"0x10\"), strtonum(\"3.5\")}"],
            [],
            "8 16 3.5\n"),

        Passing(
            "strtonum-non-decimal-signs-and-prefix-consumption",
            ["BEGIN{print strtonum(\"+0x10\"), strtonum(\"-0x10\"), strtonum(\"010x\"), strtonum(\"12abc\")}"],
            [],
            "0 0 8 12\n"),

        Passing(
            "signed-infinity-string-conversion",
            ["BEGIN{print \"+inf\"+0; print \"-Inf\"+0; print ((\"+inf\"+0)>999999), ((\"-inf\"+0)<-999999); print \"inf\"+0, \"Infinity\"+0, \"NaN\"+0, \"+infx\"+0}"],
            [],
            "+inf\n-inf\n1 1\n0 0 0 0\n"),

        Passing(
            "strtonum-signed-infinity-decisions",
            ["BEGIN{print strtonum(\"+inf\"), strtonum(\"-Inf\"), strtonum(\"inf\"), strtonum(\"Infinity\"), strtonum(\"NaN\"), strtonum(\"1e999\")}"],
            [],
            "+inf -inf 0 0 0 +inf\n"),

        Passing(
            "printf-signed-infinity-conversions",
            ["BEGIN{printf \"[%g][%G][%f][%F][%d][%X]\\n\", \"+inf\"+0, \"+inf\"+0, \"-inf\"+0, \"-inf\"+0, \"+inf\"+0, \"+inf\"+0}"],
            [],
            "[+inf][+INF][-inf][-INF][+inf][+INF]\n"),

        Passing(
            "gensub-function",
            ["BEGIN{print gensub(\"a\",\"X\",\"g\",\"banana\"); print gensub(\"a\",\"X\",2,\"banana\")}"],
            [],
            "bXnXnX\nbanXna\n"),

        PassingWithStdErr(
            "gensub-third-argument-warning",
            ["BEGIN{print gensub(/a/,\"X\",0,\"banana\"); print gensub(/a/,\"X\",\"x\",\"banana\"); print gensub(/a/,\"X\",\"2x\",\"banana\"); print gensub(/a/,\"X\",0.8,\"banana\")}"],
            [],
            "bXnana\nbXnana\nbanXna\nbXnana\n",
            "awk: warning: gensub: third argument `0' treated as 1\nawk: warning: gensub: third argument `x' treated as 1\n"),

        Passing(
            "gensub-replacement-backreferences",
            ["BEGIN{print gensub(\"([a-z]+)([0-9]+)\",\"\\\\2-\\\\1\",\"g\",\"a12 b34\"); print gensub(\"[0-9]+\",\"<&>\",1,\"a12b34\")}"],
            [],
            "12-a 34-b\na<12>b34\n"),

        Passing(
            "gensub-zero-capture-and-escaped-letter",
            ["BEGIN{print gensub(/(a)/,\"\\\\0-\\\\1\",\"g\",\"a\"); print gensub(/a/,\"\\\\q\",\"g\",\"a\")}"],
            [],
            "a-a\nq\n"),

        Passing(
            "gensub-regexp-literal-argument",
            ["BEGIN{print gensub(/a+/,\"X\",\"g\",\"baana\")}"],
            [],
            "bXnX\n"),

        CompilerDiagnostic(Failing(
            "gensub-too-few-arguments-fails",
            ["BEGIN{print gensub(\"a\",\"X\")}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for gensub\n")),

        CompilerDiagnostic(Failing(
            "gensub-too-many-arguments-fails",
            ["BEGIN{print gensub(\"a\",\"X\",\"g\",\"banana\",\"extra\")}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for gensub\n")),

        Failing(
            "gensub-array-argument-fails",
            ["BEGIN{a[1]=1; print gensub(a,\"X\",\"g\",\"banana\")}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Passing(
            "string-octal-hex-and-alert-escapes",
            ["BEGIN{print \"A\\101\\x42\"; print length(\"\\a\")}"],
            [],
            "AAB\n1\n"),

        Passing(
            "string-hex-escape-consumes-two-digits",
            ["BEGIN{print \"[\" \"\\x4142\" \"]\"}"],
            [],
            "[A42]\n"),

        Passing(
            "logical-not-truthiness",
            ["BEGIN{x=0; y=\"\"; z=\"text\"; print !x, !1, !y, !z}"],
            [],
            "1 0 1 0\n"),

        Passing(
            "truthiness-distinguishes-numbers-and-strings",
            ["BEGIN{n=0; s0=\"0\"; s00=\"00\"; e=\"\"; t=\"text\"; a[1]=0; a[2]=\"0\"; print (n?1:0), (s0?1:0), (s00?1:0), (e?1:0), (u?1:0), (t?1:0), (a[1]?1:0), (a[2]?1:0)}"],
            [],
            "0 1 1 0 0 1 0 1\n"),

        Failing(
            "array-as-scalar-condition-fails",
            ["BEGIN{a[1]=1; if(a) print \"bad\"}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Passing(
            "numeric-prefix-string-arithmetic",
            ["BEGIN{print \"12abc\"+1, \"  -3.5x\"+1, \"1e2x\"+1, \".5x\"+1, \"abc\"+1}"],
            [],
            "13 -2.5 101 1.5 1\n"),

        Passing(
            "match-capture-array",
            ["{ext=\"(noext)\"; if(match($0,/\\.([^.]+)$/,m)){ext=m[1]} print ext}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "csv\n(noext)\ngz\n"),

        Passing(
            "match-rstart-and-rlength",
            ["BEGIN{print match(\"abc\",/b/), RSTART, RLENGTH; print match(\"abc\",/z/), RSTART, RLENGTH}"],
            [],
            "2 2 1\n0 0 -1\n"),

        Passing(
            "match-capture-array-metadata",
            ["BEGIN{if(match(\"abc\",/(a)(x)?(b)/,m)){for(i=0;i<=3;i++) print i, \"[\" m[i] \"]\", m[i,\"start\"], m[i,\"length\"]}}"],
            [],
            "0 [ab] 1 2\n1 [a] 1 1\n2 []  \n3 [b] 2 1\n"),

        Passing(
            "match-dynamic-regex",
            ["BEGIN{r=\"b+\"; print match(\"abbc\", r), RSTART, RLENGTH; if(match(\"file.txt\", \"\\\\.([^.]*)$\", m)) print m[1]}"],
            [],
            "2 2 2\ntxt\n"),

        CompilerDiagnostic(Failing(
            "match-capture-target-expression-fails",
            ["BEGIN{match(\"abc\", /a/, 1+2)}"],
            [],
            2,
            string.Empty,
            "awk: argument 3 of `match' must be an array name.\n")),

        Failing(
            "match-capture-target-scalar-fails",
            ["BEGIN{m=1; match(\"abc\", /a/, m)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `m' as an array\n"),
    ];
        static AwkGoldenCase[] HostPolicyCases() =>
        [
        Failing(
            "system-function-unsupported",
            ["BEGIN{print system(\"echo hi\")}"],
            [],
            2,
            string.Empty,
            "awk: system() is unsupported in Lokad awk\n"),

        Failing(
            "above-root-command-line-path-is-diagnostic",
            ["BEGIN{print}", "../../secret"],
            [],
            2,
            string.Empty,
            "awk: invalid path\n"),

        Failing(
            "above-root-runtime-path-is-diagnostic",
            ["BEGIN{getline value < \"../../secret\"}"],
            [],
            2,
            string.Empty,
            "awk: invalid path\n"),

        Failing(
            "coprocess-output-unsupported",
            ["BEGIN{print \"hi\" |& \"cat\"}"],
            [],
            2,
            string.Empty,
            "awk: coprocesses are unsupported in Lokad awk\n"),

        Failing(
            "coprocess-getline-unsupported",
            ["BEGIN{\"cat\" |& getline seen}"],
            [],
            2,
            string.Empty,
            "awk: coprocesses are unsupported in Lokad awk\n"),

        Failing(
            "inet-output-special-file-unsupported",
            ["BEGIN{print \"hi\" > \"/inet/tcp/0/localhost/80\"}"],
            [],
            2,
            string.Empty,
            "awk: /inet special files are unsupported in Lokad awk\n"),

        Failing(
            "inet-input-special-file-unsupported",
            ["BEGIN{getline seen < \"/inet/tcp/0/localhost/80\"}"],
            [],
            2,
            string.Empty,
            "awk: /inet special files are unsupported in Lokad awk\n"),

        Failing(
            "dev-fd-3-output-unsupported",
            ["BEGIN{print \"hi\" > \"/dev/fd/3\"}"],
            [],
            2,
            string.Empty,
            "awk: unsupported /dev/fd descriptor in Lokad awk\n"),

        Failing(
            "dev-fd-3-input-unsupported",
            ["BEGIN{getline seen < \"/dev/fd/3\"}"],
            [],
            2,
            string.Empty,
            "awk: unsupported /dev/fd descriptor in Lokad awk\n")
        ];
        static AwkGoldenCase[] MainInputIoCases() =>
        [
            Passing(
            "begin-getline-triggers-beginfile-and-continues-input",
            ["BEGIN{print \"begin\"; getline x; print \"got\", x, FILENAME, NR, FNR} BEGINFILE{print \"beginfile\", FILENAME, FNR} {print \"main\", FILENAME, NR, FNR, $0} ENDFILE{print \"endfile\", FILENAME, FNR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "begin\nbeginfile /file.txt 0\ngot one /file.txt 1 1\nmain /file.txt 2 2 two\nendfile /file.txt 2\n"),

        Passing(
            "begin-getline-honors-argv-replacement-before-first-read",
            ["BEGIN{ARGV[1]=\"/two.txt\"; getline x; print \"got\", x, FILENAME} BEGINFILE{print \"beginfile\", FILENAME} {print \"main\", FILENAME, $0} ENDFILE{print \"endfile\", FILENAME}", "/one.txt"],
            [File("/one.txt", "one-a\none-b\n"), File("/two.txt", "two-a\ntwo-b\n")],
            "beginfile /two.txt\ngot two-a /two.txt\nmain /two.txt two-b\nendfile /two.txt\n"),

        Passing(
            "begin-getline-applies-leading-command-line-assignment",
            ["BEGIN{print \"before\", \"[\" x \"]\"; getline; print \"after\", x, $0}", "x=1", "/file.txt"],
            [File("/file.txt", "line\n")],
            "before []\nafter 1 line\n"),

        Passing(
            "begin-getline-continues-across-input-files",
            ["BEGIN{while((getline x)>0) print \"got\", FILENAME, NR, FNR, x; print \"done\", NR, FNR} BEGINFILE{print \"beginfile\", FILENAME} ENDFILE{print \"endfile\", FILENAME, FNR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a\nb\n"), File("/b.txt", "c\n")],
            "beginfile /a.txt\ngot /a.txt 1 1 a\ngot /a.txt 2 2 b\nendfile /a.txt 2\nbeginfile /b.txt\ngot /b.txt 3 1 c\nendfile /b.txt 1\ndone 3 1\n"),

        Passing(
            "begin-getline-deleted-input-argument-falls-back-to-stdin",
            ["BEGIN{delete ARGV[1]; getline x; print x, FILENAME}", "/unused.txt"],
            [File("/unused.txt", "unused\n")],
            "stdin -\n",
            "stdin\n"),

        Passing(
            "begin-getline-keeps-open-input-after-current-argv-replacement",
            ["BEGIN{getline x; print \"got\", x, FILENAME; ARGV[1]=\"/two.txt\"} {print \"main\", FILENAME, $0}", "/one.txt"],
            [File("/one.txt", "one-a\none-b\n"), File("/two.txt", "two-a\n")],
            "got one-a /one.txt\nmain /one.txt one-b\n"),

        Passing(
            "begin-getline-honors-custom-record-separator",
            ["BEGIN{RS=\"|\"; getline x; print \"got\", x \"[\" RT \"]\", NR, FNR} {print \"main\", $0 \"[\" RT \"]\", NR, FNR}", "/file.txt"],
            [File("/file.txt", "a|b|c|")],
            "got a[|] 1 1\nmain b[|] 2 2\nmain c[|] 3 3\n"),

        Passing(
            "main-getline-shares-dynamic-record-separator-cursor",
            ["{print \"main\", $0 \"[\" RT \"]\", NR; if(NR==1){RS=\"|\"; getline x; print \"got\", x \"[\" RT \"]\", NR}}", "/file.txt"],
            [File("/file.txt", "a\nb|c|")],
            "main a[\n] 1\ngot b[|] 2\nmain c[|] 3\n"),

        Passing(
            "redirected-getline-honors-custom-record-separator",
            ["BEGIN{RS=\"|\"; while((getline x < \"/records.txt\") > 0) print x \"[\" RT \"]\"; close(\"/records.txt\")}"],
            [File("/records.txt", "a|b|")],
            "a[|]\nb[|]\n"),

        Passing(
            "paragraph-getline-current-record-preserves-paragraph-field-splitting",
            ["BEGIN{FS=\",\"; RS=\"\"; getline; print NF, $1, $2}", "/file.txt"],
            [File("/file.txt", "a\nb\n\n")],
            "2 a b\n"),

        CompilerDiagnostic(Failing(
            "beginfile-main-getline-fails",
            ["BEGINFILE{getline}", "/file.txt"],
            [File("/file.txt", "line\n")],
            2,
            string.Empty,
            "awk: non-redirected `getline' invalid inside `BEGINFILE' rule\n")),

        CompilerDiagnostic(Failing(
            "endfile-main-getline-expression-fails",
            ["ENDFILE{if((getline value)>0) print value}", "/file.txt"],
            [File("/file.txt", "line\n")],
            2,
            string.Empty,
            "awk: non-redirected `getline' invalid inside `ENDFILE' rule\n")),

        Failing(
            "function-main-getline-called-from-beginfile-fails",
            ["function readmain(){getline value} BEGINFILE{readmain()}", "/file.txt"],
            [File("/file.txt", "line\n")],
            2,
            string.Empty,
            "awk: non-redirected `getline' invalid inside `BEGINFILE' rule\n")
        ];
        static AwkGoldenCase[] IoCases()
        {
            return [
            Posix(PassingWithExpectedFiles(
            "print-redirection-keeps-file-open",
            ["BEGIN{print \"one\" > \"/out.txt\"; print \"two\" > \"/out.txt\"}"],
            [],
            string.Empty,
            [File("/out.txt", "one\ntwo\n")])),

        Posix(PassingWithExpectedFiles(
            "print-append-redirection",
            ["BEGIN{print \"new\" >> \"/out.txt\"}"],
            [File("/out.txt", "old\n")],
            string.Empty,
            [File("/out.txt", "old\nnew\n")])),

        PassingWithExpectedFiles(
            "printf-redirection",
            ["BEGIN{path=\"/out.txt\"; printf \"%s:%d\\n\", \"a\", 7 > path}"],
            [],
            string.Empty,
            [File("/out.txt", "a:7\n")]),

        FailingWithDirectories(
            "output-redirection-directory-fails",
            ["BEGIN{print \"x\" > \"/data\"; print \"after\"}"],
            ["/data"],
            [],
            2,
            string.Empty,
            "awk: /data: Operation failed.\n"),

        Failing(
            "output-redirection-readonly-file-fails",
            ["BEGIN{print \"x\" > \"/locked.txt\"; print \"after\"}"],
            [ReadOnlyFile("/locked.txt", "old\n")],
            2,
            string.Empty,
            "awk: /locked.txt: Permission denied\n"),

        FailingWithExpectedFiles(
            "output-file-survives-runtime-failure",
            ["BEGIN{print \"file-before\" > \"/out.txt\"; print \"stdout-before\"; print 1 / 0}"],
            [],
            2,
            "stdout-before\n",
            "awk: division by zero attempted\n",
            [File("/out.txt", "file-before\n")]),

        PassingWithExpectedFiles(
            "close-redirection-target",
            ["BEGIN{path=\"/out.txt\"; print \"one\" > path; close(path); print \"two\" > path}"],
            [],
            string.Empty,
            [File("/out.txt", "two\n")]),

        Posix(Passing(
            "getline-file-target-variable",
            ["BEGIN{getline first < \"/extra.txt\"; getline second < \"/extra.txt\"; getline second < \"/extra.txt\"; print first, second}"],
            [File("/extra.txt", "alpha\nbeta\n")],
            "alpha beta\n")),

        Passing(
            "getline-file-target-current-record",
            ["BEGIN{getline < \"/extra.txt\"; print NR, FNR, NF, $1, $2}"],
            [File("/extra.txt", "alpha beta\n")],
            "0 0 2 alpha beta\n"),

        Passing(
            "getline-file-close-reopens-input",
            ["BEGIN{getline first < \"/extra.txt\"; close(\"/extra.txt\"); getline second < \"/extra.txt\"; print first, second}"],
            [File("/extra.txt", "alpha\nbeta\n")],
            "alpha alpha\n"),

        Passing(
            "getline-file-updates-record-terminator",
            ["BEGIN{getline first < \"/extra.txt\"; firstRt=RT; if(firstRt==\"\\n\") firstRt=\"<LF>\"; getline second < \"/extra.txt\"; print first \"[\" firstRt \"]\"; print second \"[\" RT \"]\"}"],
            [File("/extra.txt", "one\ntwo")],
            "one[<LF>]\ntwo[]\n"),

        Passing(
            "getline-missing-file-sets-errno",
            ["BEGIN{getline seen < \"/missing.txt\"; print ERRNO}"],
            [],
            "No such file or directory\n"),

        Passing(
            "getline-success-preserves-prior-errno",
            ["BEGIN{getline missing < \"/missing.txt\"; getline seen < \"/extra.txt\"; print seen, ERRNO}"],
            [File("/extra.txt", "ok\n")],
            "ok No such file or directory\n"),

        PassingWithReadOpenErrors(
            "getline-read-open-error-sets-errno",
            ["BEGIN{print (getline seen < \"/locked.txt\"), ERRNO}"],
            [File("/locked.txt", "secret\n")],
            [ReadOpenError("/locked.txt", "Permission denied")],
            "-1 Permission denied\n"),

        FailingWithReadOpenErrors(
            "main-input-read-open-error-fails",
            ["{print}", "/locked.txt"],
            [File("/locked.txt", "secret\n")],
            [ReadOpenError("/locked.txt", "Permission denied")],
            2,
            string.Empty,
            "awk: /locked.txt: Permission denied\n"),

        Passing(
            "getline-main-input-current-record",
            ["NR==1{getline; print NR, FNR, $0} {print \"seen\", NR, $0}", "/file.txt"],
            [File("/file.txt", "one\ntwo\nthree\n")],
            "2 2 two\nseen 2 two\nseen 3 three\n"),

        Passing(
            "getline-main-input-target-variable",
            ["NR==1{getline line; print NR, FNR, $0, line} {print \"seen\", NR, $0}", "/file.txt"],
            [File("/file.txt", "one\ntwo\nthree\n")],
            "2 2 one two\nseen 2 one\nseen 3 three\n"),

        PassingWithExpectedFiles(
            "fflush-makes-redirection-visible",
            ["BEGIN{print \"visible\" > \"/out.txt\"; fflush(\"/out.txt\"); getline seen < \"/out.txt\"; print seen; print \"later\" > \"/out.txt\"}"],
            [],
            "visible\n",
            [File("/out.txt", "visible\nlater\n")]),

        PassingWithStdErr(
            "output-special-files",
            ["BEGIN{print \"out\" > \"/dev/stdout\"; print \"err\" > \"/dev/stderr\"; print \"fd2\" > \"/dev/fd/2\"}"],
            [],
            "out\n",
            "err\nfd2\n"),

        PassingWithExpectedFiles(
            "close-function-return-values",
            ["BEGIN{path=\"/out.txt\"; print \"one\" > path; print close(path); print close(path); print \"two\" > path}"],
            [],
            "0\n-1\n",
            [File("/out.txt", "two\n")]),

        AwkGoldenCaseBuilder
            .Create("fflush-function-return-values",
                ["BEGIN{path=\"/out.txt\"; print \"one\" > path; print fflush(path); getline seen < path; print seen; print fflush(path); print fflush(\"/missing.txt\"); print \"two\" > path}"], [])
            .WithStdOut("0\none\n-1\n-1\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `/out.txt' opened for reading, not writing\n" +
                "awk: warning: fflush: `/missing.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "one\ntwo\n")])
            .Build(),

        Passing(
            "close-unopened-sets-errno",
            ["BEGIN{print close(\"/missing.txt\"), ERRNO}"],
            [],
            "-1 close of redirection that was never opened\n"),

        PassingWithStdErr(
            "fflush-unopened-preserves-errno",
            ["BEGIN{print fflush(\"/missing.txt\"), ERRNO}"],
            [],
            "-1 \n",
            "awk: warning: fflush: `/missing.txt' is not an open file, pipe or co-process\n"),

        Passing(
            "getline-file-expression-return-values",
            ["BEGIN{print (getline x < \"/extra.txt\"), x; print (getline x < \"/extra.txt\"), x; print (getline y < \"/missing.txt\"), ERRNO}"],
            [File("/extra.txt", "alpha\n")],
            "1 alpha\n0 alpha\n-1 No such file or directory\n",
            "awk: /missing.txt: No such file or directory\n"),

        Passing(
            "getline-main-expression-return-value",
            ["NR==1{print (getline line), NR, FNR, $0, line} {print \"seen\", NR, $0}", "/file.txt"],
            [File("/file.txt", "one\ntwo\nthree\n")],
            "1 2 2 one two\nseen 2 one\nseen 3 three\n"),

        Failing(
            "close-function-invalid-direction",
            ["BEGIN{path=\"/out.txt\"; print \"one\" > path; print close(path, \"bad\")}"],
            [],
            2,
            string.Empty,
            "awk: close second argument must be `to' or `from'\n"),

        Failing(
            "getline-expression-unsupported-special-file-fails-after-prior-output",
            ["BEGIN{print \"before\"; print (getline seen < \"/inet/tcp/0/localhost/80\"); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: /inet special files are unsupported in Lokad awk\n"),

        Failing(
            "close-function-unsupported-special-file-fails-after-prior-output",
            ["BEGIN{print \"before\"; print close(\"/inet/tcp/0/localhost/80\"); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: /inet special files are unsupported in Lokad awk\n"),

        Failing(
            "fflush-function-unsupported-special-file-fails-after-prior-output",
            ["BEGIN{print \"before\"; print fflush(\"/inet/tcp/0/localhost/80\"); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: /inet special files are unsupported in Lokad awk\n")
        ];
            static AwkGoldenCase PassingWithReadOpenErrors(
                string name,
                IReadOnlyList<string> arguments,
                IReadOnlyList<AwkGoldenFile> inputFiles,
                IReadOnlyList<AwkGoldenReadOpenError> readOpenErrors,
                string expectedStdOut)
            {
                return AwkGoldenCaseBuilder
                    .Create(name, arguments, inputFiles)
                    .WithReadOpenErrors(readOpenErrors)
                    .WithStdOut(expectedStdOut)
                    .Build();
            }

            static AwkGoldenCase FailingWithExpectedFiles(
                string name,
                IReadOnlyList<string> arguments,
                IReadOnlyList<AwkGoldenFile> inputFiles,
                int expectedExitCode,
                string expectedStdOut,
                string expectedStdErr,
                IReadOnlyList<AwkGoldenFile> expectedFiles)
            {
                return AwkGoldenCaseBuilder
                    .Create(name, arguments, inputFiles)
                    .ExitsWith(expectedExitCode)
                    .WithStdOut(expectedStdOut)
                    .WithStdErr(expectedStdErr)
                    .WithExpectedFiles(expectedFiles)
                    .Build();
            }

            static AwkGoldenCase FailingWithReadOpenErrors(
                string name,
                IReadOnlyList<string> arguments,
                IReadOnlyList<AwkGoldenFile> inputFiles,
                IReadOnlyList<AwkGoldenReadOpenError> readOpenErrors,
                int expectedExitCode,
                string expectedStdOut,
                string expectedStdErr)
            {
                return AwkGoldenCaseBuilder
                    .Create(name, arguments, inputFiles)
                    .WithReadOpenErrors(readOpenErrors)
                    .ExitsWith(expectedExitCode)
                    .WithStdOut(expectedStdOut)
                    .WithStdErr(expectedStdErr)
                    .Build();
            }

            static AwkGoldenFile ReadOnlyFile(string path, string content) =>
                new(path, content, AwkFileTreePermission.ReadOnly);

            static AwkGoldenReadOpenError ReadOpenError(string path, string message) => new(path, message);
        }
        static AwkGoldenCase[] OutputCases() =>
        [
            Posix(Passing(
            "print-empty-first-value-keeps-separator",
            ["{print \"\", $0}", "/file.txt"],
            [File("/file.txt", "x\n")],
            " x\n")),

        Posix(Passing(
            "print-uses-ofs-and-ors-assignments",
            ["BEGIN{OFS=\"|\"; ORS=\";\"} {print $1, $2}", "/file.txt"],
            [File("/file.txt", "a b\nc d\n")],
            "a|b;c|d;")),

        Posix(Passing(
            "print-evaluates-values-before-resolving-output-formats",
            ["BEGIN{OFS=\" \"; OFMT=\"%.1f\"; print (OFS=\"x\"), (OFS=\"y\"), \"z\"; OFS=\" \"; print 1.23, (OFMT=\"%.3f\"), 4.56}"],
            [],
            "xyyyz\n1.230 %.3f 4.560\n")),

        Passing(
            "print-uses-ofs-from-variable-option",
            ["-v", "OFS=:", "{print $1, $2}", "/file.txt"],
            [File("/file.txt", "a b\nc d\n")],
            "a:b\nc:d\n"),

        Posix(Passing(
            "begin-main-end-stdout-order",
            ["BEGIN{print \"begin\"} {print \"main\", $0} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "begin\nmain one\nmain two\nend 2\n")),

        Posix(Exiting(
            "exit-in-main-flushes-output-before-end",
            ["{print \"main\", $0; exit 7} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            7,
            "main one\nend 1\n")),

        PassingWithExpectedFiles(
            "stdout-and-file-output-flush-independently",
            ["BEGIN{print \"file-1\" > \"/out.txt\"; print \"stdout-1\"; print \"file-2\" > \"/out.txt\"; print \"stdout-2\"}"],
            [],
            "stdout-1\nstdout-2\n",
            [File("/out.txt", "file-1\nfile-2\n")]),

        PassingWithStdErr(
            "stdout-and-stderr-special-paths-flush-independently",
            ["BEGIN{printf \"out-1\" > \"/dev/stdout\"; printf \"err-1\" > \"/dev/stderr\"; print \"-out-2\"; print \"-err-2\" > \"/dev/stderr\"}"],
            [],
            "out-1-out-2\n",
            "err-1-err-2\n"),

        Posix(Passing(
            "printf-parenthesized-argument-list",
            ["BEGIN{a=\"x\"; b=\"y\"; printf(\"%s %s\\n\", a, b)}"],
            [],
            "x y\n")),

        Posix(Passing(
            "printf-parenthesized-undefined-variable-argument",
            ["BEGIN{printf(\"%s%s\", sep, \"label\")}"],
            [],
            "label")),

        Posix(Passing(
            "print-parenthesized-argument-list",
            ["BEGIN{print(\"x\", \"y\")}"],
            [],
            "x y\n")),

        Posix(Passing(
            "printf-parenthesized-nested-groups-and-comparison",
            ["BEGIN{x=2; y=1; printf(((\"%s %d\\n\")), (\"x\"), (x > y))}"],
            [],
            "x 1\n")),

        PassingWithExpectedFiles(
            "printf-parenthesized-argument-list-with-redirection",
            ["BEGIN{printf(\"%s\", \"file-output\") > \"/out.txt\"}"],
            [],
            string.Empty,
            [File("/out.txt", "file-output")]),

        Posix(PassingWithExpectedFiles(
            "redirection-target-is-evaluated-before-print-values",
            ["BEGIN{x=\"a\"; print (x=\"v\") > (x=\"/print.txt\"); printf \"[%s]\", (x=\"w\") > (x=\"/printf.txt\"); print x}"],
            [],
            "w\n",
            [File("/print.txt", "v\n"), File("/printf.txt", "[w]")]))
        ];
        static AwkGoldenCase[] PatternCases() =>
        [
            Passing(
            "regex-filter-and-nr",
            ["/foo/ {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "foo\nbar\nfood\n")],
            "1:foo\n3:food\n"),

        Passing(
            "regex-pattern-default-print",
            ["/foo/", "/file.txt"],
            [File("/file.txt", "foo\nbar\nfood\n")],
            "foo\nfood\n"),

        Passing(
            "regex-literal-logical-or-tests-current-record",
            ["NR == 1 || /93-260/", "/file.txt"],
            [File("/file.txt", "Header\nalpha\n93-260L\nomega\n")],
            "Header\n93-260L\n"),

        Passing(
            "regex-literal-logical-and-tests-current-record",
            ["NR > 1 && /foo/", "/file.txt"],
            [File("/file.txt", "foo\nfoo\nbar\n")],
            "foo\n"),

        Passing(
            "regex-match-expression-literal-and-dynamic",
            ["BEGIN{pat=\"ba.\"; print (\"bar\" ~ /ba./), (\"foo\" !~ /ba./), (\"bar\" ~ pat), (\"foo\" !~ pat)}"],
            [],
            "1 1 1 1\n"),

        Passing(
            "regex-pattern-ignorecase-default-print",
            ["BEGIN{IGNORECASE=1} /foo/", "/file.txt"],
            [File("/file.txt", "Foo\nbar\nFOOD\n")],
            "Foo\nFOOD\n"),

        Passing(
            "regex-range-pattern",
            ["/start/,/end/ {print $0}", "/file.txt"],
            [File("/file.txt", "a\nstart\nx\nend\ny\nstart end\nz\n")],
            "start\nx\nend\nstart end\n"),

        Passing(
            "expression-range-pattern",
            ["NR==2, NR==4 {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a\nb\nc\nd\ne\n")],
            "2:b\n3:c\n4:d\n"),

        Passing(
            "expression-range-pattern-default-print",
            ["NR==2, NR==3", "/file.txt"],
            [File("/file.txt", "a\nb\nc\nd\n")],
            "b\nc\n"),

        Passing(
            "expression-range-pattern-same-record-reset",
            ["NR==2, NR==2 {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a\nb\nc\n")],
            "2:b\n"),

        Passing(
            "begin-end",
            ["BEGIN{print \"start\"} {print $1} END{print NR}", "/file.txt"],
            [File("/file.txt", "a b\nc d\n")],
            "start\na\nc\n2\n"),

        Passing(
            "missing-pattern-runs-every-record",
            ["{print NR, $1}", "/file.txt"],
            [File("/file.txt", "a\nb\n")],
            "1 a\n2 b\n"),

        Passing(
            "condition-with-printf",
            ["NR>=2 && NR<=3 {printf \"%5d %s\\n\", NR, $0}", "/file.txt"],
            [File("/file.txt", "zero\none\ntwo\nthree\n")],
            "    2 one\n    3 two\n"),

        Passing(
            "variables-and-if",
            ["BEGIN{max=0} {if($1>max){max=$1;file=$2}} END{print max, file}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "20 c\n"),

        Passing(
            "condition-only-default-print",
            ["NF>0", "/file.txt"],
            [File("/file.txt", "alpha\n\n  \nbeta gamma\n")],
            "alpha\nbeta gamma\n"),

        Passing(
            "expression-patterns",
            ["length($0)>3 {print \"len\", NR} NR%2==0 {print \"even\", NR}", "/file.txt"],
            [File("/file.txt", "a\nbbbb\nccc\ndddd\n")],
            "len 2\neven 2\nlen 4\neven 4\n"),

        Passing(
            "function-call-expression-pattern",
            ["function keep(value){return value ~ /^a/} keep($1) {print $2}", "/file.txt"],
            [File("/file.txt", "a1 one\nb2 two\na3 three\n")],
            "one\nthree\n"),

        Passing(
            "composed-parenthesized-pattern",
            ["($1==\"a\" || $2==\"y\") && !($3==\"skip\") {print NR}", "/file.txt"],
            [File("/file.txt", "a x ok\nb y ok\na y skip\nb z ok\n")],
            "1\n2\n"),

        Passing(
            "matching-rules-run-in-source-order",
            ["NR==1{print \"first\"} NR==1{print \"second\"} {print \"all\", NR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "first\nsecond\nall 1\nall 2\n"),

        Passing(
            "or-condition-without-whitespace",
            ["NR==2||NR==4{print}", "/file.txt"],
            [File("/file.txt", "zero\none\ntwo\nthree\n")],
            "one\nthree\n")
        ];
        static AwkGoldenCase[] RecordSchedulingCases() =>
        [
            Posix(Passing(
            "stdin-default-input",
            ["{print $2}"],
            [],
            "b\nd\n",
            "a b\nc d\n")),

        Passing(
            "long-record-field-processing",
            ["{print length($1), $2}", "/long.txt"],
            [File("/long.txt", new string('x', 100_000) + " tail\n")],
            "100000 tail\n"),

        Passing(
            "single-character-record-separator",
            ["BEGIN{RS=\",\"} {print NR \":\" $0}"],
            [],
            "1:a\n2:b\n3:c\n",
            "a,b,c"),

        Passing(
            "record-terminator-variable",
            ["BEGIN{RS=\",\"} {print $0 \"[\" RT \"]\"}"],
            [],
            "a[,]\nb[,]\nc[]\n",
            "a,b,c"),

        Passing(
            "dynamic-record-separator-custom-to-newline",
            ["BEGIN{RS=\",\"} {print NR \":\" $0 \"[\" RT \"]\"; if(NR==1) RS=\"\\n\"}"],
            [],
            "1:a[,]\n2:b[\n]\n3:c[\n]\n",
            "a,b\nc\n"),

        Passing(
            "dynamic-record-separator-newline-to-custom",
            ["{record=$0; terminator=RT; gsub(/\\n/,\"N\",record); gsub(/\\n/,\"N\",terminator); print NR \":\" record \"[\" terminator \"]\"; if(NR==1) RS=\"|\"}", "/file.txt"],
            [File("/file.txt", "a\nb|c\n")],
            "1:a[N]\n2:b[|]\n3:cN[]\n"),

        Passing(
            "dynamic-record-separator-redirects-only-unread-tail",
            ["BEGIN{RS=\",\"} {print NR \":\" $0; if(NR==1) RS=\"\\n\"; else if(NR==2) RS=\"|\"}"],
            [],
            "1:a\n2:b\n3:c\n4:d\n",
            "a,b\nc|d|"),

        Passing(
            "regex-record-separator",
            ["BEGIN{RS=\"[,;]\"} {print NR \":\" $0 \"[\" RT \"]\"}"],
            [],
            "1:a[,]\n2:b[;]\n3:c[]\n",
            "a,b;c"),

        Passing(
            "regex-record-separator-repeated-match",
            ["BEGIN{RS=\"-+\"} {print NR \":\" $0 \"[\" RT \"]\"}"],
            [],
            "1:a[--]\n2:b[---]\n3:c[]\n",
            "a--b---c"),

        Passing(
            "regex-record-separator-rebuilds-when-ignorecase-changes",
            ["BEGIN{RS=\"x+\"} {print $0 \"[\" RT \"]\"; IGNORECASE=1}"],
            [],
            "a[xx]\nb[XX]\nc[]\n",
            "axxbXXc"),

        Passing(
            "regex-record-separator-start-anchor-only-matches-input-start",
            ["BEGIN{RS=\"^.\"} {print NR \":\" $0 \"[\" RT \"]\"}"],
            [],
            "1:[a]\n2:bc[]\n",
            "abc"),

        Failing(
            "regex-record-separator-invalid-regex-fails-after-begin-output",
            ["BEGIN{print \"before\"; RS=\"[+\"} {print \"after\"}", "/file.txt"],
            [File("/file.txt", "abc\n")],
            2,
            "before\n",
            "awk: invalid regular expression\n"),

        Passing(
            "paragraph-record-separator",
            ["BEGIN{RS=\"\"} {print NR \":\" NF \":\" $1 \":\" $2}"],
            [],
            "1:2:a1:a2\n2:2:b1:b2\n3:1:c1:\n",
            "a1\na2\n\nb1 b2\n\n\nc1\n"),

        Passing(
            "paragraph-record-separator-crlf-run-preserves-entire-rt",
            ["BEGIN{RS=\"\"} {t=RT; gsub(/\\r/,\"R\",t); gsub(/\\n/,\"N\",t); print NR \":\" $0 \":\" t}"],
            [],
            "1:a:RNRNRN\n2:b:\n",
            "a\r\n\r\n\r\nb"),

        Passing(
            "paragraph-record-separator-whitespace-only-line-is-record-data",
            ["BEGIN{RS=\"\"} {text=$0; gsub(/\\n/,\"<NL>\",text); print NR \":\" text}"],
            [],
            "1:a<NL> <NL>b\n",
            "a\n \nb\n\n"),

        Passing(
            "paragraph-record-separator-preserves-lone-carriage-returns",
            ["BEGIN{RS=\"\"} {print NR \":\" length($0) \":\" index($0,\"\\r\")}"],
            [],
            "1:2:1\n2:2:2\n",
            "\ra\n\nb\r"),

        Passing(
            "paragraph-record-separator-single-character-fs-also-splits-newlines",
            ["BEGIN{RS=\"\"; FS=\",\"} {print NF \":\" $1 \":\" $2 \":\" $3 \":\" $4}"],
            [],
            "4:a:b:c:d\n",
            "a,b\nc,d\n\n"),

        Passing(
            "default-rs-crlf-trims-cr-and-sets-rt",
            ["{t=RT; if(t==\"\\n\") t=\"<LF>\"; print NR \":\" length($0) \":\" index($0,\"\\r\") \":[\" t \"]\"}", "/file.txt"],
            [File("/file.txt", "a\r\nb\n")],
            "1:1:0:[<LF>]\n2:1:0:[<LF>]\n"),

        Passing(
            "default-rs-final-record-has-empty-rt",
            ["{t=RT; if(t==\"\\n\") t=\"<LF>\"; print NR \":\" $0 \":[\" t \"]\"}", "/file.txt"],
            [File("/file.txt", "a\nb")],
            "1:a:[<LF>]\n2:b:[]\n"),

        Passing(
            "default-rs-lone-cr-remains-record-data",
            ["{print NR, length($0), index($0,\"\\r\"), RT==\"\"}", "/file.txt"],
            [File("/file.txt", "a\rb")],
            "1 3 2 1\n"),

        Passing(
            "default-rs-nul-byte-remains-record-data",
            ["{print NR, length($0), index($0,\"\\0\"), RT==\"\\n\"}", "/file.txt"],
            [File("/file.txt", "a\0b\n")],
            "1 3 2 1\n"),

        Posix(Passing(
            "multiple-input-files",
            ["{print NR \":\" $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "1:a1\n2:a2\n3:b1\n")),

        Passing(
            "input-file-without-final-newline",
            ["{print NR, $1, $2}", "/file.txt"],
            [File("/file.txt", "a b")],
            "1 a b\n"),

        Posix(Passing(
            "filename-fnr-and-nr-across-files",
            ["{print FILENAME, FNR, NR, $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "/a.txt 1 1 a1\n/a.txt 2 2 a2\n/b.txt 1 3 b1\n")),

        Passing(
            "beginfile-and-endfile-for-files",
            ["BEGINFILE{print \"begin\", FILENAME, FNR} {print FILENAME, FNR, $1} ENDFILE{print \"end\", FILENAME, FNR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "begin /a.txt 0\n/a.txt 1 a1\n/a.txt 2 a2\nend /a.txt 2\nbegin /b.txt 0\n/b.txt 1 b1\nend /b.txt 1\n"),

        Passing(
            "beginfile-missing-file-nextfile-suppresses-error",
            ["BEGINFILE{print \"begin\", FILENAME, ERRNO; nextfile} END{print \"end\"}", "/missing.txt"],
            [],
            "begin /missing.txt No such file or directory\nend\n"),

        Passing(
            "filename-and-fnr-for-stdin",
            ["{print FILENAME, FNR, NR, $1}"],
            [],
            "- 1 1 a\n- 2 2 c\n",
            "a b\nc d\n"),

        Passing(
            "beginfile-and-endfile-for-stdin",
            ["BEGINFILE{print \"begin\", FILENAME, FNR} {print FILENAME, FNR, $1} ENDFILE{print \"end\", FILENAME, FNR}"],
            [],
            "begin - 0\n- 1 a\nend - 1\n",
            "a b\n"),

        Passing(
            "empty-argv-skip-preserves-argind-and-counters",
            ["BEGIN{ARGV[1]=\"\"} {print ARGIND, FILENAME, FNR, NR, $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "2 /b.txt 1 1 b\n"),

        Failing(
            "beginfile-missing-file-without-nextfile-fails",
            ["BEGINFILE{print \"begin\", FILENAME, ERRNO} {print \"bad\"} ENDFILE{print \"end\"} END{print \"done\"}", "/missing.txt"],
            [],
            2,
            "begin /missing.txt No such file or directory\n",
            "awk: /missing.txt: No such file or directory\n"),

        Passing(
            "beginfile-nextfile-recovers-from-missing-file",
            ["BEGINFILE{if(ERRNO){print \"skip\", FILENAME, ERRNO; nextfile}else{print \"open\", FILENAME, \"[\" ERRNO \"]\"}} {print FILENAME, FNR, NR, $1} END{print \"end\", NR, FILENAME}", "/missing.txt", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            "skip /missing.txt No such file or directory\nopen /ok.txt []\n/ok.txt 1 1 ok\nend 1 /ok.txt\n"),

        Failing(
            "beginfile-exit-on-missing-file-runs-end-with-status",
            ["BEGINFILE{if(ERRNO){print \"begin\", FILENAME, ERRNO; exit 5}} {print \"bad\"} END{print \"end\", NR, FILENAME}", "/missing.txt", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            5,
            "begin /missing.txt No such file or directory\nend 0 /missing.txt\n",
            string.Empty),

        CompilerDiagnostic(Failing(
            "begin-combined-with-expression-fails",
            ["BEGIN && 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "begin-end-range-pattern-fails",
            ["BEGIN, END {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "end-combined-with-expression-fails",
            ["END || 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "beginfile-combined-with-expression-fails",
            ["BEGINFILE && 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "beginfile-endfile-range-pattern-fails",
            ["BEGINFILE, ENDFILE {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "endfile-combined-with-expression-fails",
            ["ENDFILE || 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        Exiting(
            "beginfile-exit-skips-file-and-runs-end",
            ["BEGINFILE{print \"beginfile\", FILENAME, FNR; exit 6} {print \"bad\"} ENDFILE{print \"endfile\"} END{print \"end\", NR, FILENAME}", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            6,
            "beginfile /ok.txt 0\nend 0 /ok.txt\n"),

        Exiting(
            "endfile-exit-runs-end-with-status",
            ["{print \"main\", NR} ENDFILE{print \"endfile\", FNR; exit 7} END{print \"end\", NR, FILENAME}", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            7,
            "main 1\nendfile 1\nend 1 /ok.txt\n")
        ];
        static AwkGoldenCase[] RegexSubstitutionCases() =>
        [
            Passing(
            "gsub-mutates-current-line",
            ["NR==1{gsub(\"\\t\",\"<TAB>\"); print NR \":\" $0} NR==2{gsub(/\\t/,\"<TAB>\"); print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a\tb\nx\ty\n")],
            "1:a<TAB>b\n2:x<TAB>y\n"),

        Passing(
            "sub-mutates-first-match-in-current-line",
            ["{sub(/a/,\"X\"); print $0}", "/file.txt"],
            [File("/file.txt", "banana\n")],
            "bXnana\n"),

        Passing(
            "sub-and-gsub-replacement-ampersand",
            ["NR==1{gsub(/[0-9]+/,\"<&>\"); print $0} NR==2{sub(/[0-9]+/,\"\\\\&\"); print $0}", "/file.txt"],
            [File("/file.txt", "a12b3\nc45d\n")],
            "a<12>b<3>\nc&d\n"),

        Passing(
            "sub-and-gsub-replacement-backslash-literals",
            ["BEGIN{s=\"abc\"; gsub(/b/,\"X\\\\qY\",s); print s; s=\"abc\"; gsub(/b/,\"X\\\\\\\\qY\",s); print s; s=\"abc\"; gsub(/b/,\"X\\\\1Y\",s); print s; s=\"abc\"; gsub(/b/,\"X\\\\\\\\&Y\",s); print s}"],
            [],
            "aX\\qYc\naX\\\\qYc\naX\\1Yc\naX\\bYc\n"),

        Passing(
            "gensub-capture-replacement",
            ["BEGIN{print gensub(/([a-z]+)([0-9]+)/,\"[\\\\2:\\\\1]\",\"g\",\"ab12 cd34\")}"],
            [],
            "[12:ab] [34:cd]\n"),

        Passing(
            "gensub-capture-references-stop-at-nine",
            ["BEGIN{print gensub(/(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)/,\"<\\\\9>|<\\\\10>\",\"1\",\"abcdefghij\")}"],
            [],
            "<i>|<a0>\n"),

        Passing(
            "sub-and-gsub-explicit-targets",
            ["BEGIN{s=\"a1b2\"; gsub(/[0-9]/,\"X\",s); print s; sub(/X/,\"Y\",s); print s; a[\"k\"]=\"p1q2\"; gsub(/[0-9]/,\"Z\",a[\"k\"]); print a[\"k\"]} {$2=\"b2\"; gsub(/[0-9]/,\"X\",$2); print $0}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "aXbX\naYbX\npZqZ\na bX\n"),

        Passing(
            "sub-and-gsub-expression-return-values",
            ["BEGIN{s=\"banana\"; n=gsub(/a/,\"X\",s); print n, s; m=sub(/X/,\"Y\",s); print m, s; $0=\"foo\"; print sub(/z/,\"Q\"), $0}"],
            [],
            "3 bXnXnX\n1 bYnXnX\n0 foo\n"),

        CompilerDiagnostic(Failing(
            "sub-target-expression-fails",
            ["BEGIN{sub(/a/,\"X\",1+2)}"],
            [],
            2,
            string.Empty,
            "awk: argument 3 of `sub' must be an lvalue.\n")),

        Failing(
            "gsub-target-array-fails",
            ["BEGIN{a[1]=1; gsub(/1/,\"2\",a)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Failing(
            "sub-invalid-dynamic-regex-fails-after-prior-output",
            ["BEGIN{print \"before\"; r=\"[+\"; s=\"abc\"; sub(r,\"X\",s); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n"),

        Failing(
            "gsub-invalid-dynamic-regex-fails-after-prior-output",
            ["BEGIN{print \"before\"; r=\"[+\"; s=\"abc\"; gsub(r,\"X\",s); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n"),

        Failing(
            "gensub-invalid-dynamic-regex-fails-after-prior-output",
            ["BEGIN{print \"before\"; r=\"[+\"; print gensub(r,\"X\",\"g\",\"abc\"); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n")
        ];
        static IEnumerable<AwkGoldenCase> RegexCases()
        {
            yield return Failing(
                "dynamic-invalid-regex-fails-at-runtime",
                ["BEGIN{print \"before\"; r=\"[\"; print \"x\" ~ r; print \"after\"}"],
                [],
                2,
                "before\n",
                "awk: invalid regular expression\n");

            yield return CompilerDiagnostic(Failing(
                "literal-invalid-regex-fails-before-output",
                ["BEGIN{print \"before\"; print \"x\" ~ /[z-a]/; print \"after\"}"],
                [],
                2,
                string.Empty,
                "awk: invalid regular expression\n"));

            yield return Posix(Passing(
                "regex-posix-ere-basics",
                ["BEGIN{print (\"abc\" ~ /^a.c$/), (\"bbb\" ~ /^ab*$/), (\"ac\" ~ /^ab?c$/), (\"abbbc\" ~ /^ab+c$/), (\"abc\" ~ /^(ab|xy)c$/), (\"m\" ~ /^[a-z]$/), (\"M\" ~ /^[^a-z]$/), (\"aaa\" ~ /^a{2,3}$/)}"],
                [],
                "1 0 1 1 1 1 1 1\n"));

            yield return Passing(
                "regex-posix-equivalence-and-collating-c-locale",
                ["BEGIN{print (\"a\" ~ /[[=a=]]/), (\"b\" ~ /[[=a=]]/), (\"a\" ~ /[[.a.]]/), (\"b\" ~ /[[.a.]]/)}"],
                [],
                "1 0 1 0\n");

            yield return Posix(Passing(
                "regex-quantifier-braces",
                ["match($0,/a{2}/) {print $0}", "/file.txt"],
                [File("/file.txt", "a\naa\naaa\n")],
                "aa\naaa\n"));

            yield return Passing(
                "regex-escaped-slash",
                ["/a\\/b/ {print $0}", "/file.txt"],
                [File("/file.txt", "a/b\nab\n")],
                "a/b\n");

            yield return Passing(
                "regex-awk-escape-translation",
                ["BEGIN{print (\"A\" ~ /\\101/), (\"A\" ~ /\\x41/), (\"a\\bb\" ~ /\\b/), (\"a+b\" ~ /a\\+b/)}"],
                [],
                "1 1 1 1\n");

            yield return Passing(
                "regex-valid-utf8-match-and-captures",
                ["BEGIN{print match(\"café\", /fé/), RSTART, RLENGTH; if(match(\"ab12\", /([a-z]+)([0-9]+)/, m)) print m[0], m[1], m[2], m[1,\"start\"], m[2,\"length\"]}"],
                [],
                "3 3 2\nab12 ab 12 1 2\n");

            yield return Passing(
                "regex-empty-match-reports-zero-length",
                ["BEGIN{print match(\"abc\", //), RSTART, RLENGTH}"],
                [],
                "1 1 0\n");

            yield return Passing(
                "regex-c-style-control-escapes",
                ["BEGIN{print (\"\\n\" ~ /\\n/), (\"\\t\" ~ /\\t/), (\"\\r\" ~ /\\r/), (\"\\f\" ~ /\\f/), (\"\\v\" ~ /\\v/), (\"\\a\" ~ /\\a/)}"],
                [],
                "1 1 1 1 1 1\n");

            yield return Posix(Passing(
                "regex-posix-character-classes",
                ["BEGIN{print (\"A9\" ~ /^[[:alnum:]]+$/), (\"_\" ~ /^[[:alpha:]]+$/), (\" \" ~ /^[[:space:]]$/), (\"f\" ~ /^[[:xdigit:]]$/)}"],
                [],
                "1 0 1 1\n"));

            yield return Passing(
                "regex-gnu-word-boundaries",
                ["BEGIN{print (\"ab cd\" ~ /\\<cd/), (\"abcd\" ~ /\\<cd/), (\"ab\" ~ /ab\\>/), (\"abc\" ~ /ab\\>/), (\"ab cd\" ~ /\\ycd/)}"],
                [],
                "1 0 1 0 1\n");

            yield return Passing(
                "regex-gnu-word-operators",
                ["BEGIN{print (\"a_b\" ~ /^\\w+$/), (\"-\" ~ /^\\W$/), (\"ab\" ~ /a\\B/), (\"a \" ~ /a\\B/)}"],
                [],
                "1 1 1 0\n");

            yield return Passing(
                "regex-gnu-buffer-operators-dynamic",
                ["BEGIN{start=\"\\\\\" sprintf(\"%c\",96) \"abc\"; end=\"abc\\\\\" sprintf(\"%c\",39); print (\"abc\" ~ start), (\"xabc\" ~ start), (\"abc\" ~ end), (\"abcx\" ~ end)}"],
                [],
                "1 0 1 0\n");

            yield return Passing(
                "regex-tagged-field-separator",
                ["BEGIN{FS=\"[,;]\"} {print NF, $1, $2, $3}", "/file.txt"],
                [File("/file.txt", "a,b;c\n")],
                "3 a b c\n");

            yield return Passing(
                "regex-split-separator-array",
                ["BEGIN{n=split(\"a,b;c\", a, \"[,;]\", s); print n, a[1], a[2], a[3], \"[\" s[0] \"]\", \"[\" s[1] \"]\", \"[\" s[2] \"]\", \"[\" s[3] \"]\"}"],
                [],
                "3 a b c [] [,] [;] []\n");

            yield return Passing(
                "regex-tagged-record-separator",
                ["BEGIN{RS=\"[,;]\"} {print NR \":\" $0}", "/file.txt"],
                [File("/file.txt", "a,b;c")],
                "1:a\n2:b\n3:c\n");
        }
        static AwkGoldenCase[] StatementRuntimeCases() =>
        [
            Exiting(
            "exit-main-runs-end-and-status",
            ["{print $1; if(NR==2) exit 7; print \"after\"} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "a\nb\nc\n")],
            7,
            "a\nafter\nb\nend 2\n"),

        Passing(
            "exit-begin-skips-input-and-runs-end",
            ["BEGIN{print \"begin\"; exit} {print $0} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "a\nb\n")],
            "begin\nend 0\n"),

        Exiting(
            "exit-end-overrides-status-and-stops-end",
            ["BEGIN{exit 3} END{print \"end\"; exit 4; print \"late\"}"],
            [],
            4,
            "end\n"),

        CompilerDiagnostic(Failing(
            "return-outside-function-fails",
            ["BEGIN{return}"],
            [],
            2,
            string.Empty,
            "awk: return used outside function\n")),

        CompilerDiagnostic(Failing(
            "return-expression-outside-function-fails",
            ["BEGIN{return 1 + 2}"],
            [],
            2,
            string.Empty,
            "awk: return used outside function\n")),

        Failing(
            "division-by-zero-fails",
            ["BEGIN{print 1 / 0}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted\n"),

        Failing(
            "modulo-by-zero-fails",
            ["BEGIN{print 1 % 0}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted in `%'\n"),

        Failing(
            "divide-assignment-by-zero-fails",
            ["BEGIN{x=1; x/=0; print x}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted in `/='\n"),

        Failing(
            "modulo-assignment-by-zero-fails",
            ["BEGIN{x=1; x%=0; print x}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted in `%='\n"),

        Passing(
            "nextfile-skips-current-file",
            ["{print FILENAME, FNR, NR, $1; nextfile} END{print \"end\", NR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\nb2\n")],
            "/a.txt 1 1 a1\n/b.txt 1 2 b1\nend 2\n"),

        Passing(
            "nextfile-from-main-runs-endfile",
            ["BEGINFILE{print \"begin\", FILENAME} {print \"main\", FILENAME, FNR, NR; nextfile} ENDFILE{print \"endfile\", FILENAME, FNR} END{print \"end\", NR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "begin /a.txt\nmain /a.txt 1 1\nendfile /a.txt 1\nbegin /b.txt\nmain /b.txt 1 2\nendfile /b.txt 1\nend 2\n"),

        Passing(
            "beginfile-nextfile-skips-current-file",
            ["BEGINFILE{print \"skip\", FILENAME; nextfile} {print \"bad\"} ENDFILE{print \"end\", FILENAME, FNR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "skip /a.txt\nend /a.txt 0\nskip /b.txt\nend /b.txt 0\n"),

        CompilerDiagnostic(Failing(
            "next-in-begin-fails",
            ["BEGIN{next}"],
            [],
            2,
            string.Empty,
            "awk: next used outside input action\n")),

        CompilerDiagnostic(Failing(
            "next-in-end-fails",
            ["END{next}"],
            [],
            2,
            string.Empty,
            "awk: next used outside input action\n")),

        CompilerDiagnostic(Failing(
            "nextfile-in-begin-fails",
            ["BEGIN{nextfile}"],
            [],
            2,
            string.Empty,
            "awk: nextfile used outside input action\n")),

        CompilerDiagnostic(Failing(
            "nextfile-in-endfile-fails",
            ["ENDFILE{nextfile}", "/a.txt"],
            [File("/a.txt", "a1\n")],
            2,
            string.Empty,
            "awk: nextfile used outside input action\n"))
        ];
        static AwkGoldenCase[] StatementSeparatorCases() =>
        [
            Passing(
            "logical-operator-newline-continuation",
            ["""
                BEGIN{
                  print 1 &&
                    1
                  print 0 ||
                    1
                }
                """],
            [],
            "1\n1\n"),

        Passing(
            "ternary-newline-continuation",
            ["""
                BEGIN{
                  print 1 ?
                    "yes" :
                    "no"
                  print 0 ?
                    "bad" :
                    "ok"
                }
                """],
            [],
            "yes\nok\n"),

        Passing(
            "comma-newline-continuation",
            ["""
                BEGIN{
                  print sprintf("%s:%d",
                    "a",
                    7)
                }
                """],
            [],
            "a:7\n"),

        Passing(
            "do-while-newline-continuation",
            ["""
                BEGIN{
                  do
                    print ++i
                  while(i < 2)
                }
                """],
            [],
            "1\n2\n"),

        Passing(
            "else-newline-continuation",
            ["""
                BEGIN{
                  if(0)
                    print "bad"
                  else
                    print "ok"
                }
                """],
            [],
            "ok\n")
        ];
        static AwkGoldenCase[] StatementControlCases() =>
        [
            Posix(Passing(
            "single-statement-if-else-without-braces",
            ["{name=$0; ext=\"\"; if (match(name,/\\.([^.]+)$/ ,m)) ext=m[1]; else ext=\"(noext)\"; print ext}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "csv\n(noext)\ngz\n")),

        Passing(
            "dangling-else-binds-nearest-if",
            ["BEGIN{if(1) if(0) print \"bad\"; else print \"inner\"}"],
            [],
            "inner\n"),

        Posix(Passing(
            "blocks-as-statement-bodies",
            ["BEGIN{if(1){print \"if\"} while(i<1){print \"while\"; i++} for(j=0;j<1;j++){print \"for\"} do{print \"do\"; k++}while(k<1)}"],
            [],
            "if\nwhile\nfor\ndo\n")),

        Posix(Passing(
            "comment-text-does-not-close-syntax",
            ["BEGIN{print 1 # ignored } \" / [\nprint 2}"],
            [],
            "1\n2\n")),

        Posix(Passing(
            "for-in-mutation-snapshots-keys",
            ["BEGIN{a[\"a\"]=1; a[\"b\"]=1; for(k in a){seen++; a[\"c\"]=1; delete a[k]} print seen, length(a), (\"c\" in a)}"],
            [],
            "2 1 1\n")),

        PassingUnorderedStdOut(
            "replay-style-extension-aggregation",
            ["$1<=2025 {name=$2; ext=\"\"; if (match(name,/\\.([^.]+)$/ ,m)) ext=m[1]; else ext=\"(noext)\"; counts[ext]++} END{for(ext in counts){print ext, counts[ext]}}", "/file.txt"],
            [File("/file.txt", "2024 report.csv\n2025 README\n2026 archive.tar.gz\n")],
            "csv 1\n(noext) 1\n"),

        Passing(
            "classic-for-next-dynamic-field-index",
            ["-F", ";", "NR==1{for(i=1;i<=NF;i++) if($i==\"BuyPrice\") c=i; next} {v=$c; n++; if(index(v,\",\")>0) comma++; if(index(v,\".\")>0) dot++; if(index(v,\",\")>0 && index(v,\".\")>0) both++} END{print \"rows\",n,\"comma\",comma+0,\"dot\",dot+0,\"both\",both+0}", "/catalog.csv"],
            [File("/catalog.csv", "Sku;BuyPrice;Name\nA;12,30;x\nB;11.2;y\nC;1,234.56;z\n")],
            "rows 3 comma 2 dot 2 both 1\n"),

        Passing(
            "while-break-and-continue",
            ["BEGIN{i=0; while(i<5){i++; if(i==2) continue; if(i==4) break; print i} print \"done\"}"],
            [],
            "1\n3\ndone\n"),

        Passing(
            "classic-for-break-and-continue",
            ["BEGIN{for(i=1;i<=5;i++){if(i==2) continue; if(i==4) break; print i}}"],
            [],
            "1\n3\n"),

        CompilerDiagnostic(Failing(
            "break-outside-loop-fails",
            ["BEGIN{break}"],
            [],
            2,
            string.Empty,
            "awk: break used outside loop or switch\n")),

        CompilerDiagnostic(Failing(
            "continue-outside-loop-fails",
            ["BEGIN{continue}"],
            [],
            2,
            string.Empty,
            "awk: continue used outside loop\n")),

        Passing(
            "classic-for-omitted-components",
            ["BEGIN{i=1; for(;i<=3;){print i; i++}; for(j=1;;j++){print \"b\" j; if(j==2) break}}"],
            [],
            "1\n2\n3\nb1\nb2\n"),

        Passing(
            "switch-expression-regex-and-default-cases",
            ["BEGIN{for(i=1;i<=4;i++){switch(i){case 1: print \"one\"; break; case 2: print \"two\"; break; case /3/: print \"three\"; break; default: print \"other\"}}}"],
            [],
            "one\ntwo\nthree\nother\n"),

        Passing(
            "switch-falls-through-until-break",
            ["BEGIN{switch(1){case 1: print \"one\"; case 2: print \"two\"; default: print \"other\"}}"],
            [],
            "one\ntwo\nother\n"),

        Passing(
            "switch-label-keywords-nesting-and-empty-bodies",
            ["BEGIN{switch(\"case default\"){case \"case default\": print \"keyword-label\"; break; default: print \"bad\"} switch(\"outer\"){case \"outer\": switch(\"inner\"){case \"inner\": print \"nested\"; break}; break; default: print \"bad\"} switch(\"empty\"){case \"empty\": case \"other\": print \"bad\"; default: print \"after-empty\"}}"],
            [],
            "keyword-label\nnested\nbad\nafter-empty\n"),

        Passing(
            "switch-default-preserves-source-order",
            ["BEGIN{switch(9){case 1: print \"one\"; default: print \"default\"; case 2: print \"tail\"} switch(1){case 1: print \"matched\"; default: print \"middle\"; case 2: print \"tail\"} switch(2){case 1: print \"bad\"; default: print \"bad\"; case 2: print \"late\"}}"],
            [],
            "default\ntail\nmatched\nmiddle\ntail\nlate\n"),

        Failing(
            "posix-switch-is-gnu-extension",
            ["--posix", "BEGIN{switch(1){case 1: print \"one\"}}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: switch\n"),

        Failing(
            "traditional-switch-is-gnu-extension",
            ["--traditional", "BEGIN{switch(1){case 1: print \"one\"}}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: switch\n"),

        Passing(
            "do-while-loop",
            ["BEGIN{i=0; do{i++; print i} while(i<2)}"],
            [],
            "1\n2\n")
        ];
        static IEnumerable<AwkGoldenCase> UserFunctionCases()
        {
            yield return Posix(Passing(
                "user-function-return-expression",
                ["function inc(x){return x+1} BEGIN{print inc(2)}"],
                [],
                "3\n"));

            yield return Passing(
                "user-function-parameter-shadows-global",
                ["BEGIN{x=10; print plus(x), x} function plus(x){return x+1}"],
                [],
                "11 10\n");

            yield return Passing(
                "user-function-nested-scalar-parameter-retains-type",
                ["function inner(x){print typeof(x), x} function outer(x){inner(x)} BEGIN{outer(7)}"],
                [],
                "number 7\n");

            yield return Passing(
                "user-function-missing-parameter-becomes-unassigned-after-read",
                ["function inspect(x, copy){print typeof(x); copy=x; print typeof(x)} BEGIN{inspect()}"],
                [],
                "untyped\nunassigned\n");

            yield return Passing(
                "user-function-extra-parameter-is-uninitialized-local",
                ["function probe(x, local){return (local==0) \",\" (local==\"\") \",\" (local+2)} BEGIN{print probe(1); print probe(1, \"\")}"],
                [],
                "1,1,2\n0,1,2\n");

            yield return Passing(
                "user-function-recursive-return-expression",
                ["function fact(n){return n <= 1 ? 1 : n * fact(n - 1)} BEGIN{print fact(5)}"],
                [],
                "120\n");

            yield return Posix(Passing(
                "user-function-statement-body-return",
                ["function choose(a,b, m){if(a>b){m=a}else{m=b}; return m} BEGIN{print choose(3,5), choose(9,4)}"],
                [],
                "5 9\n"));

            yield return Passing(
                "user-function-loop-body-return",
                ["function sum(n, i, total){total=0; for(i=1;i<=n;i++){total+=i}; return total} BEGIN{print sum(4)}"],
                [],
                "10\n");

            yield return Passing(
                "user-function-print-side-effect",
                ["function emit(x){print \"emit\", x; return x+1} BEGIN{print \"value\", emit(4)}"],
                [],
                "emit 4\nvalue 5\n");

            yield return Failing(
                "user-function-getline-fails-after-prior-output",
                ["function fail(){print \"inside\"; getline x < \"/inet/tcp/0/localhost/80\"; return 1} BEGIN{print \"before\"; print fail(); print \"after\"}"],
                [],
                2,
                "before\ninside\n",
                "awk: /inet special files are unsupported in Lokad awk\n");

            yield return Posix(Passing(
                "user-function-array-parameter-by-reference",
                ["function fill(a){a[2]=9} BEGIN{x[1]=7; fill(x); print x[1], x[2]}"],
                [],
                "7 9\n"));

            yield return Passing(
                "user-function-untyped-parameter-promotes-array",
                ["function fill(a){a[1]=7} BEGIN{fill(x); print isarray(x), x[1]}"],
                [],
                "1 7\n");

            yield return Passing(
                "user-function-typeof-element-promotes-array-parameter",
                ["function inspect(a){print typeof(a[1]), isarray(a)} BEGIN{inspect(x); print isarray(x)}"],
                [],
                "untyped 1\n1\n");

            yield return Passing(
                "user-function-omitted-parameter-promotes-local-array",
                ["function fill(a){a[1]=7; print a[1], typeof(a)} BEGIN{fill(); print (\"a\" in SYMTAB)}"],
                [],
                "7 array\n0\n");

            yield return Passing(
                "user-function-same-name-untyped-argument-promotes-array",
                ["function fill(x){x[1]=7} BEGIN{fill(x); print x[1], typeof(x)}"],
                [],
                "7 array\n");

            yield return Passing(
                "user-function-forwards-untyped-array-argument",
                ["function inner(b){b[1]=9} function outer(a){inner(a); print a[1]} BEGIN{outer(x); print x[1]}"],
                [],
                "9\n9\n");

            yield return Passing(
                "user-function-forwards-same-name-untyped-array-argument",
                ["function inner(a){a[1]=9} function outer(a){inner(a); print a[1]} BEGIN{outer(x); print x[1]}"],
                [],
                "9\n9\n");

            yield return Passing(
                "user-function-forwards-existing-array-argument",
                ["function inner(b){b[2]=9} function outer(a){inner(a)} BEGIN{x[1]=7; outer(x); print x[1], x[2]}"],
                [],
                "7 9\n");

            yield return Passing(
                "user-function-untyped-scalar-argument-promotes-source",
                ["function set(a){a=1} BEGIN{set(x); print typeof(x)}"],
                [],
                "unassigned\n");

            yield return Passing(
                "user-function-mutual-recursion-statement-body",
                ["function even(n){if(n==0) return 1; return odd(n-1)} function odd(n){if(n==0) return 0; return even(n-1)} BEGIN{print even(6), odd(6)}"],
                [],
                "1 0\n");

            yield return Failing(
                "user-function-recursion-limit-fails",
                ["function loop(n){return loop(n+1)} BEGIN{print loop(0)}"],
                [],
                2,
                string.Empty,
                "awk: function recursion limit exceeded\n");

            yield return Failing(
                "user-function-scalar-parameter-array-use-fails",
                ["function fill(a){a[1]=7} BEGIN{x=1; fill(x)}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use scalar `a' as an array\n");

            yield return Passing(
                "user-function-bare-return",
                ["function empty(){return} BEGIN{print \"[\" empty() \"]\"}"],
                [],
                "[]\n");

            yield return Passing(
                "user-function-falls-off-end",
                ["function empty(){} BEGIN{print \"[\" empty() \"]\"}"],
                [],
                "[]\n");

            yield return Passing(
                "functab-builtins-and-user-functions",
                ["function inc(x){return x+1} BEGIN{print (\"length\" in FUNCTAB), FUNCTAB[\"length\"]; print (\"inc\" in FUNCTAB), FUNCTAB[\"inc\"]}"],
                [],
                "1 length\n1 inc\n");

            yield return CompilerDiagnostic(Failing(
                "functab-assignment-readonly",
                ["BEGIN{FUNCTAB[\"x\"]=\"y\"}"],
                [],
                2,
                string.Empty,
                "awk: cannot modify special array `FUNCTAB'\n"));

            yield return Passing(
                "symtab-scalar-bindings",
                ["BEGIN{SYMTAB[\"x\"]=\"y\"; print x; x=\"z\"; print SYMTAB[\"x\"]; print (\"FS\" in SYMTAB), (SYMTAB[\"FS\"]==\" \")}"],
                [],
                "y\nz\n1 1\n");

            yield return Passing(
                "symtab-special-scalar-assignment-updates-runtime-hook",
                ["BEGIN{SYMTAB[\"FS\"]=\":\"} {print $2}", "/file.txt"],
                [File("/file.txt", "a:b\n")],
                "b\n");

            yield return Failing(
                "symtab-invalid-reference-fails",
                ["BEGIN{SYMTAB[\"1bad\"]=\"y\"}"],
                [],
                2,
                string.Empty,
                "awk: invalid SYMTAB reference\n");

            yield return Failing(
                "symtab-scalar-context-fails",
                ["BEGIN{print SYMTAB}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use array `SYMTAB' in a scalar context\n");

            yield return Failing(
                "symtab-array-value-scalar-context-fails",
                ["BEGIN{a[1]=1; print SYMTAB[\"a\"]}"],
                [],
                2,
                string.Empty,
                "awk: attempt to use array `a' in a scalar context\n");

            yield return CompilerDiagnostic(Failing(
                "symtab-delete-rejected",
                ["BEGIN{delete SYMTAB[\"x\"]}"],
                [],
                2,
                string.Empty,
                "awk: `delete' is not allowed with SYMTAB\n"));

            yield return CompilerDiagnostic(Failing(
                "user-function-call-whitespace-invalid",
                ["function inc(x){return x+1} BEGIN{print inc (2)}"],
                [],
                2,
                string.Empty,
                "awk: function name `inc' used as a variable.\n"));
        }
        static AwkGoldenCase[] VariableCases() =>
        [
            Posix(PassingWithEnvironment(
            "environ-array",
            ["BEGIN{print ENVIRON[\"SKU\"] \":\" ENVIRON[\"EMPTY\"]}"],
            [],
            [new AwkEnvironmentVariable("SKU", "A1"), new AwkEnvironmentVariable("EMPTY", string.Empty)],
            "A1:\n")),

        Passing(
            "environ-does-not-inherit-ambient-host-values",
            ["BEGIN{present=(\"PATH\" in ENVIRON); print present, \"[\" ENVIRON[\"PATH\"] \"]\"}"],
            [],
            "0 []\n"),

        PassingWithEnvironment(
            "environ-array-mutation",
            ["BEGIN{ENVIRON[\"SKU\"]=\"B2\"; ENVIRON[\"NEW\"]=\"created\"; print ENVIRON[\"SKU\"], ENVIRON[\"NEW\"]}"],
            [],
            [new AwkEnvironmentVariable("SKU", "A1")],
            "B2 created\n"),

        Passing(
            "procinfo-version",
            ["BEGIN{print PROCINFO[\"version\"]}"],
            [],
            "Lokad awk\n"),

        Passing(
            "builtin-variable-defaults",
            ["BEGIN{print ARGC, ARGIND, BINMODE, CONVFMT, \"[\" ERRNO \"]\", \"[\" FIELDWIDTHS \"]\", \"[\" FILENAME \"]\", FNR, (length(FPAT)>0), \"[\" FS \"]\", IGNORECASE, LINT, NF, NR, OFMT, length(OFS), length(ORS), PREC, ROUNDMODE, length(RS), \"[\" RT \"]\", RSTART, RLENGTH, length(SUBSEP), TEXTDOMAIN}"],
            [],
            "1 0 0 %.6g [] [] [] 0 1 [ ] 0 0 0 0 %.6g 1 1 53 N 1 [] 0 0 1 messages\n")
        ];

        static IEnumerable<AwkGoldenCase> Tagged(
            IEnumerable<AwkGoldenCase> testCases,
            params AwkGoldenTag[] tags)
        {
            foreach (var testCase in testCases)
                yield return WithTags(testCase, tags);
        }

        static IEnumerable<AwkGoldenCase> CompilerDiagnostics(
            IEnumerable<AwkGoldenCase> testCases,
            params string[] excludedNames)
        {
            var excluded = new HashSet<string>(excludedNames, StringComparer.Ordinal);
            foreach (var testCase in testCases)
            {
                yield return excluded.Contains(testCase.Name)
                    ? testCase
                    : CompilerDiagnostic(testCase);
            }
        }
    }

    private static AwkGoldenCase CompilerDiagnostic(AwkGoldenCase testCase) =>
        WithTags(testCase, [AwkGoldenTag.CompilerDiagnostic]);

    private static AwkGoldenCase WithTags(AwkGoldenCase testCase, AwkGoldenTag[] tags)
    {
        var mergedTags = new List<AwkGoldenTag>(testCase.Tags);
        for (var index = 0; index < tags.Length; index++)
        {
            if (!mergedTags.Contains(tags[index]))
                mergedTags.Add(tags[index]);
        }

        AddDerivedTags(testCase, mergedTags);

        return AwkGoldenCaseBuilder
            .Create(testCase.Name, testCase.Arguments, testCase.InputFiles)
            .WithInputDirectories(testCase.InputDirectories)
            .WithReadOpenErrors(testCase.ReadOpenErrors)
            .WithStandardInput(testCase.StandardInput)
            .WithEnvironment(testCase.Environment)
            .ExitsWith(testCase.ExpectedExitCode)
            .WithStdOut(testCase.ExpectedStdOut)
            .WithStdErr(testCase.ExpectedStdErr)
            .WithExpectedFiles(testCase.ExpectedFiles)
            .WithStdOutComparison(testCase.StdOutComparison)
            .WithSupportLevel(testCase.SupportLevel)
            .WithTags(mergedTags)
            .WithNondeterminismNote(testCase.NondeterminismNote)
            .Build();

        static void AddDerivedTags(AwkGoldenCase testCase, List<AwkGoldenTag> tags)
        {
            AddTag(tags, AwkGoldenTag.DirectLayer);
            AddTag(tags, AwkGoldenTag.OracleReviewed);

            if (testCase.ExpectedStdErr.Length > 0 ||
                testCase.ExpectedExitCode != 0 ||
                testCase.SupportLevel != AwkGoldenSupportLevel.CurrentPass)
            {
                AddTag(tags, AwkGoldenTag.DiagnosticBehavior);
            }

            if (HasHostBehavior(testCase, tags))
                AddTag(tags, AwkGoldenTag.HostBehavior);

            if (!HasBehaviorTag(tags))
                AddTag(tags, AwkGoldenTag.RuntimeBehavior);

            if (testCase.NondeterminismNote.Length > 0 ||
                testCase.SupportLevel == AwkGoldenSupportLevel.KnownUnsupported ||
                tags.Contains(AwkGoldenTag.KnownUnsupported))
            {
                AddTag(tags, AwkGoldenTag.CompatibilityCaveat);
            }

            static bool HasHostBehavior(AwkGoldenCase testCase, IReadOnlyList<AwkGoldenTag> tags) =>
                testCase.ExpectedFiles.Count > 0 ||
                testCase.InputDirectories.Count > 0 ||
                testCase.ReadOpenErrors.Count > 0 ||
                tags.Contains(AwkGoldenTag.HostPolicy) ||
                tags.Contains(AwkGoldenTag.HostPolicyBoundary);

            static bool HasBehaviorTag(IReadOnlyList<AwkGoldenTag> tags) =>
                tags.Contains(AwkGoldenTag.RuntimeBehavior) ||
                tags.Contains(AwkGoldenTag.DiagnosticBehavior) ||
                tags.Contains(AwkGoldenTag.HostBehavior);

            static void AddTag(List<AwkGoldenTag> tags, AwkGoldenTag tag)
            {
                if (!tags.Contains(tag))
                    tags.Add(tag);
            }
        }
    }

    private static AwkGoldenCase Passing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut)
    {
        return Passing(name, arguments, inputFiles, expectedStdOut, string.Empty);
    }

    private static AwkGoldenCase Passing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut,
        string standardInput)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithStandardInput(standardInput)
            .Build();
    }

    private static AwkGoldenCase PassingUnorderedStdOut(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithStdOutComparison(AwkGoldenTextComparison.UnorderedNonEmptyLines)
            .WithNondeterminismNote("GNU awk leaves this array traversal order unspecified.")
            .Build();
    }

    private static AwkGoldenCase PassingWithEnvironment(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        string expectedStdOut)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithEnvironment(environment)
            .WithStdOut(expectedStdOut)
            .Build();
    }

    private static AwkGoldenCase PassingWithExpectedFiles(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut,
        IReadOnlyList<AwkGoldenFile> expectedFiles)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithExpectedFiles(expectedFiles)
            .Build();
    }

    private static AwkGoldenCase PassingWithStdErr(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase Exiting(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .Build();
    }

    private static AwkGoldenCase Failing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase FailingWithDirectories(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<string> inputDirectories,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithInputDirectories(inputDirectories)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase Posix(AwkGoldenCase testCase) =>
        AwkGoldenCaseBuilder
            .Create(testCase.Name, testCase.Arguments, testCase.InputFiles)
            .WithInputDirectories(testCase.InputDirectories)
            .WithReadOpenErrors(testCase.ReadOpenErrors)
            .WithStandardInput(testCase.StandardInput)
            .WithEnvironment(testCase.Environment)
            .ExitsWith(testCase.ExpectedExitCode)
            .WithStdOut(testCase.ExpectedStdOut)
            .WithStdErr(testCase.ExpectedStdErr)
            .WithExpectedFiles(testCase.ExpectedFiles)
            .WithStdOutComparison(testCase.StdOutComparison)
            .WithSupportLevel(testCase.SupportLevel)
            .WithTags([.. testCase.Tags, AwkGoldenTag.Posix])
            .WithNondeterminismNote(testCase.NondeterminismNote)
            .Build();

    private static AwkGoldenFile File(string path, string content) => new(path, content);
}

