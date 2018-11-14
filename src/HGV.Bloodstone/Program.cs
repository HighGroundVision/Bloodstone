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
    public struct TemplatePackage
    {
        public string Key { get; set; }
        public int Hero { get; set; }
        public Bitmap Image { get; set; }
    }

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
            Listen();
        }

        private static void Listen()
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
                bool DEBUGING = true;

                if (gs.Previously.Map.GameState == GS_WAITING && gs.Map.GameState == GS_DRAFTING)
                {
                    var client = new HGV.Basilius.MetaClient();
                    var heroKeys = client.GetADHeroes().ToDictionary(_ => _.Key, _ => _.Id);

                    Console.WriteLine("Drafting");
                    Console.WriteLine("Hero");
                    Console.WriteLine("{0}:{1}", gs.Hero.ID, gs.Hero.Name);

                    Thread.Sleep(4000); // Wait for ability draft screen to load. 

                    var captureImage = ScreenCapture.CaptureApplication();

                    if(DEBUGING == true)
                    {
                        captureImage.Save(".//output//capture.png");
                    }
                        
                    Bitmap overlay;
                    Bitmap source;
                    {


                        // create filter
                        var colorFiltering = new ColorFiltering();
                        // set channels' ranges to keep
                        colorFiltering.Red = new IntRange(50, 52);
                        colorFiltering.Green = new IntRange(50, 52);
                        colorFiltering.Blue = new IntRange(50, 52);
                        // apply the filter
                        var image = colorFiltering.Apply(captureImage);

                        var gfilter = Grayscale.CommonAlgorithms.BT709;
                        source = gfilter.Apply(image);
                    }

                    {
                        // create filter
                        var colorFiltering = new ColorFiltering();
                        // set channels' ranges to keep
                        colorFiltering.Red = new IntRange(194, 204);
                        colorFiltering.Green = new IntRange(211, 221);
                        colorFiltering.Blue = new IntRange(165, 170);
                        // apply the filter
                        var image = colorFiltering.Apply(captureImage);

                        var gfilter = Grayscale.CommonAlgorithms.BT709;
                        overlay = gfilter.Apply(image);
                    }

                    // create filter
                    var mergeFilter = new Merge(overlay);
                    var resultImage = mergeFilter.Apply(source);

                    var shapeChecker = new SimpleShapeChecker();

                    var bc = new BlobCounter();
                    bc.FilterBlobs = true;
                    bc.MinHeight = 20;
                    bc.MinWidth = 20;
                    bc.MaxHeight = 100;
                    bc.MaxWidth = 100;
                    bc.ProcessImage(resultImage);

                    var blobs = bc.GetObjectsInformation();

                    var size = new Size();

                    var left = 0;
                    var top = 0;
                    var right = 0;
                    var height = 0;

                    foreach (var blob in blobs)
                    {
                        var points = bc.GetBlobsEdgePoints(blob);
                        if (shapeChecker.IsCircle(points))
                        {
                            right = blob.Rectangle.Left;
                        }
                        else if (shapeChecker.IsQuadrilateral(points))
                        {
                            left = blob.Rectangle.Right;
                            top = blob.Rectangle.Top;
                            height = blob.Rectangle.Height;

                            const double HERO_RATIO = 0.76;
                            size.Width = (int)Math.Round(height * HERO_RATIO);
                            size.Height = height;
                        }
                    }

                    var bounds = new Rectangle(left, top, right - left, height);
                    var spacing = (bounds.Width - (size.Width * 10)) / 11;
                    bounds.X += spacing;
                    bounds.Width -= (spacing * 2);

                    if (DEBUGING == true)
                    {
                        Crop filter = new Crop(bounds);
                        var imageHeader = filter.Apply(captureImage);
                        imageHeader.Save("./output/header.png");
                    }

                    var collection = new List<Bitmap>();
                    for (int i = 0; i < 10; i++)
                    {
                        var x = (spacing * i) + (size.Width * i) + bounds.X;

                        Crop cropFilter = new Crop(new Rectangle(x, bounds.Y, size.Width, size.Height));
                        var cropedImage = cropFilter.Apply(captureImage);
                        collection.Add(cropedImage);

                        if (DEBUGING == true)
                        {
                            cropedImage.Save($"./output/hero[{i}].png");
                        }
                    }

                    var templates = new List<TemplatePackage>();
                    var resizeFilter = new ResizeBicubic(size.Width, size.Height);
                    var files = Directory.GetFiles(".//heroes");
                    foreach (var file in files)
                    {
                        var key = Path.GetFileNameWithoutExtension(file);
                        var heroId = 0;
                        heroKeys.TryGetValue(key, out heroId);

                        var template = Bitmap.FromFile(file) as Bitmap;
                        var resizedTemplate = resizeFilter.Apply(template);
                        var package = new TemplatePackage()
                        {
                            Key = key,
                            Hero = heroId,
                            Image = resizedTemplate,
                        };
                        templates.Add(package);
                    }

                    var tm = new ExhaustiveTemplateMatching(0.8f);
                    var data = new List<DataPackage>();
                    for (int i = 0; i < collection.Count; i++)
                    {
                        var image = collection[i];

                        foreach (var template in templates)
                        {
                            var matchings = tm.ProcessImage(image, template.Image);
                            foreach (var match in matchings)
                            {
                                var package = new DataPackage()
                                {
                                    Similarity = match.Similarity,
                                    Bounds = match.Rectangle,
                                    Key = template.Key,
                                    Hero = template.Hero,
                                    Index = i,
                                };
                                data.Add(package);
                            }
                        }
                    }

                    var heroes = new List<int>();
                    var groups = data.GroupBy(_ => _.Index).OrderBy(_ => _.Key).ToList();
                    foreach (var group in groups)
                    {
                        var item = group.OrderByDescending(_ => _.Similarity).Take(1).FirstOrDefault();

                        heroes.Add(item.Hero);

                        if (DEBUGING == true)
                        {
                            Console.WriteLine("{0}:{1} ({2})", item.Hero, item.Key, item.Similarity);
                        }
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

        static void Draw()
        {
            /*
            // lock image to draw on it
            Bitmap captureImage = Bitmap.FromFile("./output/capture.png") as Bitmap;
            var data = captureImage.LockBits(new Rectangle(0, 0, captureImage.Width, captureImage.Height), ImageLockMode.ReadWrite, captureImage.PixelFormat);

            // process each blob
            foreach (Blob blob in blobs)
            {
                // var edge = bc.GetBlobsEdgePoints(blob);
                // List<IntPoint> hull = hullFinder.FindHull(edge);
                // Drawing.Polygon(data, hull, Color.HotPink);

                Drawing.Rectangle(data, blob.Rectangle, Color.HotPink);
            }

            captureImage.UnlockBits(data);

            captureImage.Save("./output/test-bounds.png");
            */
        }
    }
}
