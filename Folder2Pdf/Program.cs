using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Folder2Pdf
{
    class Program
    {
        public static void ParseArgs(IEnumerable<String> Args, out String Base, out String MangaName, out String MangaNameOG, out ProgressDisplayType ProgressDisplay)
        {
            Dictionary<String, String> ParsedArgs = new Dictionary<String, String>
            {
                { "Base", GetArgument(Args, "-Base") },
                { "MangaName", GetArgument(Args, "-MangaName") },
                { "MangaNameOG", GetArgument(Args, "-MangaNameOG") },
                { "ProgressDisplay", GetArgument(Args, "-ProgressDisplay") }
            };

            if (!ParsedArgs.TryGetValue("Base", out Base)) Base = "C:\\Users\\Twister\\Downloads";
            if (!ParsedArgs.TryGetValue("MangaName", out MangaName)) MangaName = "Noblesse";
            if (!ParsedArgs.TryGetValue("MangaNameOG", out MangaNameOG)) MangaNameOG = "Manhwa";

            ParsedArgs.TryGetValue("ProgressDisplay", out String ProgressDisplayIn);
            switch (ProgressDisplayIn)
            {
                default:
                case "Text":
                    ProgressDisplay = ProgressDisplayType.Text;
                    break;
                case "Total":
                    ProgressDisplay = ProgressDisplayType.Total;
                    break;
                case "Partial":
                    ProgressDisplay = ProgressDisplayType.Partial;
                    break;
            }
            ProgressDisplay = ToProgressDisplayType(ProgressDisplayIn);
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => Console.WriteLine("I'm out of here"));

            ParseArgs(args, out String Base, out String MangaName, out String MangaNameOG, out ProgressDisplayType ProgressDisplay);

            GetSubDirCount($"{Base}\\{MangaName}", out Int32 ChapterCount);
            SetNew(ChapterCount - 1, out Int32 ChapterCount_);

            for (Int32 i = 1; i < ChapterCount; i++)
                if (!File.Exists($"{Base}\\{MangaName}\\pdf\\{MangaName} ({MangaNameOG}) {i.ToString("D3")}.pdf"))
                    ToConvert.Add(i);

            if (ToConvert.Count > 0)
            {
                Console.WriteLine($"{PercentageString(PercentageType.Begin, ChapterCount, ChapterCount)}, {ToConvert.Count} to convert.");

                ProgressBar progress = new ProgressBar();
                progress.Dispose(ProgressDisplay == ProgressDisplayType.Text);

                for (Int32 chapter = 0; chapter < ToConvert.Count; chapter++)
                {
                    String Chapter = ToConvert[chapter].ToString("D3");
                    String Images = $"{Base}\\{MangaName}\\{Chapter}\\*";
                    String Pdf = $"{Base}\\{MangaName}\\pdf\\{MangaName} ({MangaNameOG}) {Chapter}.pdf";
                    Boolean exists = File.Exists(Pdf);

                    process($"Tools\\ImageMagick\\magick", $"convert -adjoin \"{Images}\" \"{Pdf}\"");
                    DoProgress(ProgressDisplay, progress, Chapter, ChapterCount_, chapter, ChapterCount);
                };
                progress.Dispose(ProgressDisplay != ProgressDisplayType.Text);
            }

            Console.WriteLine(PercentageString((ToConvert.Count < 1 ? PercentageType.None : PercentageType.End), ChapterCount, ChapterCount));
            Console.ReadKey();
        }

        public static Int32 SetNew(Int32 In, out Int32 Out) => Out = In;

        public static String GetArgument(IEnumerable<string> args, string option) => args.SkipWhile(i => i.ToLower() != option.ToLower()).Skip(1).Take(1).FirstOrDefault();
        public static Dictionary<String, String> ParsedArgs = new Dictionary<string, string>();

        public static List<Int32> ToConvert = new List<Int32>();

        public static IEnumerable<String> GetFilesFromDir(String dir) => Directory.EnumerateDirectories(dir);

        public static Int32 GetSubDirCount(String dir, out Int32 Count) => Count = GetFilesFromDir(dir).Count();

        public static Double GetPercentage(Int32 Current, Int32 Maximum) => (Double)Current / Maximum;

        public enum PercentageType { Begin, End, Total, Partial, None };

        public static String PercentageString(PercentageType type, Int32 Current, Int32 Maximum)
        {
            String output = "";
            switch (type)
            {
                case PercentageType.Begin:
                    output = $"Conversion has begun";
                    break;
                case PercentageType.Total:
                    output = $"Chapter {Current.ToString("D3")}: {GetPercentage(Current, Maximum):P}";
                    break;
                case PercentageType.Partial:
                    output = $"Conversion task {Current.ToString("D3")}: {GetPercentage(Current, Maximum):P}";
                    break;
                case PercentageType.End:
                    output = $"Successfully converted all chapters {GetPercentage(Current, Maximum):P}.";
                    break;
                case PercentageType.None:
                    output = $"No chapters need converting.";
                    break;
            }
            return output;
        }

        public enum ProgressDisplayType { Text, Total, Partial };

        public static ProgressDisplayType ToProgressDisplayType(String progressDisplayIn)
        {
            switch (progressDisplayIn)
            {
                case "Total":
                    return ProgressDisplayType.Total;
                case "Partial":
                    return ProgressDisplayType.Partial;
                case "Text":
                default:
                    return ProgressDisplayType.Text;
            }
        }

        public static void DoProgress(ProgressDisplayType ProgressDisplay, ProgressBar progress, String Chapter, Int32 ChapterCount_, Int32 chapter, Int32 ChapterCount)
        {
            switch (ProgressDisplay)
            {
                case ProgressDisplayType.Text:
                    Console.WriteLine($"{PercentageString(PercentageType.Total, Int32.Parse(Chapter), ChapterCount_)}, {PercentageString(PercentageType.Partial, chapter, ChapterCount)}");
                    break;
                case ProgressDisplayType.Total:
                    progress.Report(GetPercentage(Int32.Parse(Chapter), ChapterCount_));
                    break;
                case ProgressDisplayType.Partial:
                    progress.Report(GetPercentage(chapter, ChapterCount));
                    break;
            }
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
