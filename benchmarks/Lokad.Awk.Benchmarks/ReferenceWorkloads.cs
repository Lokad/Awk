using System.Globalization;
using System.Text;

namespace Lokad.Awk.Benchmarking;

internal sealed record ReferenceWorkload(string Name, string Scale, int Records, int Fields,
    string Program, byte[] StandardInput, BenchmarkFile[] Files, byte[] ExpectedOutput, string Locale = "C")
{
    public long InputBytes => StandardInput.LongLength + Files.Sum(file => file.ContentBytes.LongLength);
    public string[] Arguments => [Program, .. Files.Select(file => file.Path.TrimStart('/'))];
    public OutputDigest ExpectedDigest { get; } = OutputDigest.FromBytes(ExpectedOutput);
}

internal static class ReferenceWorkloads
{
    public static readonly string[] Families =
    [
        "projection", "multi-column", "filter", "membership", "array-read", "array-read-alternating", "early-fields-wide", "long-fields", "aggregate", "profile-narrow", "profile-wide", "two-file",
        "fs-literal", "fs-regex", "rs-literal", "rs-regex", "substitution", "formatting", "sorting", "unicode",
        "arithmetic-loops", "while-arithmetic", "do-while-arithmetic", "user-functions", "loop-with-calls", "multi-arg-loop", "branching-calls", "branching-unary-calls", "string-functions", "case-conversion", "concatenation", "regex-match", "regex-captures"
    ];
    public static readonly string[] Scales = ["small", "medium", "large"];

    public static IEnumerable<ReferenceWorkload> CreateAll()
    {
        foreach (var family in Families)
            foreach (var scale in Scales)
                yield return Create(family, scale);
        yield return new("startup-empty", "control", 0, 0, "BEGIN{}", [], [], []);
        yield return new("startup-tiny", "control", 1, 3, "{print $2}", Bytes("a b c\n"), [], Bytes("b\n"));
    }

    public static ReferenceWorkload Create(string name, string scale)
    {
        var rows = scale switch
        {
            "small" => 16,
            "medium" => 4_096,
            "large" => 65_536,
            _ => throw new ArgumentException("Unknown workload scale.")
        };
        var input = new StringBuilder();
        var expected = new StringBuilder();
        string program;
        var fields = 3;
        BenchmarkFile[] files = [];
        var locale = "C";
        switch (name)
        {
            case "projection":
                program = "{print $2}";
                Repeat("sku\tvalue\ttail\n", "value\n");
                break;
            case "multi-column":
                program = "{print $1,$3,$2}";
                Repeat("sku\tvalue\ttail\n", "sku tail value\n");
                break;
            case "long-fields":
                // Long tokens isolate delimiter traversal from field count and output volume.
                program = "$2==\"keep\"{n++} END{print n+0}";
                fields = 2;
                rows = scale switch { "small" => 16, "medium" => 256, _ => 4_096 };
                var longToken = new string('x', 4_096);
                for (var i = 0; i < rows; i++)
                    input.Append(longToken).Append(i % 2 == 0 ? "\tkeep\n" : "\tskip\n");
                expected.Append(rows / 2).Append('\n');
                break;
            case "case-conversion":
                // Both directions change actual bytes; string-functions mostly reuses lowercase input.
                program = "{s=tolower($0);t=toupper($0);total+=length(s)+length(t)} END{print total}";
                fields = 1;
                var mixedCase = string.Concat(Enumerable.Repeat("Sku09-Ab:", 16));
                for (var i = 0; i < rows; i++)
                    input.Append(mixedCase).Append('\n');
                expected.Append(rows * mixedCase.Length * 2).Append('\n');
                break;
            case "filter":
                program = "$1==\"A\" && $2==\"B\" && $3==\"C\"{n++} END{print n+0}";
                for (var i = 0; i < rows; i++)
                    input.Append(i % 2 == 0 ? "A\tB\tC\n" : "A\tB\tD\n");
                expected.Append(rows / 2).Append('\n');
                break;
            case "early-fields-wide":
                // Early rejection on wide records; profile-wide deliberately consumes every field.
                program = "$1==\"ERROR\"{print $3}";
                fields = 64;
                var tail = string.Concat(Enumerable.Repeat("\ttail", fields - 3));
                for (var i = 0; i < rows; i++)
                {
                    input.Append(i % 8 == 0 ? "ERROR" : "INFO").Append("\tworker\tv")
                        .Append(i.ToString(CultureInfo.InvariantCulture)).Append(tail).Append('\n');
                }
                for (var i = 0; i < rows; i += 8)
                    expected.Append('v').Append(i.ToString(CultureInfo.InvariantCulture)).Append('\n');
                break;
            case "aggregate":
                program = "{sum[$1]+=$2} END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in sum)print k,sum[k]}";
                fields = 2;
                var totals = new SortedDictionary<string, int>(StringComparer.Ordinal);
                for (var i = 0; i < rows; i++)
                {
                    var key = "k" + (i % 16).ToString("D2", CultureInfo.InvariantCulture);
                    input.Append(key).Append("\t1\n");
                    totals[key] = totals.GetValueOrDefault(key) + 1;
                }
                foreach (var entry in totals)
                    expected.Append(entry.Key).Append(' ').Append(entry.Value).Append('\n');
                break;
            case "profile-narrow":
            case "profile-wide":
                fields = name == "profile-wide" ? 64 : 3;
                if (name == "profile-wide")
                    rows = scale switch { "small" => 16, "medium" => 256, _ => 4_096 };
                program = "{for(i=1;i<=NF;i++){nonempty+=($i!=\"\");chars+=length($i)}} END{print nonempty,chars}";
                var row = string.Join('\t', Enumerable.Repeat("text", fields)) + "\n";
                for (var i = 0; i < rows; i++)
                    input.Append(row);
                expected.Append(rows * fields).Append(' ').Append(rows * fields * 4).Append('\n');
                break;
            case "membership":
                program = "BEGIN{for(i=0;i<64;i++)lookup[\"k\" i]=1} ($1 in lookup){n++} END{print n+0}";
                fields = 2;
                for (var i = 0; i < rows; i++)
                    input.Append('k').Append(i % 128).Append("\tpayload\n");
                expected.Append(scale == "small" ? rows : rows / 2).Append('\n');
                break;
            case "array-read":
                program = "BEGIN{for(i=0;i<64;i++)lookup[\"k\" i]=i+1} {total+=lookup[$1]} END{print total,length(lookup)}";
                fields = 2;
                var readTotal = 0;
                for (var i = 0; i < rows; i++)
                {
                    if ((i & 1) == 0)
                    {
                        var key = (i / 2) % 64;
                        input.Append('k').Append(key);
                        readTotal += key + 1;
                    }
                    else
                        input.Append("missing").Append(i);
                    input.Append("\tpayload\n");
                }
                expected.Append(readTotal).Append(' ').Append(64 + rows / 2).Append('\n');
                break;
            case "array-read-alternating":
                // Combining two lookup tables contrasts with repeatedly accessing one container.
                program = "BEGIN{for(i=0;i<16;i++){a[\"sku\" i]=i;b[\"sku\" i]=i+1}} {n+=a[$1]+b[$1]} END{print n}";
                fields = 1;
                for (var i = 0; i < rows; i++)
                    input.Append("sku").Append(i % 16).Append('\n');
                expected.Append(rows * 16).Append('\n');
                break;
            case "two-file":
                program = "NR==FNR{left[$1]=$2;next} ($1 in left)&&left[$1]!=$2{n++} END{print n+0}";
                fields = 2;
                var left = new StringBuilder();
                var right = new StringBuilder();
                for (var i = 0; i < rows; i++)
                {
                    left.Append("k").Append(i).Append("\told\n");
                    right.Append("k").Append(i).Append(i % 4 == 0 ? "\tnew\n" : "\told\n");
                }
                files = [new("/left.tsv", left.ToString()), new("/right.tsv", right.ToString())];
                expected.Append(rows / 4).Append('\n');
                rows *= 2;
                break;
            case "fs-literal":
            case "fs-regex":
                program = "BEGIN{FS=\"" + (name == "fs-literal" ? "|" : "[|;]") +
                    "\"} {total+=NF+length($2)} END{print total}";
                Repeat(name == "fs-literal" ? "a|beta|flag\n" : "a|beta;flag\n", "");
                expected.Append(rows * 7).Append('\n');
                break;
            case "rs-literal":
            case "rs-regex":
                program = "BEGIN{RS=\"" + (name == "rs-literal" ? "</row>" : "[,;]+") +
                    "\"} {n++} END{print n+0}";
                fields = 1;
                Repeat(name == "rs-literal" ? "payload</row>" : "payload,;", "");
                expected.Append(rows).Append('\n');
                break;
            case "substitution":
                program = "{gsub(/[0-9]+/,\"#\");print}";
                fields = 2;
                Repeat("sku001 item22\n", "sku# item#\n");
                break;
            case "formatting":
                program = "{printf \"%s:%04d\\n\",$1,$2}";
                fields = 2;
                Repeat("sku\t7\n", "sku:0007\n");
                break;
            case "sorting":
                program = "{a[NR]=$1} END{n=asort(a,b,\"@val_num_asc\");for(i=1;i<=n;i++)print b[i]}";
                fields = 1;
                for (var i = rows; i >= 1; i--)
                    input.Append(i).Append('\n');
                for (var i = 1; i <= rows; i++)
                    expected.Append(i).Append('\n');
                break;
            case "unicode":
                program = "{print length($1),substr($2,1,2)}";
                fields = 2;
                locale = "C.UTF-8";
                Repeat("café résumé\n", "4 ré\n");
                break;
            // Scripts have canonical small controls in AwkGoldenCorpus.BenchmarkCore.cs.
            // Expected scaled results below use independent fixture formulas, outside timing.
            case "arithmetic-loops":
            case "while-arithmetic":
            case "do-while-arithmetic":
            case "loop-with-calls":
                program = name switch
                {
                    "loop-with-calls" => "function step(x){return (x*3+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}",
                    "while-arithmetic" => "{x=$1;i=0;while(i<8){x=(x*3+1)%101;i++}total+=x} END{print total}",
                    "do-while-arithmetic" => "{x=$1;i=0;do{x=(x*3+1)%101;i++}while(i<8);total+=x} END{print total}",
                    _ => "{x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=x} END{print total}"
                };
                fields = 1;
                Repeat("7\n", "");
                expected.Append(rows * 20).Append('\n');
                break;
            case "user-functions":
                program = "function inc(x){return x+1} function twice(x){return inc(inc(x))} {total+=twice($1)} END{print total}";
                fields = 1;
                Repeat("7\n", "");
                expected.Append(rows * 9).Append('\n');
                break;
            case "multi-arg-loop":
                program = "function step(x,a){return (x*3+a)%101} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}";
                fields = 1;
                Repeat("7\n", "");
                expected.Append(rows * 93).Append('\n');
                break;
            case "branching-calls":
                // A multi-statement body exercises ordinary calls, separately from
                // the prepared single-return function in multi-arg-loop.
                program = "function step(x,a){if(x<50)return (x*3+a)%101;return (x+a)%101} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}";
                fields = 1;
                Repeat("7\n", "");
                expected.Append(rows * 91).Append('\n');
                break;
            case "branching-unary-calls":
                // Contrast ordinary unary dispatch with both ordinary pairs and
                // the prepared single-return unary function in loop-with-calls.
                program = "function step(x){if(x<50)return (x*3+1)%101;return (x+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}";
                fields = 1;
                Repeat("7\n", "");
                expected.Append(rows * 73).Append('\n');
                break;
            case "string-functions":
                program = "{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))} END{print total}";
                fields = 1;
                Repeat("id:beta:tail\n", "");
                expected.Append(rows * 12).Append('\n');
                break;
            case "concatenation":
                program = "{s=\"[\" $0 \":\" NR \"]\";total+=length(s)} END{print total}";
                fields = 1;
                Repeat("item\n", "");
                var concatenatedLength = rows * 7;
                for (var i = 1; i <= rows; i++)
                    concatenatedLength += i.ToString(CultureInfo.InvariantCulture).Length;
                expected.Append(concatenatedLength).Append('\n');
                break;
            case "regex-match":
                program = "$0~/^[A-Z]{3}[0-9]{4}$/{n++} END{print n+0}";
                fields = 1;
                for (var i = 0; i < rows; i++)
                    input.Append(i % 2 == 0 ? "ABC0123\n" : "abc0123\n");
                expected.Append(rows / 2).Append('\n');
                break;
            case "regex-captures":
                program = "match($0,/^([[:alpha:]]+)([0-9]+)$/,m){total+=length(m[1])+m[2]} END{print total}";
                fields = 1;
                Repeat("sku007\n", "");
                expected.Append(rows * 10).Append('\n');
                break;
            default:
                throw new ArgumentException("Unknown workload family.");
        }
        return new(name, scale, rows, fields, program, Bytes(input.ToString()), files, Bytes(expected.ToString()), locale);

        void Repeat(string row, string output)
        {
            for (var i = 0; i < rows; i++)
            {
                input.Append(row);
                expected.Append(output);
            }
        }
    }

    private static byte[] Bytes(string value) => new UTF8Encoding(false, true).GetBytes(value);
}

internal sealed class PreparedReferenceWorkload : IDisposable
{
    public PreparedReferenceWorkload(ReferenceWorkload workload)
    {
        Workload = workload;
        DirectoryPath = Path.Combine(Path.GetTempPath(), "lokad-awk-benchmark-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
        try
        {
            foreach (var file in workload.Files)
                File.WriteAllBytes(Path.Combine(DirectoryPath, file.Path.TrimStart('/')), file.ContentBytes);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public ReferenceWorkload Workload { get; }
    public string DirectoryPath { get; }

    public void Dispose()
    {
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        var resolved = Path.GetFullPath(DirectoryPath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(Path.GetDirectoryName(resolved), temporaryRoot, comparison) ||
            !Path.GetFileName(resolved).StartsWith("lokad-awk-benchmark-", StringComparison.Ordinal))
            throw new InvalidOperationException("Fixture cleanup target is outside the owned temporary directory.");
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}
