namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> CalendarNormalizationCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-0",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-1",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 32 0 0 0\",1)}"], [])
            .WithStdOut("2678400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-2",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 13 1 0 0 0\",1)}"], [])
            .WithStdOut("31536000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-3",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 0 1 0 0 0\",1)}"], [])
            .WithStdOut("-2678400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-4",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2000 2 29 0 0 0\",1)}"], [])
            .WithStdOut("951782400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-5",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1900 2 29 0 0 0\",1)}"], [])
            .WithStdOut("-2203891200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-6",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2000 2 30 0 0 0\",1)}"], [])
            .WithStdOut("951868800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-7",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1969 12 31 23 59 59\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-8",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"0 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-62167219200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-9",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-1 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-62198755200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-10",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-62135596800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-11",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"9999 12 31 23 59 59\",1)}"], [])
            .WithStdOut("253402300799\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-12",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"10000 1 1 0 0 0\",1)}"], [])
            .WithStdOut("253402300800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-13",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 1 1 0 0 0\",1)}"], [])
            .WithStdOut("67768036160140800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-14",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 12 31 23 59 59\",1)}"], [])
            .WithStdOut("67768036191676800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-15",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485548 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-16",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2147481748 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-67768040609740800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-17",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2147481749 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-18",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"9223372036854775807 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-19",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 -2147483648 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-20",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 -2147483647 1 0 0 0\",1)}"], [])
            .WithStdOut("-5647336533504000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-21",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 -1000000 1 0 0 0\",1)}"], [])
            .WithStdOut("-2629748678400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-22",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 -100 1 0 0 0\",1)}"], [])
            .WithStdOut("-265680000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-23",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 -1 1 0 0 0\",1)}"], [])
            .WithStdOut("-5270400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-24",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 12 1 0 0 0\",1)}"], [])
            .WithStdOut("28857600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-25",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 24 1 0 0 0\",1)}"], [])
            .WithStdOut("60393600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-26",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 31 1 0 0 0\",1)}"], [])
            .WithStdOut("78796800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-27",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 59 1 0 0 0\",1)}"], [])
            .WithStdOut("152496000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-28",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 60 1 0 0 0\",1)}"], [])
            .WithStdOut("155088000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-29",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 100 1 0 0 0\",1)}"], [])
            .WithStdOut("260236800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-30",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1000000 1 0 0 0\",1)}"], [])
            .WithStdOut("2629743235200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-31",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 2147483647 1 0 0 0\",1)}"], [])
            .WithStdOut("5647336528060800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-32",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 -2147483648 0 0 0\",1)}"], [])
            .WithStdOut("-185542587273600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-33",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 -2147483647 0 0 0\",1)}"], [])
            .WithStdOut("-185542587187200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-34",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 -1000000 0 0 0\",1)}"], [])
            .WithStdOut("-86400086400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-35",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 -100 0 0 0\",1)}"], [])
            .WithStdOut("-8726400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-36",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 -1 0 0 0\",1)}"], [])
            .WithStdOut("-172800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-37",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 0 0 0 0\",1)}"], [])
            .WithStdOut("-86400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-38",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 12 0 0 0\",1)}"], [])
            .WithStdOut("950400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-39",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 24 0 0 0\",1)}"], [])
            .WithStdOut("1987200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-40",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 31 0 0 0\",1)}"], [])
            .WithStdOut("2592000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-41",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 59 0 0 0\",1)}"], [])
            .WithStdOut("5011200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-42",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 60 0 0 0\",1)}"], [])
            .WithStdOut("5097600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-43",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 100 0 0 0\",1)}"], [])
            .WithStdOut("8553600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-44",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1000000 0 0 0\",1)}"], [])
            .WithStdOut("86399913600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-45",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 2147483647 0 0 0\",1)}"], [])
            .WithStdOut("185542587014400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-46",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 -2147483648 0 0\",1)}"], [])
            .WithStdOut("-7730941132800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-47",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 -2147483647 0 0\",1)}"], [])
            .WithStdOut("-7730941129200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-48",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 -1000000 0 0\",1)}"], [])
            .WithStdOut("-3600000000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-49",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 -100 0 0\",1)}"], [])
            .WithStdOut("-360000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-50",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 -1 0 0\",1)}"], [])
            .WithStdOut("-3600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-51",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 1 0 0\",1)}"], [])
            .WithStdOut("3600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-52",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 12 0 0\",1)}"], [])
            .WithStdOut("43200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-53",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 24 0 0\",1)}"], [])
            .WithStdOut("86400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-54",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 31 0 0\",1)}"], [])
            .WithStdOut("111600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-55",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 59 0 0\",1)}"], [])
            .WithStdOut("212400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-56",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 60 0 0\",1)}"], [])
            .WithStdOut("216000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-57",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 100 0 0\",1)}"], [])
            .WithStdOut("360000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-58",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 1000000 0 0\",1)}"], [])
            .WithStdOut("3600000000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-59",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 2147483647 0 0\",1)}"], [])
            .WithStdOut("7730941129200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-60",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 -2147483648 0\",1)}"], [])
            .WithStdOut("-128849018880\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-61",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 -2147483647 0\",1)}"], [])
            .WithStdOut("-128849018820\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-62",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 -1000000 0\",1)}"], [])
            .WithStdOut("-60000000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-63",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 -100 0\",1)}"], [])
            .WithStdOut("-6000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-64",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 -1 0\",1)}"], [])
            .WithStdOut("-60\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-65",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 1 0\",1)}"], [])
            .WithStdOut("60\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-66",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 12 0\",1)}"], [])
            .WithStdOut("720\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-67",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 24 0\",1)}"], [])
            .WithStdOut("1440\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-68",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 31 0\",1)}"], [])
            .WithStdOut("1860\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-69",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 59 0\",1)}"], [])
            .WithStdOut("3540\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-70",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 60 0\",1)}"], [])
            .WithStdOut("3600\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-71",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 100 0\",1)}"], [])
            .WithStdOut("6000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-72",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 1000000 0\",1)}"], [])
            .WithStdOut("60000000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-73",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 2147483647 0\",1)}"], [])
            .WithStdOut("128849018820\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-74",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 -2147483648\",1)}"], [])
            .WithStdOut("-2147483648\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-75",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 -2147483647\",1)}"], [])
            .WithStdOut("-2147483647\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-76",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 -1000000\",1)}"], [])
            .WithStdOut("-1000000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-77",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 -100\",1)}"], [])
            .WithStdOut("-100\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-78",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 -1\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-79",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 1\",1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-80",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 12\",1)}"], [])
            .WithStdOut("12\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-81",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 24\",1)}"], [])
            .WithStdOut("24\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-82",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 31\",1)}"], [])
            .WithStdOut("31\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-83",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 59\",1)}"], [])
            .WithStdOut("59\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-84",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 60\",1)}"], [])
            .WithStdOut("60\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-85",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 100\",1)}"], [])
            .WithStdOut("100\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-86",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 1000000\",1)}"], [])
            .WithStdOut("1000000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-87",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 2147483647\",1)}"], [])
            .WithStdOut("2147483647\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-88",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-5145 -393 -508 53 -183 604\",1)}"], [])
            .WithStdOut("-225607758776\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-91",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"10011 256 -769 -63 -113 -167\",1)}"], [])
            .WithStdOut("254353244653\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-94",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"4016 -115 -606 -141 201 281\",1)}"], [])
            .WithStdOut("64207405541\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-97",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"755 -309 -74 -135 -344 787\",1)}"], [])
            .WithStdOut("-39164070653\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-100",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"5948 219 493 -133 -62 -550\",1)}"], [])
            .WithStdOut("126148700930\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-103",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"5190 366 -355 -2 -427 -985\",1)}"], [])
            .WithStdOut("102542423795\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-106",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-6961 -70 -111 145 194 -699\",1)}"], [])
            .WithStdOut("-282030926259\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-109",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"7194 -34 26 -63 -557 -792\",1)}"], [])
            .WithStdOut("164763415788\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-112",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-3456 69 -625 42 96 842\",1)}"], [])
            .WithStdOut("-171103118998\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-115",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-5814 -130 -181 35 144 390\",1)}"], [])
            .WithStdOut("-245999327370\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-118",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-6661 -211 714 148 -506 -644\",1)}"], [])
            .WithStdOut("-272863571804\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-121",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"928 304 -272 149 -342 947\",1)}"], [])
            .WithStdOut("-32108660773\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-124",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-4224 -61 -385 42 -249 804\",1)}"], [])
            .WithStdOut("-195660006936\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-127",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2512 -323 -687 18 -58 743\",1)}"], [])
            .WithStdOut("16192545263\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-130",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-9934 -161 609 96 108 554\",1)}"], [])
            .WithStdOut("-376027135366\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-133",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"3687 -164 714 -80 -48 16\",1)}"], [])
            .WithStdOut("53810665936\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-136",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"5866 -284 789 131 512 -116\",1)}"], [])
            .WithStdOut("122264883004\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-139",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-4891 -104 -698 12 277 -640\",1)}"], [])
            .WithStdOut("-216848820820\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-142",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"4058 26 -319 10 390 -567\",1)}"], [])
            .WithStdOut("65929134033\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-145",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"6853 -357 -207 140 -493 -211\",1)}"], [])
            .WithStdOut("153133501409\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-148",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"8587 -378 595 -111 542 124\",1)}"], [])
            .WithStdOut("207866541844\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-151",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"6363 -17 529 25 349 927\",1)}"], [])
            .WithStdOut("138627961467\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-154",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"10036 -30 -686 115 -488 -383\",1)}"], [])
            .WithStdOut("254397840337\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-157",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1766 277 464 -42 -460 -759\",1)}"], [])
            .WithStdOut("-5671907559\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-160",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-5899 292 -31 15 144 312\",1)}"], [])
            .WithStdOut("-247559178648\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-163",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"10988 -365 304 116 -562 -649\",1)}"], [])
            .WithStdOut("283644584831\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-166",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-6770 -194 -291 -84 385 -456\",1)}"], [])
            .WithStdOut("-276346100556\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-169",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2384 -131 -220 -98 564 257\",1)}"], [])
            .WithStdOut("12698062097\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-172",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2283 136 437 -135 521 19\",1)}"], [])
            .WithStdOut("-133819539521\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-175",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-8619 49 344 -25 -81 -221\",1)}"], [])
            .WithStdOut("-334000722281\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-178",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"78 11 -696 -20 -497 839\",1)}"], [])
            .WithStdOut("-59739739381\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-181",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1463 32 197 -75 -206 -880\",1)}"], [])
            .WithStdOut("-15901252840\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-184",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-9640 319 -159 -147 -359 -828\",1)}"], [])
            .WithStdOut("-365554458768\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-187",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2166 -202 -731 -22 140 -70\",1)}"], [])
            .WithStdOut("-131116650070\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-190",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"11858 -90 627 114 -333 -649\",1)}"], [])
            .WithStdOut("311850216971\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-193",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"11565 -382 18 -52 111 883\",1)}"], [])
            .WithStdOut("301783097143\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-196",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"4717 397 -357 146 52 -989\",1)}"], [])
            .WithStdOut("87697910131\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-199",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"5244 -257 -98 -130 -389 97\",1)}"], [])
            .WithStdOut("102629835157\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-202",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"7857 326 -53 33 126 -477\",1)}"], [])
            .WithStdOut("186625940283\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-205",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-380 334 295 57 -297 -955\",1)}"], [])
            .WithStdOut("-73257595975\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-208",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"8156 -151 673 9 -20 -756\",1)}"], [])
            .WithStdOut("194869528044\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-211",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-4844 369 -796 64 -107 -195\",1)}"], [])
            .WithStdOut("-214129993815\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-214",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2395 -258 339 32 531 7\",1)}"], [])
            .WithStdOut("12759871867\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-217",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-3120 -112 698 23 -500 -397\",1)}"], [])
            .WithStdOut("-160861800397\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-220",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"725 -395 277 25 301 -906\",1)}"], [])
            .WithStdOut("-40305838446\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-223",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-7707 -132 -550 100 -69 -403\",1)}"], [])
            .WithStdOut("-305773650943\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-226",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"6420 -345 -246 -90 -332 -839\",1)}"], [])
            .WithStdOut("139496688841\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-229",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"8354 -359 -447 88 456 -541\",1)}"], [])
            .WithStdOut("200474436419\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-232",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2019 -19 294 121 -193 -436\",1)}"], [])
            .WithStdOut("-125907704416\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-235",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2631 -34 -735 -78 -317 -145\",1)}"], [])
            .WithStdOut("-145349378365\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-238",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"8852 6 578 1 -41 953\",1)}"], [])
            .WithStdOut("217237941293\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-241",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2807 25 -28 -114 121 742\",1)}"], [])
            .WithStdOut("-150687359198\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-244",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"6549 165 -143 -16 320 630\",1)}"], [])
            .WithStdOut("144918077430\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-247",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"3698 -130 -158 -142 -405 -851\",1)}"], [])
            .WithStdOut("54171745249\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-250",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"4949 -396 284 113 -81 -409\",1)}"], [])
            .WithStdOut("92988919931\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-253",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-8555 339 -623 -105 -210 579\",1)}"], [])
            .WithStdOut("-331302486021\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-256",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-9153 -383 619 -110 -352 -216\",1)}"], [])
            .WithStdOut("-351964814136\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-259",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"725 18 515 122 -167 -207\",1)}"], [])
            .WithStdOut("-39198991827\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-262",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-8336 -110 -398 -127 491 7\",1)}"], [])
            .WithStdOut("-325552776533\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-265",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-5108 -189 -38 50 -315 -52\",1)}"], [])
            .WithStdOut("-223863016552\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-268",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2807 385 584 -142 -571 73\",1)}"], [])
            .WithStdOut("-149687854187\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-271",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-7002 -190 -745 -120 -599 473\",1)}"], [])
            .WithStdOut("-283696077067\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-274",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"5057 -227 680 -110 467 605\",1)}"], [])
            .WithStdOut("96875027825\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-277",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"10066 345 280 29 -445 668\",1)}"], [])
            .WithStdOut("256413937568\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-280",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-281",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-282",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-283",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 junk\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-284",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0junk\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-285",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 1junk\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-286",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 junk\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-287",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 -1 ignored\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-288",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"  +1970 +1 +1 -0 +0 +0  \",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-289",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970+1+1+0+0+0\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-290",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0.5\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-291",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970\\t1\\n1\\r0\\v0\\f0\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-292",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-293",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 1\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-294",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0\\0 ignored\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-295",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1\\0 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-296",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 +\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-297",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 -\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-298",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 2147483648\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        // BoundedIntegerPolicy: native scanf integer narrowing is not reproduced.
        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-299",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 4294967297 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        // BoundedIntegerPolicy: native scanf integer narrowing is not reproduced.
        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-300",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 2147483648 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        // BoundedIntegerPolicy: native scanf integer narrowing is not reproduced.
        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-301",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 4294967296\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-302",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 4294967296\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-304",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 1 1 2 0 0\",1)}"], [])
            .WithStdOut("1577844000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-306",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 1 1 2 0 0 -2\",1)}"], [])
            .WithStdOut("1577844000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-308",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 1 1 2 0 0 -1\",1)}"], [])
            .WithStdOut("1577844000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-310",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 1 1 2 0 0 0\",1)}"], [])
            .WithStdOut("1577844000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-312",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 1 1 2 0 0 1\",1)}"], [])
            .WithStdOut("1577844000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-314",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 1 1 2 0 0 2\",1)}"], [])
            .WithStdOut("1577844000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-316",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 7 1 2 0 0\",1)}"], [])
            .WithStdOut("1593568800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-318",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 7 1 2 0 0 -2\",1)}"], [])
            .WithStdOut("1593568800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-320",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 7 1 2 0 0 -1\",1)}"], [])
            .WithStdOut("1593568800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-322",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 7 1 2 0 0 0\",1)}"], [])
            .WithStdOut("1593568800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-324",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 7 1 2 0 0 1\",1)}"], [])
            .WithStdOut("1593568800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-326",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 7 1 2 0 0 2\",1)}"], [])
            .WithStdOut("1593568800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-328",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 1 30 0\",1)}"], [])
            .WithStdOut("1583026200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-330",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 1 30 0 -2\",1)}"], [])
            .WithStdOut("1583026200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-332",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 1 30 0 -1\",1)}"], [])
            .WithStdOut("1583026200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-334",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 1 30 0 0\",1)}"], [])
            .WithStdOut("1583026200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-336",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 1 30 0 1\",1)}"], [])
            .WithStdOut("1583026200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-338",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 1 30 0 2\",1)}"], [])
            .WithStdOut("1583026200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-340",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 2 30 0\",1)}"], [])
            .WithStdOut("1583029800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-342",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 2 30 0 -2\",1)}"], [])
            .WithStdOut("1583029800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-344",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 2 30 0 -1\",1)}"], [])
            .WithStdOut("1583029800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-346",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 2 30 0 0\",1)}"], [])
            .WithStdOut("1583029800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-348",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 2 30 0 1\",1)}"], [])
            .WithStdOut("1583029800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-350",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 2 30 0 2\",1)}"], [])
            .WithStdOut("1583029800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-352",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 3 30 0\",1)}"], [])
            .WithStdOut("1583033400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-354",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 3 30 0 -2\",1)}"], [])
            .WithStdOut("1583033400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-356",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 3 30 0 -1\",1)}"], [])
            .WithStdOut("1583033400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-358",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 3 30 0 0\",1)}"], [])
            .WithStdOut("1583033400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-360",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 3 30 0 1\",1)}"], [])
            .WithStdOut("1583033400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-362",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 3 1 3 30 0 2\",1)}"], [])
            .WithStdOut("1583033400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-364",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 0 30 0\",1)}"], [])
            .WithStdOut("1601512200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-366",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 0 30 0 -2\",1)}"], [])
            .WithStdOut("1601512200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-368",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 0 30 0 -1\",1)}"], [])
            .WithStdOut("1601512200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-370",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 0 30 0 0\",1)}"], [])
            .WithStdOut("1601512200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-372",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 0 30 0 1\",1)}"], [])
            .WithStdOut("1601512200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-374",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 0 30 0 2\",1)}"], [])
            .WithStdOut("1601512200\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-376",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 1 30 0\",1)}"], [])
            .WithStdOut("1601515800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-378",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 1 30 0 -2\",1)}"], [])
            .WithStdOut("1601515800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-380",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 1 30 0 -1\",1)}"], [])
            .WithStdOut("1601515800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-382",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 1 30 0 0\",1)}"], [])
            .WithStdOut("1601515800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-384",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 1 30 0 1\",1)}"], [])
            .WithStdOut("1601515800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-386",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 1 30 0 2\",1)}"], [])
            .WithStdOut("1601515800\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-388",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 2 30 0\",1)}"], [])
            .WithStdOut("1601519400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-390",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 2 30 0 -2\",1)}"], [])
            .WithStdOut("1601519400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-392",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 2 30 0 -1\",1)}"], [])
            .WithStdOut("1601519400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-394",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 2 30 0 0\",1)}"], [])
            .WithStdOut("1601519400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-396",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 2 30 0 1\",1)}"], [])
            .WithStdOut("1601519400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-398",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 2 30 0 2\",1)}"], [])
            .WithStdOut("1601519400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-400",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 3 30 0\",1)}"], [])
            .WithStdOut("1601523000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-402",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 3 30 0 -2\",1)}"], [])
            .WithStdOut("1601523000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-404",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 3 30 0 -1\",1)}"], [])
            .WithStdOut("1601523000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-406",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 3 30 0 0\",1)}"], [])
            .WithStdOut("1601523000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-408",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 3 30 0 1\",1)}"], [])
            .WithStdOut("1601523000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-410",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2020 10 1 3 30 0 2\",1)}"], [])
            .WithStdOut("1601523000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-630",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 -1\",-1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-631",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 0\",-1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-632",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 1\",-1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-633",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 -1\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-634",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 0\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-635",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 1\",1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-639",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 -1\",\"0\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-640",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 0\",\"0\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-641",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 1\",\"0\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-642",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 -1\",\"1\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-643",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 0\",\"1\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-644",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"1970 1 1 0 0 0 1\",\"1\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-effects-663",
                ["function f(){n++;return \"1970 1 32 0 0 0\"}BEGIN{print mktime(f(),++u),n,u}"], [])
            .WithStdOut("2678400 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-effects-664",
                ["BEGIN{print typeof(mktime(\"1970 13 1 0 0 0\",1)),mktime(\"1970 13 1 0 0 0\",1)}"], [])
            .WithStdOut("number 31536000\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-effects-665",
                ["BEGIN{print mktime(1970,1),mktime(\"bad\",1),mktime(\"1970 1 1 0 0 0\",-1)}"], [])
            .WithStdOut("-1 -1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-effects-666",
                ["BEGIN{print mktime(\"1970 1 32 0 0 0\",mktime(\"bad\",1))}"], [])
            .WithStdOut("2678400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-667",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 13 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-668",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 0 1 0 0 0\",1)}"], [])
            .WithStdOut("67768036157462400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-669",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2147481748 0 1 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-670",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2147481748 13 1 0 0 0\",1)}"], [])
            .WithStdOut("-67768040578118400\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-671",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 12 31 23 59 60\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-672",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"-2147481748 1 1 0 0 -1\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-673",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 12 31 2147483647 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("calendar-date-674",
                ["BEGIN{printf \"%.0f\\n\",mktime(\"2147485547 2147483647 -2147483648 0 0 0\",1)}"], [])
            .WithStdOut("-1\n")
            .Build();

    }
}
