using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Folder2Pdf
{
    class Program
    {
        static void Main(String[] Args)
        {
            ParseArgs(Args, out String Base, out String MangaName, out String MangaNameOG, out ProgressDisplayType ProgressDisplay, out ProcessPriorityClass SpawnedPriority);
            Int32 ChapterCount = Directory.EnumerateDirectories($"{Base}\\{MangaName}\\Chapters").Count();
            
            List<ConversionTask> Tasks = new List<ConversionTask>();
            for (Int32 i = 1; i <= ChapterCount; i++)
                if (!File.Exists($"{Base}\\{MangaName}\\Converted\\{MangaName} ({MangaNameOG}) {i.ToString("D3")}.pdf"))
                    Tasks.Add(new ConversionTask(Tasks.Count, i.ToString("D3")));

            if (Tasks.Count > 0)
            {
                Console.WriteLine($"{PercentageString(PercentageType.Begin, ChapterCount, ChapterCount)}, {Tasks.Count} to convert.");

                ProgressBar progress = new ProgressBar();
                progress.Dispose(ProgressDisplay == ProgressDisplayType.Text);

                Tasks.ForEach(Task => {
                    StartProcess($"Tools\\ImageMagick\\magick", $"convert -adjoin \"{Base}\\{MangaName}\\Chapters\\{Task.Chapter}\\*\" \"{Base}\\{MangaName}\\Converted\\{MangaName} ({MangaNameOG}) {Task.Chapter}.pdf\"", SpawnedPriority);
                    DoProgress(ProgressDisplay, progress, ChapterCount, Task.Chapter, Tasks.Count, Task.Position);
                });
                progress.Dispose(ProgressDisplay != ProgressDisplayType.Text);
            }

            Console.WriteLine(PercentageString((Tasks.Count < 1 ? PercentageType.None : PercentageType.End), ChapterCount, ChapterCount));
            Console.ReadKey();
        }

        public static void ParseArgs(IEnumerable<String> Args, out String Base, out String MangaName, out String MangaNameOG, out ProgressDisplayType ProgressDisplay, out ProcessPriorityClass SpawnedPriority)
        {
            String GetArgument(IEnumerable<string> args, string option) => args.SkipWhile(i => i.ToLower() != option.ToLower()).Skip(1).Take(1).FirstOrDefault();

            Dictionary<String, String> ParsedArgs = new Dictionary<String, String>
            {
                { "Base", GetArgument(Args, "-Base") },
                { "MangaName", GetArgument(Args, "-MangaName") },
                { "MangaNameOG", GetArgument(Args, "-MangaNameOG") },

                { "ProgressDisplay", GetArgument(Args, "-ProgressDisplay") },

                { "SpawnedPriority", GetArgument(Args, "-SpawnedPriority") }
            };

            if (!ParsedArgs.TryGetValue("Base", out Base)) Base = "C:\\Users\\Twister\\Downloads";
            if (!ParsedArgs.TryGetValue("MangaName", out MangaName)) MangaName = "Noblesse";
            if (!ParsedArgs.TryGetValue("MangaNameOG", out MangaNameOG)) MangaNameOG = "Manhwa";

            ProgressDisplay = ProgressDisplayType.Text;
            if (ParsedArgs.TryGetValue("ProgressDisplay", out String ProgressDisplayIn)) ProgressDisplay = ParseProgressDisplay(ProgressDisplayIn);

            SpawnedPriority = ProcessPriorityClass.Normal;
            if (ParsedArgs.TryGetValue("SpawnedPriority", out String SpawnedPriorityIn)) SpawnedPriority = ParseSpawnedPriority(SpawnedPriorityIn);
        }

        public static ProgressDisplayType ParseProgressDisplay(String ProgressDisplayIn)
        {
            switch (ProgressDisplayIn)
            {
                default:
                case "Text":
                    return ProgressDisplayType.Text;
                case "Total":
                    return ProgressDisplayType.Total;
                case "Partial":
                    return ProgressDisplayType.Partial;
            }
        }

        public static ProcessPriorityClass ParseSpawnedPriority(String SpawnedPriorityIn)
        {
            switch (SpawnedPriorityIn)
            {
                default:
                case "Normal":
                    return ProcessPriorityClass.Normal;
                case "Idle":
                    return ProcessPriorityClass.Idle;
                case "High":
                    return ProcessPriorityClass.High;
                case "RealTime":
                    return ProcessPriorityClass.RealTime;
                case "BelowNormal":
                    return ProcessPriorityClass.BelowNormal;
                case "AboveNormal":
                    return ProcessPriorityClass.AboveNormal;
            }
        }

        public static Double GetPercentage(Int32 Current, Int32 Maximum) => (Double)Current / Maximum;

        public static String PercentageString(PercentageType type, Int32 Current, Int32 Maximum)
        {
            switch (type)
            {
                case PercentageType.Begin:
                    return "Conversion has begun";
                case PercentageType.Total:
                    return $"Chapter {Current.ToString("D3")}: {GetPercentage(Current, Maximum):P}";
                case PercentageType.Partial:
                    return $"Task {Current.ToString("D2")}: {GetPercentage(Current, Maximum):P}";
                case PercentageType.End:
                    return $"Successfully converted all chapters {GetPercentage(Current, Maximum):P}.";
                case PercentageType.None:
                    return "No chapters need converting.";
                default:
                    return "Why the fuck do I need profanity in my code Eli?";
            }
        }

        public static void DoProgress(ProgressDisplayType ProgressDisplay, ProgressBar progress, Int32 ChapterCount, String Chapter, Int32 TaskCount, Int32 TaskPosition)
        {
            switch (ProgressDisplay)
            {
                case ProgressDisplayType.Text:
                    Console.WriteLine($"{PercentageString(PercentageType.Total, Int32.Parse(Chapter), ChapterCount)}, {PercentageString(PercentageType.Partial, TaskPosition, TaskCount)}");
                    break;
                case ProgressDisplayType.Total:
                    progress.Report(GetPercentage(Int32.Parse(Chapter), ChapterCount));
                    break;
                case ProgressDisplayType.Partial:
                    progress.Report(GetPercentage(TaskPosition, TaskCount));
                    break;
            }
        }

        public enum PercentageType { Begin, End, Total, Partial, None };

        public enum ProgressDisplayType { Text, Total, Partial };

        public struct ConversionTask
        {
            public Int32 Position;
            public String Chapter;

            public ConversionTask(Int32 position, String chapter)
            {
                Position = position;
                Chapter = chapter;
            }
        }

        public static String StartProcess(String FileName, String Arguments, ProcessPriorityClass Priority)
        {
            Process p = new Process();
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = FileName;
            p.StartInfo.Arguments = Arguments;
            p.Start();
            p.PriorityClass = Priority;

            String output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output;
        }
    }
}
