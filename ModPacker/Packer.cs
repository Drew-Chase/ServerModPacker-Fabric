using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace ModPacker
{
    public class Packer
    {
        public string WorkingDirectory { get; }
        public string MinecraftVersion { get; }
        public List<string> mods { get; }
        private string TempDirectory { get; }
        private int Ram { get; }

        public Packer(string directory, int ram, string minecraft_version)
        {
            WorkingDirectory = directory;
            MinecraftVersion = minecraft_version;
            Ram = ram;
            mods = new();
            TempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Modpacker", DateTime.Now.Ticks.ToString())).FullName;
            Extract();
        }

        private void Extract()
        {
            Program.log.Warn("Extracting");
            string[] jars = Directory.GetFiles(WorkingDirectory, "*.jar", SearchOption.TopDirectoryOnly);
            string dir = Directory.CreateDirectory(Path.Combine(TempDirectory, "source")).FullName;
            for (int i = 0; i < jars.Length; i++)
            {
                string work = Path.Combine(dir, new FileInfo(jars[i]).Name + ".json");
                using ZipArchive archive = ZipFile.OpenRead(jars[i]);
                var zip = archive.Entries.FirstOrDefault(z => z.Name.Equals("fabric.mod.json"));
                if (zip != null)
                {
                    zip.ExtractToFile(work, true);
                    GetServerSideMods(work, jars[i]);
                }
                else
                {
                    Program.log.Error($"No fabric.mod.json was found");
                }
            }
            CreateModsDirectory();
            InstallServer(DownloadServer());
            GenerateZip();
        }

        private void GetServerSideMods(string jsonPath, string jar_file)
        {
            JObject json = JObject.Parse(File.ReadAllText(jsonPath));
            if (json["environment"] == null || !json["environment"].ToString().Equals("client"))
            {
                Program.log.Info($"{json["id"]} is a Server mod");
                mods.Add(jar_file);
            }
            else
            {
                Program.log.Warn($"{json["id"]} is a Client Mod this will not be added");
            }
        }

        private void CreateModsDirectory()
        {
            Program.log.Warn("Creating Mods Directory");
            string tmp = Directory.CreateDirectory(Path.Combine(TempDirectory, "server", "mods")).FullName;
            foreach (string file in mods)
            {
                File.Copy(file, Path.Combine(tmp, new FileInfo(file).Name), true);
            }
        }

        private string DownloadServer()
        {
            Program.log.Warn("Downloading Server File");
            WebClient client = new();
            XElement xml = XElement.Parse(client.DownloadString($"https://maven.fabricmc.net/net/fabricmc/fabric-installer/maven-metadata.xml"));
            string version = xml.Elements().Where(e => e.Name.LocalName.Equals("versioning")).ElementAt(0).Elements().Where(e => e.Name.LocalName.Equals("release")).ElementAt(0).Value;
            string installer = Path.Combine(Path.GetTempPath(), "fabric-installer.jar");
            client.DownloadFile($"https://maven.fabricmc.net/net/fabricmc/fabric-installer/{version}/fabric-installer-{version}.jar", installer);
            return installer;
        }

        private void InstallServer(string installer)
        {
            Program.log.Warn("Installing Server");
            Process.Start(new ProcessStartInfo()
            {
                FileName = "java",
                Arguments = $"-jar {installer} server -dir \"{Directory.CreateDirectory(Path.Combine(TempDirectory, "server")).FullName}\" -mcversion {MinecraftVersion} -downloadMinecraft",
            }).WaitForExit();
        }

        private void GenerateZip()
        {
            Program.log.Warn("Packing Server");
            string zip = Path.Combine(Directory.GetParent(TempDirectory).FullName, $"{DateTime.Now.Ticks}.zip");
            if (File.Exists(zip))
                File.Delete(zip);
            if (Ram > 0)
            {
                string start = $"java -Xmx{Ram}G -Xms128M -jar fabric-server-launch.jar nogui";
                var bat = File.CreateText(Path.Combine(TempDirectory, "server", "start-win.bat"));
                bat.WriteLine("echo off");
                bat.WriteLine("title Minecraft Modpack Server");
                bat.WriteLine("cls");
                bat.WriteLine("echo Starting Minecraft Modpack Server...");
                bat.WriteLine(start);
                bat.WriteLine("pause");
                bat.Close();

                bat = File.CreateText(Path.Combine(TempDirectory, "server", "start-unix.sh"));
                bat.WriteLine("#!/bin/bash");
                bat.Write(start);
                bat.Close();
            }

            ZipFile.CreateFromDirectory(Path.Combine(TempDirectory, "server"), zip, CompressionLevel.NoCompression, false);
            Program.log.Warn("Cleaning Temp Directory");
            Directory.Delete(TempDirectory, true);
            Program.log.Info("Opening Zip");
            Process.Start(new ProcessStartInfo()
            {
                FileName = zip,
                UseShellExecute = true,
            });
        }
    }
}