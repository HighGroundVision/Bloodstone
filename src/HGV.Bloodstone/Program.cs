using AForge;
using AForge.Imaging;
using AForge.Imaging.ColorReduction;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using Dota2GSI;
using Dota2GSI.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Bloodstone
{
    public struct DataPackage
    {
        public float Similarity { get; set; }
        public Rectangle Bounds { get; set; }
        public string Key { get; set; }
        public int Hero { get; set; }
        public int Index { get; set; }
    }

    class Program
    {
        private const DOTA_GameState GS_WAITING = DOTA_GameState.DOTA_GAMERULES_STATE_WAIT_FOR_PLAYERS_TO_LOAD;
        private const DOTA_GameState GS_DRAFTING = DOTA_GameState.DOTA_GAMERULES_STATE_HERO_SELECTION;

        static void Main(string[] args)
        {
            var gsl = new GameStateListener(4000);
            gsl.NewGameState += new NewGameStateHandler(OnNewGameState);

            var result = gsl.Start();

            Console.WriteLine("Waiting...");
            var key = Console.ReadKey();

            gsl.Stop();
        }

        static void OnNewGameState(GameState gs)
        {
            try
            {
                if (gs.Previously.Map.GameState == GS_WAITING && gs.Map.GameState == GS_DRAFTING)
                {
                    var client = new HGV.Basilius.MetaClient();
                    var mappping = client.GetADHeroes().ToDictionary(_ => _.Key, _ => _.Id);

                    Console.WriteLine("Drafting");
                    Console.WriteLine("Hero");
                    Console.WriteLine("{0}:{1}", gs.Hero.ID, gs.Hero.Name);

                    Thread.Sleep(4000); // Wait for ability draft screen to load. 

                    var captureImage = ScreenCapture.CaptureApplication();

                    captureImage.Save(".//output//capture.png");

                    // create filter
                    int h = (int)(captureImage.Height * 0.075);
                    int x = (int)(captureImage.Width * 0.110);
                    int w = (int)(captureImage.Width * 0.325);
                    Crop filter = new Crop(new Rectangle(x, 10, w, h));
                    // apply the filter
                    Bitmap sourceImage = filter.Apply(captureImage);

                    sourceImage.Save(".//output//header.png");

                    // create template matching algorithm's instance
                    ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.8f);

                    var height = sourceImage.Height - 10;
                    var raito = height / 94.0f;
                    var width = (int)(71 * raito);
                    var parts = sourceImage.Width / 10;

                    // create filter
                    var resizeFilter = new ResizeBicubic(width, height);

                    var collection = new List<DataPackage>();
                    var files = Directory.GetFiles(".//heroes");
                    foreach (var file in files)
                    {
                        var key = Path.GetFileNameWithoutExtension(file);
                        var heroId = 0;
                        mappping.TryGetValue(key, out heroId);

                        var template = (Bitmap)Bitmap.FromFile(file);

                        // apply the filter
                        var resizedTemplate = resizeFilter.Apply(template);

                        // find all matchings with specified above similarity
                        var matchings = tm.ProcessImage(sourceImage, resizedTemplate);
                        foreach (var match in matchings)
                        {
                            var data = new DataPackage()
                            {
                                Similarity = match.Similarity,
                                Bounds = match.Rectangle,
                                Key = key,
                                Hero = heroId,
                                Index = (int)(match.Rectangle.X / parts)
                            };
                            collection.Add(data);
                        }
                    }

                    Console.WriteLine("Pool");

                    // Sort
                    var heroes = new List<int>();
                    var groups = collection.GroupBy(_ => _.Index).OrderBy(_ => _.Key);
                    foreach (var group in groups)
                    {
                        var item = group.OrderByDescending(_ => _.Similarity).Take(1).FirstOrDefault();
                        Console.WriteLine("[{0}] {1}:{2} ({3})", group.Key, item.Hero, item.Key, item.Similarity);
                        heroes.Add(item.Hero);
                    }

                    Console.WriteLine("Launching Drafter");

                    var roster = string.Join(",", heroes.ToArray());
                    Process.Start("https://hgv-desolator.azurewebsites.net/#/draft?roster=" + roster);

                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                // Next Time Gadget. NEXT TIME!
            }
        }
    }
}
