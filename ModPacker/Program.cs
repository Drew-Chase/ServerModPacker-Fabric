using ChaseLabs.CLLogger;
using ChaseLabs.CLLogger.Interfaces;
using System;
using System.IO;

namespace ModPacker
{
    internal class Program
    {
        public static ILog log = LogManager.Init().SetMinimumLogType(Lists.LogTypes.All).SetDumpMethod(DumpType.NoBuffer).SetPattern("[%TYPE% - %DATE%]: %MESSAGE%").SetLogDirectory(Path.Combine(Directory.CreateDirectory("logs").FullName, "latest.log"));

        private static void Main(string[] args)
        {
            log.Info($"Welcome to Minecraft Server Packer");
            string path = string.Empty;
            string minecraft_version = string.Empty;
            int ram = 0;
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-p")) path = args[i + 1];
                    if (args[i].ToLower().Equals("-mc")) minecraft_version = args[i + 1];
                    if (args[i].ToLower().Equals("-shell")) ram = int.Parse(args[i + 1]);
                    if (args[i].ToLower().Equals("?"))
                    {
                        PrintHelp();
                        return;
                    }
                }
            }
            catch
            {
                string cmd = "";
                for (int i = 0; i < args.Length; i++)
                {
                    cmd += args[i] + " ";
                }
                log.Error($"\"{cmd}\" is not a recognized command");
                PrintHelp();
                return;
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.Write("PATH: ");
                path = Console.ReadLine();
            }
            if (string.IsNullOrWhiteSpace(minecraft_version))
            {
                Console.Write("MC Version: ");
                minecraft_version = Console.ReadLine();
            }
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(minecraft_version))
            {
                PrintHelp();
                return;
            }

            _ = new Packer(path.Replace("\"", ""), ram, minecraft_version);
        }

        private static void PrintHelp()
        {
            string[] help = {
                        "help - Shows this message.",
                        "-p PATH - To specify the path",
                        "-mc 1.17.1 - To specify minecraft version",
                        "-shell 4 - To Generate a Start Script with Specific RAM",
                        @"Usage #1: packer -p C:\Path\To\Mods\Directory -o pack.zip -mc 1.17.1",
                    };
            foreach (string h in help)
            {
                log.Info(h);
            }
        }
    }
}