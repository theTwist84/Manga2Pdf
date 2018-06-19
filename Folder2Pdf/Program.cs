using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Folder2Pdf
{
    class Program
    {
        static void Main(string[] args)
        {
            ParsedArgs.Add("Base", GetArgument(args, "-Base"));
            ParsedArgs.Add("MangaName", GetArgument(args, "-MangaName"));
            ParsedArgs.Add("MangaNameOG", GetArgument(args, "-MangaNameOG"));

            if (!ParsedArgs.TryGetValue("Base", out String Base))
                Base = "C:\\Users\\Twister\\Downloads";
            if (!ParsedArgs.TryGetValue("MangaName", out String MangaName))
                Base = "Noblesse";
            if (!ParsedArgs.TryGetValue("MangaNameOG", out String MangaNameOG))
                Base = "Manhwa";

            Int32 ChapterCount = GetSubDirCount($"{Base}\\{MangaName}");

            for (Int32 i = 1; i < ChapterCount; i++)
                if (!File.Exists($"{Base}\\{MangaName}\\pdf\\{MangaName} ({MangaNameOG}) {i.ToString("D3")}.pdf"))
                    ToConvert.Add(i);

            if (ToConvert.Count > 0)
            {
                Console.WriteLine(PercentageString(PercentageType.Started, ChapterCount, ChapterCount));
                ChapterCount = ToConvert.Count;
            }

            ToConvert.ForEach(chapter => {
                String Chapter = chapter.ToString("D3");
                String Images = $"{Base}\\{MangaName}\\{Chapter}\\*";
                String Pdf = $"{Base}\\{MangaName}\\pdf\\{MangaName} ({MangaNameOG}) {Chapter}.pdf";
                Boolean exists = File.Exists(Pdf);

                process($"Tools\\ImageMagick\\magick", $"convert -adjoin \"{Images}\" \"{Pdf}\"");
                Console.WriteLine(PercentageString(PercentageType.Converting, chapter, ChapterCount));
            });

            Console.WriteLine(PercentageString((ToConvert.Count < 1 ? PercentageType.NoNeed : PercentageType.Finished), ChapterCount, ChapterCount));
            Console.ReadKey();
        }


        public static String GetArgument(IEnumerable<string> args, string option) => args.SkipWhile(i => i.ToLower() != option.ToLower()).Skip(1).Take(1).FirstOrDefault();
        public static Dictionary<String, String> ParsedArgs = new Dictionary<string, string>();

        public static List<Int32> ToConvert = new List<Int32>();

        public static IEnumerable<String> GetFilesFromDir(String dir) => Directory.EnumerateDirectories(dir);

        public static Int32 GetSubDirCount(String dir) => GetFilesFromDir(dir).Count();

        public static String GetPercentage(Int32 Current, Int32 Maximum) => $"{(Decimal)Current / Maximum:P}";

        public enum PercentageType { Started, Converting, Finished, NoNeed };

        public static String PercentageString(PercentageType type, Int32 Current, Int32 Maximum)
        {
            String output = "";
            switch (type)
            {
                case PercentageType.Started:
                    output = $"Started conversion.";
                    break;
                case PercentageType.Converting:
                    output = $"Converting chapter {Current.ToString("D3")}: {GetPercentage(Current, Maximum)}";
                    break;
                case PercentageType.Finished:
                    output = $"All chapters {GetPercentage(Maximum, Maximum)} converted.";
                    break;
                case PercentageType.NoNeed:
                    output = $"No chapters need converting.";
                    break;
            }

            return output;
        }

        public static String process(String FileName, String Arguments)
        {
            Process p = new Process();
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = FileName;
            p.StartInfo.Arguments = Arguments;
            p.Start();

            String output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output;
        }
    }
}
