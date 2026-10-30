namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> AssignmentOperandCompatibilityRepairs()
    {
        yield return Passing("numeric-updates-preserve-input-field-spelling-across-targets",
            ["function update(x){x+=$2;return x}{x=8;x+=$2;a[\"row\"][\"sum\"]=8;a[\"row\"][\"sum\"]+=$2;print x,update(8),a[\"row\"][\"sum\"],$2,$2==2;$1+=$2;print $1,$2}"],
            [], "10 10 10 0002 1\n10 0002\n", "8 0002\n");
        yield return AwkGoldenCaseBuilder.Create(
            "array-compound-captures-key-and-left-before-right-effects",
            ["BEGIN{i=1;a[1]=7;print (a[i++] += (a[1]=100)),i,a[1];SUBSEP=\":\";a[1,2]=5;print (a[1,2] += ((SUBSEP=\"/\")?3:0)),a[\"1:2\"],a[1,2]+0}"],
            [])
            .WithStdOut("107 2 107\n8 8 0\n")
            .WithTags([AwkGoldenTag.Posix])
            .WithNondeterminismNote("Assignment operand order is left to the implementation. Lokad captures the target/key and left value before the RHS; GNU awk 5.2.1 returns 200 2 200 and 3 5 3 for these interfering effects.")
            .Build();

        yield return AwkGoldenCaseBuilder.Create(
            "array-compound-preserves-forwarded-alias",
            ["function rhs(a){a[\"k\"]=100;return 3} function bump(a){return a[\"k\"]+=rhs(a)} BEGIN{a[\"k\"]=7;print bump(a),a[\"k\"]}"],
            [])
            .WithStdOut("10 10\n")
            .WithTags([AwkGoldenTag.Posix])
            .WithNondeterminismNote("Lokad captures the compound left value before the RHS mutates the aliased array. Assignment operand order may differ between implementations; GNU awk 5.2.1 returns 103 103.")
            .Build();

        yield return Posix(Passing(
            "assignment-logical-and-conditional-original-repros",
            ["BEGIN{x=0;print (1?x=2:x=3),x;x=0;print (0?x=2:x=3),x;x=0;print (1&&x=2),x;x=0;print (0||x=2),x;x=0;y=0?1:x=2;print x,y}"],
            [],
            "2 2\n3 3\n1 2\n1 2\n2 2\n"));

        yield return Posix(Passing(
            "assignment-operands-preserve-short-circuit-evaluation",
            ["function set(){calls++;return 7} BEGIN{x=0;d=0;print (0&&x=set()),x,calls+0;print (1||x=set()),x,calls+0;print (1?x=4:x=set()),x,calls+0;print (0?x=set():x=5),x,calls+0;print (1&&x=set()),x,calls+0;print (0||x=set()),x,calls+0;print (1||x=1/d),(0&&x=1/d)}"],
            [],
            "0 0 0\n1 0 0\n4 4 0\n5 5 0\n1 7 1\n1 7 2\n1 0\n"));

        yield return Posix(Passing(
            "assignment-operands-retain-full-right-expression",
            ["BEGIN{x=0;print (1&&x=0||1),x;x=0;print (1&&(x=0)||1),x;x=0;print (0||x=0&&2),x;x=0;print (0||x=1?2:3),x;x=y=0;print (1&&x=2&&y=3||4),x,y;x=y=0;print (0||x=2||y=3&&4),x,y;print (1||0&&x=9),x;print (0&&1||x=9),x}"],
            [],
            "1 1\n1 0\n0 0\n1 2\n1 1 1\n1 1 0\n1 1\n1 9\n"));

        yield return Posix(Passing(
            "assignment-operands-support-compound-updates",
            ["BEGIN{x=2;print (1&&x+=3),x;print (0||x*=2),x;print (1?x-=4:x/=0),x;print (0?x%=0:x^=2),x;print (1&&x/=4),x;print (0||x%=5),x}"],
            [],
            "1 5\n1 10\n6 6\n36 36\n1 9\n1 4\n"));

        yield return Posix(Passing(
            "assignment-operands-evaluate-field-and-array-targets-once",
            ["BEGIN{$0=\"a b\";i=0;print (1&&a[++i]=7),i,a[1];print (0||$(++i)=\"changed\"),i,$0;print (1?a[++i]+=3:$(++i)=99),i,a[3],NF;print (0?a[++i]=99:$(++i)=9),i,NF,$0;print (0&&a[++i]=99),(1||$(++i)=99),i;print (1&&NF=2),NF,$0}"],
            [],
            "1 1 7\n1 2 a changed\n3 3 3 2\n9 4 4 a changed  9\n0 1 4\n1 2 a changed\n"));

        yield return Posix(Passing(
            "assignment-operands-preserve-right-associativity",
            ["BEGIN{x=y=z=0;print (1&&x=y=z=2),x,y,z;x=y=0;print (x=1&&y=2),x,y;x=y=0;print (x=0?y=2:y=3),x,y;x=0;print (0?x=1:1?x=2:x=3),x;x=0;print (1?0?x=1:x=2:x=3),x;print (0?1:1&&x=4),x;print (1?0||x=5:3),x}"],
            [],
            "1 2 2 2\n1 1 2\n3 3 3\n2 2\n2 2\n1 4\n1 5\n"));

        yield return Posix(Passing(
            "assignment-comparison-operands-use-expression-grammar",
            ["BEGIN{x=0;print (1<x=2),x;print (2<=x=2),x;print (3>x=2),x;print (2>=x=2),x;print (2==x=2),x;print (1!=x=2),x;print (\"abc\"~p=\"b\"),p;print (\"abc\"!~p=\"z\"),p;print (x=1<y=2),x,y}"],
            [],
            "1 2\n1 2\n1 2\n1 2\n1 2\n1 2\n1 b\n1 z\n1 1 2\n"));

        yield return Posix(Passing(
            "assignment-operands-work-in-control-conditions",
            ["BEGIN{if(1&&x=2)print x;while(x<5&&y=x+=1)print x,y;for(i=0;i<3&&seen=i+1;i++)print i,seen;print i,seen;if(0||a[\"key\"]=8)print a[\"key\"]}"],
            [],
            "2\n3 3\n4 4\n5 5\n0 1\n1 2\n2 3\n3 3\n8\n"));

        yield return Posix(Passing(
            "assignment-comparison-operands-work-in-rule-patterns",
            ["($1==v=\"hit\"){print NR,v} END{print v}"],
            [],
            "1 hit\nhit\n", "hit\nmiss\n"));

        yield return Posix(Passing(
            "assignment-operands-work-in-calls-and-array-indexes",
            ["function f(x){print x;return x} BEGIN{x=0;f(1&&x=2);print x;a[0||x=3]=9;print a[1],x;print (0?x=99:(1&&y=x=4)),x,y}"],
            [],
            "1\n2\n9 3\n1 4 4\n"));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-0",
            ["BEGIN{print (1&&2=3)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-1",
            ["BEGIN{print (0||2+=3)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-2",
            ["BEGIN{print (1?x=2:3=4)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-3",
            ["BEGIN{print (1&&(x)=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-4",
            ["BEGIN{print ((1&&x)=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-5",
            ["BEGIN{print (1+x=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-6",
            ["BEGIN{print (\"prefix\" x=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-7",
            ["BEGIN{print (1<x+1=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-8",
            ["BEGIN{print (1~(x)=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-9",
            ["BEGIN{print (1&&x++=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-10",
            ["BEGIN{print ((1?x:y)=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

        yield return Posix(CompilerDiagnostic(Failing(
            "assignment-invalid-operand-target-11",
            ["BEGIN{print (1&&sin(1)=2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")));

    }
}
