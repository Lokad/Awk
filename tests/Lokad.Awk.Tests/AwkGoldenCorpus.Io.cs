namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] IoCases() =>
    [
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

        PassingWithStdErr(
            "getline-missing-file-sets-errno",
            ["BEGIN{getline seen < \"/missing.txt\"; print ERRNO}"],
            [],
            "No such file or directory\n",
            "awk: /missing.txt: No such file or directory\n"),

        PassingWithStdErr(
            "getline-success-preserves-prior-errno",
            ["BEGIN{getline missing < \"/missing.txt\"; getline seen < \"/extra.txt\"; print seen, ERRNO}"],
            [File("/extra.txt", "ok\n")],
            "ok No such file or directory\n",
            "awk: /missing.txt: No such file or directory\n"),

        PassingWithReadOpenErrorsAndStdErr(
            "getline-read-open-error-sets-errno",
            ["BEGIN{print (getline seen < \"/locked.txt\"), ERRNO}"],
            [File("/locked.txt", "secret\n")],
            [ReadOpenError("/locked.txt", "Permission denied")],
            "-1 Permission denied\n",
            "awk: /locked.txt: Permission denied\n"),

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

        PassingWithExpectedFiles(
            "fflush-function-return-values",
            ["BEGIN{path=\"/out.txt\"; print \"one\" > path; print fflush(path); getline seen < path; print seen; print fflush(path); print fflush(\"/missing.txt\"); print \"two\" > path}"],
            [],
            "0\none\n-1\n-1\n",
            [File("/out.txt", "one\ntwo\n")]),

        Passing(
            "close-unopened-sets-errno",
            ["BEGIN{print close(\"/missing.txt\"), ERRNO}"],
            [],
            "-1 close of redirection that was never opened\n"),

        Passing(
            "fflush-unopened-sets-errno",
            ["BEGIN{print fflush(\"/missing.txt\"), ERRNO}"],
            [],
            "-1 close of redirection that was never opened\n"),

        PassingWithStdErr(
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
}
