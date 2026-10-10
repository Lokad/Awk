namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SortingCompatibilityCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-3]\n[2]:number:[0]\n[3]:number:[0]\n[4]:number:[1.5]\n[5]:number:[2]\n[6]:number:[10]\n[7]:number:[100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[a]\n[2]:string:[b]\n[3]:string:[m]\n[4]:string:[q]\n[5]:string:[r]\n[6]:string:[t]\n[7]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-3]\n[2]:number:[0]\n[3]:number:[0]\n[4]:number:[1.5]\n[5]:number:[2]\n[6]:number:[10]\n[7]:number:[100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[m]\n[2]:string:[q]\n[3]:string:[r]\n[4]:string:[t]\n[5]:string:[a]\n[6]:string:[z]\n[7]:string:[b]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[m]:number:[-3]\n[q]:number:[0]\n[r]:number:[0]\n[t]:number:[1.5]\n[a]:number:[2]\n[z]:number:[10]\n[b]:number:[100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[100]\n[2]:number:[10]\n[3]:number:[2]\n[4]:number:[1.5]\n[5]:number:[0]\n[6]:number:[0]\n[7]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[b]\n[2]:string:[z]\n[3]:string:[a]\n[4]:string:[t]\n[5]:string:[r]\n[6]:string:[q]\n[7]:string:[m]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[b]:number:[100]\n[z]:number:[10]\n[a]:number:[2]\n[t]:number:[1.5]\n[r]:number:[0]\n[q]:number:[0]\n[m]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[2]\n[2]:number:[100]\n[3]:number:[-3]\n[4]:number:[0]\n[5]:number:[0]\n[6]:number:[1.5]\n[7]:number:[10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[a]\n[2]:string:[b]\n[3]:string:[m]\n[4]:string:[q]\n[5]:string:[r]\n[6]:string:[t]\n[7]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[a]:number:[2]\n[b]:number:[100]\n[m]:number:[-3]\n[q]:number:[0]\n[r]:number:[0]\n[t]:number:[1.5]\n[z]:number:[10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[10]\n[2]:number:[1.5]\n[3]:number:[0]\n[4]:number:[0]\n[5]:number:[-3]\n[6]:number:[100]\n[7]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[z]\n[2]:string:[t]\n[3]:string:[r]\n[4]:string:[q]\n[5]:string:[m]\n[6]:string:[b]\n[7]:string:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[z]:number:[10]\n[t]:number:[1.5]\n[r]:number:[0]\n[q]:number:[0]\n[m]:number:[-3]\n[b]:number:[100]\n[a]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-3]\n[2]:number:[0]\n[3]:number:[0]\n[4]:number:[1.5]\n[5]:number:[2]\n[6]:number:[10]\n[7]:number:[100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[m]\n[2]:string:[q]\n[3]:string:[r]\n[4]:string:[t]\n[5]:string:[a]\n[6]:string:[z]\n[7]:string:[b]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[m]:number:[-3]\n[q]:number:[0]\n[r]:number:[0]\n[t]:number:[1.5]\n[a]:number:[2]\n[z]:number:[10]\n[b]:number:[100]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[100]\n[2]:number:[10]\n[3]:number:[2]\n[4]:number:[1.5]\n[5]:number:[0]\n[6]:number:[0]\n[7]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[b]\n[2]:string:[z]\n[3]:string:[a]\n[4]:string:[t]\n[5]:string:[r]\n[6]:string:[q]\n[7]:string:[m]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[b]:number:[100]\n[z]:number:[10]\n[a]:number:[2]\n[t]:number:[1.5]\n[r]:number:[0]\n[q]:number:[0]\n[m]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("7 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("7 7 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-numbers-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=10;a[\"a\"]=2;a[\"m\"]=-3;a[\"b\"]=100;a[\"q\"]=0;a[\"r\"]=-0;a[\"t\"]=1.5;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("7 7 7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[a]\n[2]:string:[e]\n[3]:string:[m]\n[4]:string:[n]\n[5]:string:[x]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[z]\n[2]:string:[m]\n[3]:string:[e]\n[4]:string:[a]\n[5]:string:[n]\n[6]:string:[x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[z]:number:[2]\n[m]:number:[10]\n[e]:string:[]\n[a]:string:[10]\n[n]:string:[2]\n[x]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[cat]\n[2]:string:[2]\n[3]:string:[10]\n[4]:string:[]\n[5]:number:[10]\n[6]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[x]\n[2]:string:[n]\n[3]:string:[a]\n[4]:string:[e]\n[5]:string:[m]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[x]:string:[cat]\n[n]:string:[2]\n[a]:string:[10]\n[e]:string:[]\n[m]:number:[10]\n[z]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[10]\n[2]:string:[]\n[3]:number:[10]\n[4]:string:[2]\n[5]:string:[cat]\n[6]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[a]\n[2]:string:[e]\n[3]:string:[m]\n[4]:string:[n]\n[5]:string:[x]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[a]:string:[10]\n[e]:string:[]\n[m]:number:[10]\n[n]:string:[2]\n[x]:string:[cat]\n[z]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:string:[cat]\n[3]:string:[2]\n[4]:number:[10]\n[5]:string:[]\n[6]:string:[10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[z]\n[2]:string:[x]\n[3]:string:[n]\n[4]:string:[m]\n[5]:string:[e]\n[6]:string:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[z]:number:[2]\n[x]:string:[cat]\n[n]:string:[2]\n[m]:number:[10]\n[e]:string:[]\n[a]:string:[10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[]\n[2]:string:[cat]\n[3]:string:[2]\n[4]:number:[2]\n[5]:string:[10]\n[6]:number:[10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[e]\n[2]:string:[x]\n[3]:string:[n]\n[4]:string:[z]\n[5]:string:[a]\n[6]:string:[m]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[e]:string:[]\n[x]:string:[cat]\n[n]:string:[2]\n[z]:number:[2]\n[a]:string:[10]\n[m]:number:[10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[10]\n[2]:string:[10]\n[3]:number:[2]\n[4]:string:[2]\n[5]:string:[cat]\n[6]:string:[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[m]\n[2]:string:[a]\n[3]:string:[z]\n[4]:string:[n]\n[5]:string:[x]\n[6]:string:[e]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[m]:number:[10]\n[a]:string:[10]\n[z]:number:[2]\n[n]:string:[2]\n[x]:string:[cat]\n[e]:string:[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("6 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("6 6 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-mixed-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[-1]\n[2]:unassigned:[]\n[3]:number:[0]\n[4]:number:[2]\n[5]:string:[]\n[6]:string:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[empty]\n[2]:string:[minus]\n[3]:string:[s]\n[4]:string:[two]\n[5]:string:[u]\n[6]:string:[zero]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[-1]\n[2]:unassigned:[]\n[3]:number:[0]\n[4]:number:[2]\n[5]:string:[]\n[6]:string:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[minus]\n[2]:string:[u]\n[3]:string:[zero]\n[4]:string:[two]\n[5]:string:[empty]\n[6]:string:[s]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[minus]:number:[-1]\n[u]:unassigned:[]\n[zero]:number:[0]\n[two]:number:[2]\n[empty]:string:[]\n[s]:string:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[1]\n[2]:string:[]\n[3]:number:[2]\n[4]:number:[0]\n[5]:unassigned:[]\n[6]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[s]\n[2]:string:[empty]\n[3]:string:[two]\n[4]:string:[zero]\n[5]:string:[u]\n[6]:string:[minus]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[s]:string:[1]\n[empty]:string:[]\n[two]:number:[2]\n[zero]:number:[0]\n[u]:unassigned:[]\n[minus]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[]\n[2]:number:[-1]\n[3]:string:[1]\n[4]:number:[2]\n[5]:unassigned:[]\n[6]:number:[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[empty]\n[2]:string:[minus]\n[3]:string:[s]\n[4]:string:[two]\n[5]:string:[u]\n[6]:string:[zero]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[empty]:string:[]\n[minus]:number:[-1]\n[s]:string:[1]\n[two]:number:[2]\n[u]:unassigned:[]\n[zero]:number:[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[0]\n[2]:unassigned:[]\n[3]:number:[2]\n[4]:string:[1]\n[5]:number:[-1]\n[6]:string:[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[zero]\n[2]:string:[u]\n[3]:string:[two]\n[4]:string:[s]\n[5]:string:[minus]\n[6]:string:[empty]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[zero]:number:[0]\n[u]:unassigned:[]\n[two]:number:[2]\n[s]:string:[1]\n[minus]:number:[-1]\n[empty]:string:[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[-1]\n[2]:string:[]\n[3]:unassigned:[]\n[4]:number:[0]\n[5]:string:[1]\n[6]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[minus]\n[2]:string:[empty]\n[3]:string:[u]\n[4]:string:[zero]\n[5]:string:[s]\n[6]:string:[two]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[minus]:number:[-1]\n[empty]:string:[]\n[u]:unassigned:[]\n[zero]:number:[0]\n[s]:string:[1]\n[two]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:string:[1]\n[3]:number:[0]\n[4]:unassigned:[]\n[5]:string:[]\n[6]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("6\n[1]:string:[two]\n[2]:string:[s]\n[3]:string:[zero]\n[4]:string:[u]\n[5]:string:[empty]\n[6]:string:[minus]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[two]:number:[2]\n[s]:string:[1]\n[zero]:number:[0]\n[u]:unassigned:[]\n[empty]:string:[]\n[minus]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("6 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("6 6 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unassigned-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"u\"]=u;a[\"minus\"]=-1;a[\"zero\"]=0;a[\"two\"]=2;a[\"empty\"]=\"\";a[\"s\"]=\"1\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[2]\n[2]:string:[ 2]\n[3]:string:[02]\n[4]:string:[2]\n[5]:string:[2.0]\n[6]:string:[cat]\n[7]:string:[dog]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[a]\n[2]:string:[b]\n[3]:string:[c]\n[4]:string:[m]\n[5]:string:[n]\n[6]:string:[u]\n[7]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[2]\n[2]:string:[ 2]\n[3]:string:[02]\n[4]:string:[2]\n[5]:string:[2.0]\n[6]:string:[cat]\n[7]:string:[dog]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[u]\n[2]:string:[n]\n[3]:string:[z]\n[4]:string:[a]\n[5]:string:[m]\n[6]:string:[c]\n[7]:string:[b]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[u]:number:[2]\n[n]:string:[ 2]\n[z]:string:[02]\n[a]:string:[2]\n[m]:string:[2.0]\n[c]:string:[cat]\n[b]:string:[dog]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[dog]\n[2]:string:[cat]\n[3]:string:[2.0]\n[4]:string:[2]\n[5]:string:[02]\n[6]:string:[ 2]\n[7]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[b]\n[2]:string:[c]\n[3]:string:[m]\n[4]:string:[a]\n[5]:string:[z]\n[6]:string:[n]\n[7]:string:[u]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[b]:string:[dog]\n[c]:string:[cat]\n[m]:string:[2.0]\n[a]:string:[2]\n[z]:string:[02]\n[n]:string:[ 2]\n[u]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[2]\n[2]:string:[dog]\n[3]:string:[cat]\n[4]:string:[2.0]\n[5]:string:[ 2]\n[6]:number:[2]\n[7]:string:[02]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[a]\n[2]:string:[b]\n[3]:string:[c]\n[4]:string:[m]\n[5]:string:[n]\n[6]:string:[u]\n[7]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[a]:string:[2]\n[b]:string:[dog]\n[c]:string:[cat]\n[m]:string:[2.0]\n[n]:string:[ 2]\n[u]:number:[2]\n[z]:string:[02]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[02]\n[2]:number:[2]\n[3]:string:[ 2]\n[4]:string:[2.0]\n[5]:string:[cat]\n[6]:string:[dog]\n[7]:string:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[z]\n[2]:string:[u]\n[3]:string:[n]\n[4]:string:[m]\n[5]:string:[c]\n[6]:string:[b]\n[7]:string:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[z]:string:[02]\n[u]:number:[2]\n[n]:string:[ 2]\n[m]:string:[2.0]\n[c]:string:[cat]\n[b]:string:[dog]\n[a]:string:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[cat]\n[2]:string:[dog]\n[3]:string:[ 2]\n[4]:string:[02]\n[5]:string:[2]\n[6]:number:[2]\n[7]:string:[2.0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[c]\n[2]:string:[b]\n[3]:string:[n]\n[4]:string:[z]\n[5]:string:[a]\n[6]:string:[u]\n[7]:string:[m]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[c]:string:[cat]\n[b]:string:[dog]\n[n]:string:[ 2]\n[z]:string:[02]\n[a]:string:[2]\n[u]:number:[2]\n[m]:string:[2.0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[2.0]\n[2]:number:[2]\n[3]:string:[2]\n[4]:string:[02]\n[5]:string:[ 2]\n[6]:string:[dog]\n[7]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[m]\n[2]:string:[u]\n[3]:string:[a]\n[4]:string:[z]\n[5]:string:[n]\n[6]:string:[b]\n[7]:string:[c]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[m]:string:[2.0]\n[u]:number:[2]\n[a]:string:[2]\n[z]:string:[02]\n[n]:string:[ 2]\n[b]:string:[dog]\n[c]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("7 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("7 7 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-ties-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=\"02\";a[\"a\"]=\"2\";a[\"m\"]=\"2.0\";a[\"n\"]=\" 2\";a[\"u\"]=2;a[\"b\"]=\"dog\";a[\"c\"]=\"cat\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("7 7 7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[-3]\n[2]:number:[-2]\n[3]:number:[-1]\n[4]:number:[0]\n[5]:number:[1]\n[6]:number:[2]\n[7]:number:[3]\n[8]:number:[4]\n[9]:number:[5]\n[10]:number:[6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[+inf]\n[2]:string:[+nan]\n[3]:string:[-0]\n[4]:string:[0]\n[5]:string:[01]\n[6]:string:[0x10]\n[7]:string:[1]\n[8]:string:[1.0]\n[9]:string:[1x]\n[10]:string:[x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[-3]\n[2]:number:[-2]\n[3]:number:[-1]\n[4]:number:[0]\n[5]:number:[1]\n[6]:number:[2]\n[7]:number:[3]\n[8]:number:[4]\n[9]:number:[5]\n[10]:number:[6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[0x10]\n[2]:string:[+nan]\n[3]:string:[+inf]\n[4]:string:[0]\n[5]:string:[-0]\n[6]:string:[x]\n[7]:string:[1x]\n[8]:string:[1.0]\n[9]:string:[01]\n[10]:string:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[0x10]:number:[-3]\n[+nan]:number:[-2]\n[+inf]:number:[-1]\n[0]:number:[0]\n[-0]:number:[1]\n[x]:number:[2]\n[1x]:number:[3]\n[1.0]:number:[4]\n[01]:number:[5]\n[1]:number:[6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[6]\n[2]:number:[5]\n[3]:number:[4]\n[4]:number:[3]\n[5]:number:[2]\n[6]:number:[1]\n[7]:number:[0]\n[8]:number:[-1]\n[9]:number:[-2]\n[10]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[1]\n[2]:string:[01]\n[3]:string:[1.0]\n[4]:string:[1x]\n[5]:string:[x]\n[6]:string:[-0]\n[7]:string:[0]\n[8]:string:[+inf]\n[9]:string:[+nan]\n[10]:string:[0x10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[1]:number:[6]\n[01]:number:[5]\n[1.0]:number:[4]\n[1x]:number:[3]\n[x]:number:[2]\n[-0]:number:[1]\n[0]:number:[0]\n[+inf]:number:[-1]\n[+nan]:number:[-2]\n[0x10]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[1]\n[2]:number:[0]\n[3]:number:[-3]\n[4]:number:[2]\n[5]:number:[5]\n[6]:number:[6]\n[7]:number:[4]\n[8]:number:[3]\n[9]:number:[-1]\n[10]:number:[-2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[-0]\n[2]:string:[0]\n[3]:string:[0x10]\n[4]:string:[x]\n[5]:string:[01]\n[6]:string:[1]\n[7]:string:[1.0]\n[8]:string:[1x]\n[9]:string:[+inf]\n[10]:string:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[-0]:number:[1]\n[0]:number:[0]\n[0x10]:number:[-3]\n[x]:number:[2]\n[01]:number:[5]\n[1]:number:[6]\n[1.0]:number:[4]\n[1x]:number:[3]\n[+inf]:number:[-1]\n[+nan]:number:[-2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[-2]\n[2]:number:[-1]\n[3]:number:[3]\n[4]:number:[4]\n[5]:number:[6]\n[6]:number:[5]\n[7]:number:[2]\n[8]:number:[-3]\n[9]:number:[0]\n[10]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[+nan]\n[2]:string:[+inf]\n[3]:string:[1x]\n[4]:string:[1.0]\n[5]:string:[1]\n[6]:string:[01]\n[7]:string:[x]\n[8]:string:[0x10]\n[9]:string:[0]\n[10]:string:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[+nan]:number:[-2]\n[+inf]:number:[-1]\n[1x]:number:[3]\n[1.0]:number:[4]\n[1]:number:[6]\n[01]:number:[5]\n[x]:number:[2]\n[0x10]:number:[-3]\n[0]:number:[0]\n[-0]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[-3]\n[2]:number:[-2]\n[3]:number:[-1]\n[4]:number:[0]\n[5]:number:[1]\n[6]:number:[2]\n[7]:number:[3]\n[8]:number:[4]\n[9]:number:[5]\n[10]:number:[6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[0x10]\n[2]:string:[+nan]\n[3]:string:[+inf]\n[4]:string:[0]\n[5]:string:[-0]\n[6]:string:[x]\n[7]:string:[1x]\n[8]:string:[1.0]\n[9]:string:[01]\n[10]:string:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[0x10]:number:[-3]\n[+nan]:number:[-2]\n[+inf]:number:[-1]\n[0]:number:[0]\n[-0]:number:[1]\n[x]:number:[2]\n[1x]:number:[3]\n[1.0]:number:[4]\n[01]:number:[5]\n[1]:number:[6]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:number:[6]\n[2]:number:[5]\n[3]:number:[4]\n[4]:number:[3]\n[5]:number:[2]\n[6]:number:[1]\n[7]:number:[0]\n[8]:number:[-1]\n[9]:number:[-2]\n[10]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("10\n[1]:string:[1]\n[2]:string:[01]\n[3]:string:[1.0]\n[4]:string:[1x]\n[5]:string:[x]\n[6]:string:[-0]\n[7]:string:[0]\n[8]:string:[+inf]\n[9]:string:[+nan]\n[10]:string:[0x10]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[1]:number:[6]\n[01]:number:[5]\n[1.0]:number:[4]\n[1x]:number:[3]\n[x]:number:[2]\n[-0]:number:[1]\n[0]:number:[0]\n[+inf]:number:[-1]\n[+nan]:number:[-2]\n[0x10]:number:[-3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("10 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("10 10 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-indices-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("10 10 10\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-inf]\n[2]:number:[-1]\n[3]:number:[0]\n[4]:number:[1]\n[5]:number:[+inf]\n[6]:number:[-nan]\n[7]:number:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[neg]\n[2]:string:[ni]\n[3]:string:[nn]\n[4]:string:[pi]\n[5]:string:[pn]\n[6]:string:[pos]\n[7]:string:[zero]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-inf]\n[2]:number:[-1]\n[3]:number:[0]\n[4]:number:[1]\n[5]:number:[+inf]\n[6]:number:[-nan]\n[7]:number:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[ni]\n[2]:string:[neg]\n[3]:string:[zero]\n[4]:string:[pos]\n[5]:string:[pi]\n[6]:string:[nn]\n[7]:string:[pn]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[ni]:number:[-inf]\n[neg]:number:[-1]\n[zero]:number:[0]\n[pos]:number:[1]\n[pi]:number:[+inf]\n[nn]:number:[-nan]\n[pn]:number:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[+nan]\n[2]:number:[-nan]\n[3]:number:[+inf]\n[4]:number:[1]\n[5]:number:[0]\n[6]:number:[-1]\n[7]:number:[-inf]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[pn]\n[2]:string:[nn]\n[3]:string:[pi]\n[4]:string:[pos]\n[5]:string:[zero]\n[6]:string:[neg]\n[7]:string:[ni]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[pn]:number:[+nan]\n[nn]:number:[-nan]\n[pi]:number:[+inf]\n[pos]:number:[1]\n[zero]:number:[0]\n[neg]:number:[-1]\n[ni]:number:[-inf]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-3",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@ind_str_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-1]\n[2]:number:[-inf]\n[3]:number:[-nan]\n[4]:number:[+inf]\n[5]:number:[+nan]\n[6]:number:[1]\n[7]:number:[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-3",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@ind_str_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[neg]\n[2]:string:[ni]\n[3]:string:[nn]\n[4]:string:[pi]\n[5]:string:[pn]\n[6]:string:[pos]\n[7]:string:[zero]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-3",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[neg]:number:[-1]\n[ni]:number:[-inf]\n[nn]:number:[-nan]\n[pi]:number:[+inf]\n[pn]:number:[+nan]\n[pos]:number:[1]\n[zero]:number:[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-4",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@ind_str_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[0]\n[2]:number:[1]\n[3]:number:[+nan]\n[4]:number:[+inf]\n[5]:number:[-nan]\n[6]:number:[-inf]\n[7]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-4",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@ind_str_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[zero]\n[2]:string:[pos]\n[3]:string:[pn]\n[4]:string:[pi]\n[5]:string:[nn]\n[6]:string:[ni]\n[7]:string:[neg]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-4",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@ind_str_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[zero]:number:[0]\n[pos]:number:[1]\n[pn]:number:[+nan]\n[pi]:number:[+inf]\n[nn]:number:[-nan]\n[ni]:number:[-inf]\n[neg]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-1]\n[2]:number:[-inf]\n[3]:number:[-nan]\n[4]:number:[+inf]\n[5]:number:[+nan]\n[6]:number:[1]\n[7]:number:[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[neg]\n[2]:string:[ni]\n[3]:string:[nn]\n[4]:string:[pi]\n[5]:string:[pn]\n[6]:string:[pos]\n[7]:string:[zero]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[neg]:number:[-1]\n[ni]:number:[-inf]\n[nn]:number:[-nan]\n[pi]:number:[+inf]\n[pn]:number:[+nan]\n[pos]:number:[1]\n[zero]:number:[0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[0]\n[2]:number:[1]\n[3]:number:[+nan]\n[4]:number:[+inf]\n[5]:number:[-nan]\n[6]:number:[-inf]\n[7]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[zero]\n[2]:string:[pos]\n[3]:string:[pn]\n[4]:string:[pi]\n[5]:string:[nn]\n[6]:string:[ni]\n[7]:string:[neg]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[zero]:number:[0]\n[pos]:number:[1]\n[pn]:number:[+nan]\n[pi]:number:[+inf]\n[nn]:number:[-nan]\n[ni]:number:[-inf]\n[neg]:number:[-1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-7",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@val_str_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[+inf]\n[2]:string:[+nan]\n[3]:number:[-1]\n[4]:string:[-inf]\n[5]:string:[-nan]\n[6]:number:[0]\n[7]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-7",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@val_str_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[pi]\n[2]:string:[pn]\n[3]:string:[neg]\n[4]:string:[ni]\n[5]:string:[nn]\n[6]:string:[zero]\n[7]:string:[pos]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-7",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@val_str_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[pi]:number:[+inf]\n[pn]:number:[+nan]\n[neg]:number:[-1]\n[ni]:number:[-inf]\n[nn]:number:[-nan]\n[zero]:number:[0]\n[pos]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-8",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@val_str_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[1]\n[2]:number:[0]\n[3]:string:[-nan]\n[4]:string:[-inf]\n[5]:number:[-1]\n[6]:string:[+nan]\n[7]:string:[+inf]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-8",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@val_str_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[pos]\n[2]:string:[zero]\n[3]:string:[nn]\n[4]:string:[ni]\n[5]:string:[neg]\n[6]:string:[pn]\n[7]:string:[pi]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-8",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@val_str_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[pos]:number:[1]\n[zero]:number:[0]\n[nn]:number:[-nan]\n[ni]:number:[-inf]\n[neg]:number:[-1]\n[pn]:number:[+nan]\n[pi]:number:[+inf]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-inf]\n[2]:number:[-1]\n[3]:number:[0]\n[4]:number:[1]\n[5]:number:[+inf]\n[6]:number:[+nan]\n[7]:number:[-nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[ni]\n[2]:string:[neg]\n[3]:string:[zero]\n[4]:string:[pos]\n[5]:string:[pi]\n[6]:string:[pn]\n[7]:string:[nn]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[ni]:number:[-inf]\n[neg]:number:[-1]\n[zero]:number:[0]\n[pos]:number:[1]\n[pi]:number:[+inf]\n[pn]:number:[+nan]\n[nn]:number:[-nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:number:[-nan]\n[2]:number:[+nan]\n[3]:number:[+inf]\n[4]:number:[1]\n[5]:number:[0]\n[6]:number:[-1]\n[7]:number:[-inf]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("7\n[1]:string:[nn]\n[2]:string:[pn]\n[3]:string:[pi]\n[4]:string:[pos]\n[5]:string:[zero]\n[6]:string:[neg]\n[7]:string:[ni]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[nn]:number:[-nan]\n[pn]:number:[+nan]\n[pi]:number:[+inf]\n[pos]:number:[1]\n[zero]:number:[0]\n[neg]:number:[-1]\n[ni]:number:[-inf]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("7 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("7 7 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-special-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"pn\"]=\"+nan\"+0;a[\"nn\"]=\"-nan\"+0;a[\"pi\"]=\"+inf\"+0;a[\"ni\"]=\"-inf\"+0;a[\"zero\"]=0;a[\"neg\"]=-1;a[\"pos\"]=1;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("7 7 7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[0.3]\n[2]:number:[1.2]\n[3]:number:[1.2]\n[4]:number:[1.2]\n[5]:number:[1.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[a]\n[2]:string:[b]\n[3]:string:[c]\n[4]:string:[d]\n[5]:string:[e]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[0.3]\n[2]:number:[1.2]\n[3]:number:[1.2]\n[4]:number:[1.2]\n[5]:number:[1.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[e]\n[2]:string:[b]\n[3]:string:[a]\n[4]:string:[d]\n[5]:string:[c]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[e]:number:[0.3]\n[b]:number:[1.2]\n[a]:number:[1.2]\n[d]:number:[1.2]\n[c]:number:[1.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[1.3]\n[2]:number:[1.2]\n[3]:number:[1.2]\n[4]:number:[1.2]\n[5]:number:[0.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[c]\n[2]:string:[d]\n[3]:string:[a]\n[4]:string:[b]\n[5]:string:[e]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[c]:number:[1.3]\n[d]:number:[1.2]\n[a]:number:[1.2]\n[b]:number:[1.2]\n[e]:number:[0.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[1.2]\n[2]:number:[1.2]\n[3]:number:[1.3]\n[4]:number:[1.2]\n[5]:number:[0.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[a]\n[2]:string:[b]\n[3]:string:[c]\n[4]:string:[d]\n[5]:string:[e]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[a]:number:[1.2]\n[b]:number:[1.2]\n[c]:number:[1.3]\n[d]:number:[1.2]\n[e]:number:[0.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[0.3]\n[2]:number:[1.2]\n[3]:number:[1.3]\n[4]:number:[1.2]\n[5]:number:[1.2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[e]\n[2]:string:[d]\n[3]:string:[c]\n[4]:string:[b]\n[5]:string:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[e]:number:[0.3]\n[d]:number:[1.2]\n[c]:number:[1.3]\n[b]:number:[1.2]\n[a]:number:[1.2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[0.3]\n[2]:number:[1.2]\n[3]:number:[1.2]\n[4]:number:[1.2]\n[5]:number:[1.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[e]\n[2]:string:[b]\n[3]:string:[a]\n[4]:string:[d]\n[5]:string:[c]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[e]:number:[0.3]\n[b]:number:[1.2]\n[a]:number:[1.2]\n[d]:number:[1.2]\n[c]:number:[1.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[1.3]\n[2]:number:[1.2]\n[3]:number:[1.2]\n[4]:number:[1.2]\n[5]:number:[0.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[c]\n[2]:string:[d]\n[3]:string:[a]\n[4]:string:[b]\n[5]:string:[e]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[c]:number:[1.3]\n[d]:number:[1.2]\n[a]:number:[1.2]\n[b]:number:[1.2]\n[e]:number:[0.3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("5 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("5 5 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-fractions-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{CONVFMT=\"%.1f\";a[\"b\"]=1.21;a[\"a\"]=1.24;a[\"d\"]=1.25;a[\"c\"]=1.27;a[\"e\"]=1/3;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("5 5 5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[1]\n[2]:number:[2]\n[3]:string:[a]\n[4]:string:[\u00e9]\n[5]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b);print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[z]\n[2]:string:[\u00e4]\n[3]:string:[\u00e9]\n[4]:string:[\u03b1]\n[5]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[1]\n[2]:number:[2]\n[3]:string:[a]\n[4]:string:[\u00e9]\n[5]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[\ud801\udc00]\n[2]:string:[\u00e9]\n[3]:string:[\u03b1]\n[4]:string:[\u00e4]\n[5]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[\ud801\udc00]:number:[1]\n[\u00e9]:number:[2]\n[\u03b1]:string:[a]\n[\u00e4]:string:[\u00e9]\n[z]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[\ud801\udc00]\n[2]:string:[\u00e9]\n[3]:string:[a]\n[4]:number:[2]\n[5]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[z]\n[2]:string:[\u00e4]\n[3]:string:[\u03b1]\n[4]:string:[\u00e9]\n[5]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[z]:string:[\ud801\udc00]\n[\u00e4]:string:[\u00e9]\n[\u03b1]:string:[a]\n[\u00e9]:number:[2]\n[\ud801\udc00]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[\ud801\udc00]\n[2]:string:[\u00e9]\n[3]:number:[2]\n[4]:string:[a]\n[5]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@ind_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[z]\n[2]:string:[\u00e4]\n[3]:string:[\u00e9]\n[4]:string:[\u03b1]\n[5]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-5",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@ind_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[z]:string:[\ud801\udc00]\n[\u00e4]:string:[\u00e9]\n[\u00e9]:number:[2]\n[\u03b1]:string:[a]\n[\ud801\udc00]:number:[1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[1]\n[2]:string:[a]\n[3]:number:[2]\n[4]:string:[\u00e9]\n[5]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@ind_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[\ud801\udc00]\n[2]:string:[\u03b1]\n[3]:string:[\u00e9]\n[4]:string:[\u00e4]\n[5]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-6",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@ind_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[\ud801\udc00]:number:[1]\n[\u03b1]:string:[a]\n[\u00e9]:number:[2]\n[\u00e4]:string:[\u00e9]\n[z]:string:[\ud801\udc00]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[a]\n[2]:string:[\u00e9]\n[3]:string:[\ud801\udc00]\n[4]:number:[1]\n[5]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@val_num_asc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[\u03b1]\n[2]:string:[\u00e4]\n[3]:string:[z]\n[4]:string:[\ud801\udc00]\n[5]:string:[\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[\u03b1]:string:[a]\n[\u00e4]:string:[\u00e9]\n[z]:string:[\ud801\udc00]\n[\ud801\udc00]:number:[1]\n[\u00e9]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asort(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:number:[2]\n[2]:number:[1]\n[3]:string:[\ud801\udc00]\n[4]:string:[\u00e9]\n[5]:string:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@val_num_desc\");print n;for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStdOut("5\n[1]:string:[\u00e9]\n[2]:string:[\ud801\udc00]\n[3]:string:[z]\n[4]:string:[\u00e4]\n[5]:string:[\u03b1]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStdOut("[\u00e9]:number:[2]\n[\ud801\udc00]:number:[1]\n[z]:string:[\ud801\udc00]\n[\u00e4]:string:[\u00e9]\n[\u03b1]:string:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asort-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("5 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-asorti-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";n=asorti(a,b,\"@unsorted\");ok=1;for(i=1;i<=n;i++){seen[b[i]]=1;if(!(b[i] in a))ok=0}print n,length(seen),ok}"], [])
            .WithStdOut("5 5 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-unicode-traversal-11",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"\u00e9\"]=2;a[\"\u03b1\"]=\"a\";a[\"\ud801\udc00\"]=1;a[\"\u00e4\"]=\"\u00e9\";a[\"z\"]=\"\ud801\udc00\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("5 5 5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asort-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b);for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[-0]\n[2]:strnum:[2]\n[3]:strnum:[ 2]\n[4]:strnum:[2 ]\n[5]:strnum:[010]\n[6]:strnum:[+inf]\n[7]:strnum:[+nan]\n[8]:string:[0x10]\n[9]:string:[2x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asorti-0",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b);for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k1]\n[2]:string:[k2]\n[3]:string:[k3]\n[4]:string:[k4]\n[5]:string:[k5]\n[6]:string:[k6]\n[7]:string:[k7]\n[8]:string:[k8]\n[9]:string:[k9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asort-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[-0]\n[2]:strnum:[2]\n[3]:strnum:[ 2]\n[4]:strnum:[2 ]\n[5]:strnum:[010]\n[6]:strnum:[+inf]\n[7]:strnum:[+nan]\n[8]:string:[0x10]\n[9]:string:[2x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asorti-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k4]\n[2]:string:[k2]\n[3]:string:[k7]\n[4]:string:[k8]\n[5]:string:[k1]\n[6]:string:[k6]\n[7]:string:[k5]\n[8]:string:[k9]\n[9]:string:[k3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-traversal-1",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k4]:strnum:[-0]\n[k2]:strnum:[2]\n[k7]:strnum:[ 2]\n[k8]:strnum:[2 ]\n[k1]:strnum:[010]\n[k6]:strnum:[+inf]\n[k5]:strnum:[+nan]\n[k9]:string:[0x10]\n[k3]:string:[2x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asort-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[2x]\n[2]:string:[0x10]\n[3]:strnum:[+nan]\n[4]:strnum:[+inf]\n[5]:strnum:[010]\n[6]:strnum:[2 ]\n[7]:strnum:[ 2]\n[8]:strnum:[2]\n[9]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asorti-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k3]\n[2]:string:[k9]\n[3]:string:[k5]\n[4]:string:[k6]\n[5]:string:[k1]\n[6]:string:[k8]\n[7]:string:[k7]\n[8]:string:[k2]\n[9]:string:[k4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-traversal-2",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k3]:string:[2x]\n[k9]:string:[0x10]\n[k5]:strnum:[+nan]\n[k6]:strnum:[+inf]\n[k1]:strnum:[010]\n[k8]:strnum:[2 ]\n[k7]:strnum:[ 2]\n[k2]:strnum:[2]\n[k4]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asort-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_num_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[-0]\n[2]:string:[0x10]\n[3]:strnum:[ 2]\n[4]:strnum:[2]\n[5]:strnum:[2 ]\n[6]:string:[2x]\n[7]:strnum:[010]\n[8]:strnum:[+inf]\n[9]:strnum:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asorti-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_num_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k4]\n[2]:string:[k9]\n[3]:string:[k7]\n[4]:string:[k2]\n[5]:string:[k8]\n[6]:string:[k3]\n[7]:string:[k1]\n[8]:string:[k6]\n[9]:string:[k5]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-traversal-9",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k4]:strnum:[-0]\n[k9]:string:[0x10]\n[k7]:strnum:[ 2]\n[k2]:strnum:[2]\n[k8]:strnum:[2 ]\n[k3]:string:[2x]\n[k1]:strnum:[010]\n[k6]:strnum:[+inf]\n[k5]:strnum:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asort-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_num_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[+nan]\n[2]:strnum:[+inf]\n[3]:strnum:[010]\n[4]:string:[2x]\n[5]:strnum:[2 ]\n[6]:strnum:[2]\n[7]:strnum:[ 2]\n[8]:string:[0x10]\n[9]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-asorti-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_num_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k5]\n[2]:string:[k6]\n[3]:string:[k1]\n[4]:string:[k3]\n[5]:string:[k8]\n[6]:string:[k2]\n[7]:string:[k7]\n[8]:string:[k9]\n[9]:string:[k4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-gnu-traversal-10",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k5]:strnum:[+nan]\n[k6]:strnum:[+inf]\n[k1]:strnum:[010]\n[k3]:string:[2x]\n[k8]:strnum:[2 ]\n[k2]:strnum:[2]\n[k7]:strnum:[ 2]\n[k9]:string:[0x10]\n[k4]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asort-0",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b);for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[-0]\n[2]:strnum:[2]\n[3]:strnum:[ 2]\n[4]:strnum:[2 ]\n[5]:strnum:[010]\n[6]:strnum:[0x10]\n[7]:strnum:[+inf]\n[8]:strnum:[+nan]\n[9]:string:[2x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asorti-0",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b);for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k1]\n[2]:string:[k2]\n[3]:string:[k3]\n[4]:string:[k4]\n[5]:string:[k5]\n[6]:string:[k6]\n[7]:string:[k7]\n[8]:string:[k8]\n[9]:string:[k9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asort-1",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[-0]\n[2]:strnum:[2]\n[3]:strnum:[ 2]\n[4]:strnum:[2 ]\n[5]:strnum:[010]\n[6]:strnum:[0x10]\n[7]:strnum:[+inf]\n[8]:strnum:[+nan]\n[9]:string:[2x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asorti-1",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k4]\n[2]:string:[k2]\n[3]:string:[k7]\n[4]:string:[k8]\n[5]:string:[k1]\n[6]:string:[k9]\n[7]:string:[k6]\n[8]:string:[k5]\n[9]:string:[k3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-traversal-1",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k4]:strnum:[-0]\n[k2]:strnum:[2]\n[k7]:strnum:[ 2]\n[k8]:strnum:[2 ]\n[k1]:strnum:[010]\n[k9]:strnum:[0x10]\n[k6]:strnum:[+inf]\n[k5]:strnum:[+nan]\n[k3]:string:[2x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asort-2",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[2x]\n[2]:strnum:[+nan]\n[3]:strnum:[+inf]\n[4]:strnum:[0x10]\n[5]:strnum:[010]\n[6]:strnum:[2 ]\n[7]:strnum:[ 2]\n[8]:strnum:[2]\n[9]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asorti-2",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k3]\n[2]:string:[k5]\n[3]:string:[k6]\n[4]:string:[k9]\n[5]:string:[k1]\n[6]:string:[k8]\n[7]:string:[k7]\n[8]:string:[k2]\n[9]:string:[k4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-traversal-2",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k3]:string:[2x]\n[k5]:strnum:[+nan]\n[k6]:strnum:[+inf]\n[k9]:strnum:[0x10]\n[k1]:strnum:[010]\n[k8]:strnum:[2 ]\n[k7]:strnum:[ 2]\n[k2]:strnum:[2]\n[k4]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asort-9",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_num_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[-0]\n[2]:strnum:[ 2]\n[3]:strnum:[2]\n[4]:strnum:[2 ]\n[5]:string:[2x]\n[6]:strnum:[010]\n[7]:strnum:[0x10]\n[8]:strnum:[+inf]\n[9]:strnum:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asorti-9",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_num_asc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k4]\n[2]:string:[k7]\n[3]:string:[k2]\n[4]:string:[k8]\n[5]:string:[k3]\n[6]:string:[k1]\n[7]:string:[k9]\n[8]:string:[k6]\n[9]:string:[k5]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-traversal-9",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_num_asc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k4]:strnum:[-0]\n[k7]:strnum:[ 2]\n[k2]:strnum:[2]\n[k8]:strnum:[2 ]\n[k3]:string:[2x]\n[k1]:strnum:[010]\n[k9]:strnum:[0x10]\n[k6]:strnum:[+inf]\n[k5]:strnum:[+nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asort-10",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asort(a,b,\"@val_num_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:strnum:[+nan]\n[2]:strnum:[+inf]\n[3]:strnum:[0x10]\n[4]:strnum:[010]\n[5]:string:[2x]\n[6]:strnum:[2 ]\n[7]:strnum:[2]\n[8]:strnum:[ 2]\n[9]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-asorti-10",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{n=asorti(a,b,\"@val_num_desc\");for(i=1;i<=n;i++)emit(i,b[i])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[1]:string:[k5]\n[2]:string:[k6]\n[3]:string:[k9]\n[4]:string:[k1]\n[5]:string:[k3]\n[6]:string:[k8]\n[7]:string:[k2]\n[8]:string:[k7]\n[9]:string:[k4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-nondecimal-traversal-10",
                ["--non-decimal-data", "function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} {a[\"k\" NR]=$0}END{PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in a)emit(k,a[k])}"], [])
            .WithStandardInput("010\n2\n2x\n-0\n+nan\n+inf\n 2\n2 \n0x10\n")
            .WithStdOut("[k5]:strnum:[+nan]\n[k6]:strnum:[+inf]\n[k9]:strnum:[0x10]\n[k1]:strnum:[010]\n[k3]:string:[2x]\n[k8]:strnum:[2 ]\n[k2]:strnum:[2]\n[k7]:strnum:[ 2]\n[k4]:strnum:[-0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-inplace-default",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-same-default",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,a,\"\");print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-function-default",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asort(a,a,\"\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-same-@val_type_asc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,a,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-function-@val_type_asc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asort(a,a,\"@val_type_asc\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:number:[2]\n[2]:number:[10]\n[3]:string:[]\n[4]:string:[10]\n[5]:string:[2]\n[6]:string:[cat]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-same-@val_type_desc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,a,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[cat]\n[2]:string:[2]\n[3]:string:[10]\n[4]:string:[]\n[5]:number:[10]\n[6]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-function-@val_type_desc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asort(a,a,\"@val_type_desc\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[cat]\n[2]:string:[2]\n[3]:string:[10]\n[4]:string:[]\n[5]:number:[10]\n[6]:number:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-same-@unsorted",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asort(a,a,\"@unsorted\");print n;for(i=1;i<=n;i++)sum+=(i in a);print sum}"], [])
            .WithStdOut("6\n6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asort-function-@unsorted",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asort(a,a,\"@unsorted\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)sum+=(i in a);print sum}"], [])
            .WithStdOut("6\n6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-inplace-default",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[a]\n[2]:string:[e]\n[3]:string:[m]\n[4]:string:[n]\n[5]:string:[x]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-same-default",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,a,\"\");print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[a]\n[2]:string:[e]\n[3]:string:[m]\n[4]:string:[n]\n[5]:string:[x]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-function-default",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asorti(a,a,\"\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[a]\n[2]:string:[e]\n[3]:string:[m]\n[4]:string:[n]\n[5]:string:[x]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-same-@val_type_asc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,a,\"@val_type_asc\");print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[z]\n[2]:string:[m]\n[3]:string:[e]\n[4]:string:[a]\n[5]:string:[n]\n[6]:string:[x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-function-@val_type_asc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asorti(a,a,\"@val_type_asc\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[z]\n[2]:string:[m]\n[3]:string:[e]\n[4]:string:[a]\n[5]:string:[n]\n[6]:string:[x]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-same-@val_type_desc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,a,\"@val_type_desc\");print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[x]\n[2]:string:[n]\n[3]:string:[a]\n[4]:string:[e]\n[5]:string:[m]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-function-@val_type_desc",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asorti(a,a,\"@val_type_desc\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)emit(i,a[i])}"], [])
            .WithStdOut("6\n[1]:string:[x]\n[2]:string:[n]\n[3]:string:[a]\n[4]:string:[e]\n[5]:string:[m]\n[6]:string:[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-same-@unsorted",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=asorti(a,a,\"@unsorted\");print n;for(i=1;i<=n;i++)sum+=(i in a);print sum}"], [])
            .WithStdOut("6\n6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-alias-asorti-function-@unsorted",
                ["function emit(k,v){printf \"[%s]:%s:[%s]\\n\",k,typeof(v),v} function f(a){return asorti(a,a,\"@unsorted\")} BEGIN{a[\"z\"]=2;a[\"a\"]=\"10\";a[\"m\"]=10;a[\"n\"]=\"2\";a[\"e\"]=\"\";a[\"x\"]=\"cat\";n=f(a);print n;for(i=1;i<=n;i++)sum+=(i in a);print sum}"], [])
            .WithStdOut("6\n6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-empty-@val_type_asc",
                ["BEGIN{print asort(a,b,\"@val_type_asc\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-empty-@val_type_desc",
                ["BEGIN{print asort(a,b,\"@val_type_desc\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-empty-@unsorted",
                ["BEGIN{print asort(a,b,\"@unsorted\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-empty-@bogus",
                ["BEGIN{print asort(a,b,\"@bogus\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-empty-notDefined",
                ["BEGIN{print asort(a,b,\"notDefined\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-one-@val_type_asc",
                ["BEGIN{a[\"x\"]=1;print asort(a,b,\"@val_type_asc\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-one-@val_type_desc",
                ["BEGIN{a[\"x\"]=1;print asort(a,b,\"@val_type_desc\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-one-@unsorted",
                ["BEGIN{a[\"x\"]=1;print asort(a,b,\"@unsorted\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-one-@bogus",
                ["BEGIN{a[\"x\"]=1;print asort(a,b,\"@bogus\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-one-notDefined",
                ["BEGIN{a[\"x\"]=1;print asort(a,b,\"notDefined\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-two-@val_type_asc",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asort(a,b,\"@val_type_asc\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-two-@val_type_desc",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asort(a,b,\"@val_type_desc\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-two-@unsorted",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asort(a,b,\"@unsorted\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-two-@bogus",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asort(a,b,\"@bogus\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asort-two-notDefined",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asort(a,b,\"notDefined\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-empty-@val_type_asc",
                ["BEGIN{print asorti(a,b,\"@val_type_asc\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-empty-@val_type_desc",
                ["BEGIN{print asorti(a,b,\"@val_type_desc\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-empty-@unsorted",
                ["BEGIN{print asorti(a,b,\"@unsorted\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-empty-@bogus",
                ["BEGIN{print asorti(a,b,\"@bogus\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-empty-notDefined",
                ["BEGIN{print asorti(a,b,\"notDefined\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-one-@val_type_asc",
                ["BEGIN{a[\"x\"]=1;print asorti(a,b,\"@val_type_asc\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-one-@val_type_desc",
                ["BEGIN{a[\"x\"]=1;print asorti(a,b,\"@val_type_desc\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-one-@unsorted",
                ["BEGIN{a[\"x\"]=1;print asorti(a,b,\"@unsorted\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-one-@bogus",
                ["BEGIN{a[\"x\"]=1;print asorti(a,b,\"@bogus\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-one-notDefined",
                ["BEGIN{a[\"x\"]=1;print asorti(a,b,\"notDefined\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-two-@val_type_asc",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asorti(a,b,\"@val_type_asc\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-two-@val_type_desc",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asorti(a,b,\"@val_type_desc\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-two-@unsorted",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asorti(a,b,\"@unsorted\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-two-@bogus",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asorti(a,b,\"@bogus\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-asorti-two-notDefined",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;print asorti(a,b,\"notDefined\")}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-empty-@val_type_asc",
                ["BEGIN{PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-empty-@val_type_desc",
                ["BEGIN{PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-empty-@unsorted",
                ["BEGIN{PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-empty-@bogus",
                ["BEGIN{PROCINFO[\"sorted_in\"]=\"@bogus\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-empty-notDefined",
                ["BEGIN{PROCINFO[\"sorted_in\"]=\"notDefined\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-one-@val_type_asc",
                ["BEGIN{a[\"x\"]=1;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-one-@val_type_desc",
                ["BEGIN{a[\"x\"]=1;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-one-@unsorted",
                ["BEGIN{a[\"x\"]=1;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-one-@bogus",
                ["BEGIN{a[\"x\"]=1;PROCINFO[\"sorted_in\"]=\"@bogus\";for(k in a)n++;print n+0}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-one-notDefined",
                ["BEGIN{a[\"x\"]=1;PROCINFO[\"sorted_in\"]=\"notDefined\";for(k in a)n++;print n+0}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-two-@val_type_asc",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-two-@val_type_desc",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-two-@unsorted",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a)n++;print n+0}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-two-@bogus",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;PROCINFO[\"sorted_in\"]=\"@bogus\";for(k in a)n++;print n+0}"], [])
            .WithStdErr("awk: sort comparison function is not defined: @bogus\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-boundary-traversal-two-notDefined",
                ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;PROCINFO[\"sorted_in\"]=\"notDefined\";for(k in a)n++;print n+0}"], [])
            .WithStdErr("awk: sort comparison function is not defined: notDefined\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-gnu-0",
                ["BEGIN{x=\"+nan\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("+nan\n[+nan]\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-gnu-1",
                ["BEGIN{x=\"-nan\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("-nan\n[-nan]\n-nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-gnu-2",
                ["BEGIN{x=\"+inf\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("+inf\n[+inf]\n+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-gnu-3",
                ["BEGIN{x=\"-inf\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("-inf\n[-inf]\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-traditional-0",
                ["--traditional", "BEGIN{x=\"+nan\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("+nan\n[+nan]\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-traditional-1",
                ["--traditional", "BEGIN{x=\"-nan\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("-nan\n[-nan]\n-nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-traditional-2",
                ["--traditional", "BEGIN{x=\"+inf\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("+inf\n[+inf]\n+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-traditional-3",
                ["--traditional", "BEGIN{x=\"-inf\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("-inf\n[-inf]\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-posix-0",
                ["--posix", "BEGIN{x=\"+nan\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("+nan\n[+nan]\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-posix-1",
                ["--posix", "BEGIN{x=\"-nan\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("-nan\n[-nan]\n-nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-posix-2",
                ["--posix", "BEGIN{x=\"+inf\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("+inf\n[+inf]\n+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-format-posix-3",
                ["--posix", "BEGIN{x=\"-inf\"+0;print x;print \"[\"x\"]\";printf \"%s\\n\",x}"], [])
            .WithStdOut("-inf\n[-inf]\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_type_asc-0",
                ["BEGIN{a[\"key\"]=\"+nan\"+0;n=asort(a,b,\"@val_type_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_type_asc-1",
                ["BEGIN{a[\"key\"]=\"-inf\"+0;n=asort(a,b,\"@val_type_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_type_asc-2",
                ["BEGIN{a[\"key\"]=2;n=asort(a,b,\"@val_type_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_type_asc-3",
                ["BEGIN{a[\"key\"]=\"2\";n=asort(a,b,\"@val_type_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 string string\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_str_asc-0",
                ["BEGIN{a[\"key\"]=\"+nan\"+0;n=asort(a,b,\"@val_str_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number string\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_str_asc-1",
                ["BEGIN{a[\"key\"]=\"-inf\"+0;n=asort(a,b,\"@val_str_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number string\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_str_asc-2",
                ["BEGIN{a[\"key\"]=2;n=asort(a,b,\"@val_str_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_str_asc-3",
                ["BEGIN{a[\"key\"]=\"2\";n=asort(a,b,\"@val_str_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 string string\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_num_asc-0",
                ["BEGIN{a[\"key\"]=\"+nan\"+0;n=asort(a,b,\"@val_num_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_num_asc-1",
                ["BEGIN{a[\"key\"]=\"-inf\"+0;n=asort(a,b,\"@val_num_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_num_asc-2",
                ["BEGIN{a[\"key\"]=2;n=asort(a,b,\"@val_num_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@val_num_asc-3",
                ["BEGIN{a[\"key\"]=\"2\";n=asort(a,b,\"@val_num_asc\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 string string\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@unsorted-0",
                ["BEGIN{a[\"key\"]=\"+nan\"+0;n=asort(a,b,\"@unsorted\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@unsorted-1",
                ["BEGIN{a[\"key\"]=\"-inf\"+0;n=asort(a,b,\"@unsorted\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@unsorted-2",
                ["BEGIN{a[\"key\"]=2;n=asort(a,b,\"@unsorted\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 number number\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-singleton-type-@unsorted-3",
                ["BEGIN{a[\"key\"]=\"2\";n=asort(a,b,\"@unsorted\");print n,typeof(a[\"key\"]),typeof(b[1]);printf \"%s\\n\",b[1]}"], [])
            .WithStdOut("1 string string\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-0",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b);for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("+inf\n+nan\n-0\n0\n01\n0x10\n1\n1.0\n1x\nx\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-1",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_type_asc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("0x10\n+nan\n+inf\n0\n-0\nx\n1x\n1.0\n01\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-2",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_type_desc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("1\n01\n1.0\n1x\nx\n-0\n0\n+inf\n+nan\n0x10\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-3",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@ind_str_asc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("+inf\n+nan\n-0\n0\n01\n0x10\n1\n1.0\n1x\nx\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-4",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@ind_str_desc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("x\n1x\n1.0\n1\n0x10\n01\n0\n-0\n+nan\n+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-5",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@ind_num_asc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("-0\n0\nx\n01\n1\n1.0\n1x\n0x10\n+inf\n+nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-6",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@ind_num_desc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("+nan\n+inf\n0x10\n1x\n1.0\n1\n01\nx\n0\n-0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-7",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_str_asc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("+inf\n+nan\n0x10\n0\n-0\nx\n1x\n1.0\n01\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-8",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_str_desc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("1\n01\n1.0\n1x\nx\n-0\n0\n0x10\n+nan\n+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-9",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_num_asc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("0x10\n+nan\n+inf\n0\n-0\nx\n1x\n1.0\n01\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("sorting-input-indices-nondecimal-10",
                ["--non-decimal-data", "BEGIN{a[\"1\"]=6;a[\"01\"]=5;a[\"1.0\"]=4;a[\"1x\"]=3;a[\"x\"]=2;a[\"-0\"]=1;a[\"0\"]=0;a[\"+inf\"]=-1;a[\"+nan\"]=-2;a[\"0x10\"]=-3;n=asorti(a,b,\"@val_num_desc\");for(j=1;j<=n;j++)print b[j]}"], [])
            .WithStdOut("1\n01\n1.0\n1x\nx\n-0\n0\n+inf\n+nan\n0x10\n")
            .Build();

    }
}
