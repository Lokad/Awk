
namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] CommandLineSourceLoadingCases() =>
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

        Passing(
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
            "ok\n"),

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
}
