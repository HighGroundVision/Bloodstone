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
using AForge.Imaging;
using AForge.Math.Geometry;
using AForge;
using System.IO;

namespace CompareImages
{
    
    class Program
    {
        static void Main(string[] args)
        {
            //try
            //{
            //    GamestateIntegration.CopyGSIFile();
            //}
            //catch(Exception ex)
            //{
            //    Console.Error.WriteLine("Failed to set CFG: "  + ex.Message);
            //}

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
                Console.ReadKey();

                gsl.Stop();
            }
        }

        static void OnNewGameState(GameState gs)
        {
            try
            {
                if (gs.Previously.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_WAIT_FOR_PLAYERS_TO_LOAD && gs.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_HERO_SELECTION)
                {
                    var imageDirectory = string.Format("images/{0}/", gs.Map.MatchID);
                    Directory.CreateDirectory(imageDirectory);

                    Console.WriteLine("");
                    Console.WriteLine("Started Game, waiting for animation to finish.");

                    // Wait for ability draft screen to load.
                    Thread.Sleep(3000); // Yes it takes 3 seconds to finish the load animation.

                    Console.WriteLine("Animation finished. Capturing Dota2 screen.");

                    // Take screen shot
                    var template = ScreenCapture.CaptureApplication("dota2");
                    template.Save(imageDirectory + "capture.png", ImageFormat.Png);

                    Console.WriteLine("Captured screen. Extracting images for Ultimate's");

                    // Extract ultimate images
                    var limit = template.Height - (template.Height * 0.3);
                    var bounds = ImageLoader.ExtractUltimatesBounds(template)
                        .Where(_ => _.Y > limit)
                        .OrderBy(_ => _.X)
                        .ToList();

                    var icons = ImageLoader.ExtractUltimates(template, bounds).ToList();
                    for (int i = 0; i < icons.Count; i++) icons[i].Save(imageDirectory + string.Format("icon{0}.png", (i + 1)), ImageFormat.Png);

                    Console.WriteLine("Extracted Ultimate's. Matching extractions to Ultimate's on file.");

                    // Process icons to find match
                    var abilities = icons.AsParallel().Select(icon => MatchIcon(icon)).ToList();

                    // if full draft found then lunch web site
                    var keys = abilities.Distinct().ToList();
                    if (keys.Count == 12)
                    {
                        Console.WriteLine("Draft found. Opening drafting page");

                        var key = string.Join(",", keys);
                        var url = string.Format("http://www.abilitydrafter.com/Draft?key={0}", key);
                        Process.Start(url);
                    }
                    else
                    {
                        Console.WriteLine("Failed to draft. Check debug images.");
                    }
                }
            }
            catch(Exception)
            {
                // Next Time Gadget. NEXT TIME!
            }

        }

        private static int MatchIcon(Bitmap icon)
        {
            var abilities = ImageLoader.LoadIcons("icons", icon.Size);
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

        

    }

}
