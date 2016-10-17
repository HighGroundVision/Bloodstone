using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dota2GSI;
using Dota2GSI.Nodes;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using Microsoft.Win32;

namespace CompareImages
{
    
    class Program
    {
        static void Main(string[] args)
        {
            SetGameStateCFG();
            using (var gsl = new GameStateListener(4000))
            {
                gsl.NewGameState += new NewGameStateHandler(OnNewGameState);

                if (!gsl.Start())
                {
                    Console.WriteLine("GameStateListener could not start. Try running this program as Administrator.");
                    Console.WriteLine("Exiting.");
                    return;
                }

                Console.WriteLine("Listening for game integration calls...");
                Console.WriteLine("Press any key to close.");
                Console.WriteLine("");
                Console.ReadKey();

                gsl.Stop();
            }
        }

        static void OnNewGameState(GameState gs)
        {
            try
            {
                Console.WriteLine("Ping");

                if (gs.Previously.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_WAIT_FOR_PLAYERS_TO_LOAD && gs.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_HERO_SELECTION)
                {
                    Console.WriteLine("Started Game, waiting for animation to finish.");

                    // Wait for ability draft screen to load.
                    Thread.Sleep(3000); // Yes it takes 3 seconds to finish the load animation.

                    Console.WriteLine("Animation finished. Capturing Dota2 screen.");

                    // Take screen shot
                    var template = ScreenCapture.CaptureApplication("dota2");
                    template.Save("tempalte.png", ImageFormat.Png);

                    Console.WriteLine("Captured screen. Extracting images for Ultimate's");

                    // Extract ultimate images
                    var icons = ImageLoader.ExtractUltimates(template).ToList();
                    for (int i = 0; i < icons.Count; i++) icons[i].Save(string.Format("icon{0}.png", (i + 1)), ImageFormat.Png);

                    Console.WriteLine("Extracted Ultimate's. Matching extractions to Ultimate's on file.");

                    // Process icons to find match
                    var abilities = icons.AsParallel().Select(icon => MatchIcon(icon)).ToList();
                    var keys = abilities.Distinct().ToList();

                    // if full draft found then lunch web site
                    if (keys.Count == 12)
                    {
                        Console.WriteLine("Draft found. Opening drafting page");

                        var key = string.Join(",", keys);
                        var url = string.Format("http://www.abilitydrafter.com/Draft?key={0}", key);
                        Process.Start(url);
                    }
                    else
                    {
                        Process.Start("http://www.abilitydrafter.com/Draft");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                // Try again next time...
            } 
        }

        private static int MatchIcon(Bitmap icon)
        {
            var abilities = ImageLoader.LoadIcons("icons");
            var threshold = 1.0f;

            var collection = new List<int>();
            do
            {
                collection = abilities.Where(_ => icon.Contains(_.Value, threshold) == true).Select(_ => _.Key).ToList();
                threshold -= 0.05f;
            }
            while (collection.Count == 0 && threshold > 0.5f);

            return collection.FirstOrDefault();
        }

        private static List<int> MatchTemplate(Bitmap template)
        {
            var abilities = ImageLoader.LoadIcons("icons");
            var keys = new List<int>();

            foreach (var item in abilities)
            {
                var threshold = 1.0f;

                do
                {
                    if (template.Contains(item.Value, threshold) == true)
                    {
                        keys.Add(item.Key);
                        break;
                    }

                    threshold -= 0.05f;
                }
                while (threshold > 0.5f);
            }

            return keys;
        }

        static List<string> GetSteamLibFolders()
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

        static void SetGameStateCFG()
        {
            var steamAppFolders = GetSteamLibFolders();

            const string DOTAGAMEFOLDER = "dota 2 beta";
            const string HGVCFG = "gamestate_integration_hgv.cfg";

            foreach (var path in steamAppFolders)
            {
                var dotaPath = System.IO.Path.Combine(path, DOTAGAMEFOLDER);

                var foundPath = System.IO.Directory.EnumerateDirectories(path).FirstOrDefault(x => x == dotaPath);
                if(foundPath != null)
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
