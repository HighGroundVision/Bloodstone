using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CompareImages
{
    public class GamestateIntegration
    {
        private static List<string> GetSteamLibFolders()
        {
            const string STEAMAPP_FOLDERPATH = "steamapps\\common";

            List<string> folderPaths = new List<string>();

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam");

            if (key != null)
            {
                var steamInstallLocation = key.GetValue("SteamPath") as string;


                folderPaths.Add(System.IO.Path.Combine(steamInstallLocation, STEAMAPP_FOLDERPATH));

                using (var sr = new System.IO.StreamReader(System.IO.Path.Combine(steamInstallLocation, "SteamApps", "libraryfolders.vdf")))
                {
                    bool start = false;
                    while (!sr.EndOfStream)
                    {
                        var r = sr.ReadLine();
                        if (!start && r == "{")
                        {
                            start = true;
                        }
                        else if (r == "}")
                        {
                            break;
                        }
                        else if (start)
                        {
                            var split = r.Split(new string[] { "\t\t" }, StringSplitOptions.RemoveEmptyEntries);
                            int i = 0;
                            if (int.TryParse(split[0].Trim().Trim('"'), out i))
                            {
                                var s = split[1].Trim().Trim('"').Replace("\\\\", "/");
                                folderPaths.Add(System.IO.Path.Combine(s, STEAMAPP_FOLDERPATH));
                            }
                        }
                    }

                }
            }

            return folderPaths;
        }

        public static void CopyGSIFile()
        {
            var steamAppFolders = GetSteamLibFolders();

            const string DOTAGAMEFOLDER = "dota 2 beta";
            const string HGVCFG = "gamestate_integration_hgv.cfg";

            foreach (var path in steamAppFolders)
            {
                var dotaPath = System.IO.Path.Combine(path, DOTAGAMEFOLDER);

                var foundPath = System.IO.Directory.EnumerateDirectories(path).FirstOrDefault(x => x == dotaPath);
                if (foundPath != null)
                {
                    var cfgPath = System.IO.Path.Combine(foundPath, "game\\dota\\cfg\\gamestate_integration");
                    if (!System.IO.Directory.Exists(cfgPath))
                        System.IO.Directory.CreateDirectory(cfgPath);

                    var hgvCFGFilePath = System.IO.Path.Combine(cfgPath, HGVCFG);

                    if (!System.IO.File.Exists(hgvCFGFilePath))
                    {
                        System.IO.File.Copy(HGVCFG, hgvCFGFilePath);
                    }

                    break;
                }
            }
        }
    }
}
