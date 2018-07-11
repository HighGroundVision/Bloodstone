using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Threading;
using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Geometry;

namespace HGV.Bloodstone
{
    class Program
    {
        static void Main(string[] args) => Test3();

        private static void Test3()
        {
            var directoryTemplates = @"C:\Users\Jamie Webster\source\repos\HGV\Hyperstone\heroes\profile";
            var directoryHeroes = @"C:\Users\Jamie Webster\source\repos\HGV\Bloodstone\src\data\Test3\";

            var filter = new ResizeBilinear(45, 64);
            var matcher = new ExhaustiveTemplateMatching(0f);

            var filesTemplate = System.IO.Directory.GetFiles(directoryTemplates);
            var templates = new Dictionary<string, Bitmap>();
            foreach (var f in filesTemplate)
            {
                var fi = new System.IO.FileInfo(f);

                try
                {
                    var image = (Bitmap)Bitmap.FromFile(f);
                    var resized = filter.Apply(image);

                    templates.Add(fi.Name, resized);
                }
                catch (Exception) { }
            }

            var filesHero = System.IO.Directory.GetFiles(directoryHeroes);
            foreach (var f in filesHero)
            {
                var heroImage = (Bitmap)Bitmap.FromFile(f);

                var matches = new List<Tuple<string, float>>();

                foreach (var pair in templates)
                {
                    try
                    {
                        var score = matcher.ProcessImage(heroImage, pair.Value).Select(_ => _.Similarity).FirstOrDefault();
                        matches.Add(Tuple.Create(pair.Key, score));
                    }
                    catch (Exception) { }
                }

                var match = matches.OrderBy(_ => _.Item2).Select(_ => _.Item1).FirstOrDefault();
                Console.WriteLine($"Match: {match}");
            }

            Console.WriteLine("Done");
        }

        private static void Test2()
        {
            var input = (Bitmap)Bitmap.FromFile(@"C:\Users\Jamie Webster\source\repos\HGV\Bloodstone\src\data\Test2\Input.png");

            Crop filterCrop = new Crop(new Rectangle(0, 0, input.Width, 100));
            var image = filterCrop.Apply(input);

            var stat = new ImageStatistics(image);
            var medianBGColor = Color.FromArgb(stat.Red.Median, stat.Green.Median, stat.Blue.Median);
            var bgColor = Color.FromArgb(30, 40, 50); //var bgColor = Color.FromArgb(48, 68, 68);

            // create filter
            PointedColorFloodFill filterFloodFill = new PointedColorFloodFill();
            // configure the filter
            filterFloodFill.Tolerance = bgColor;
            filterFloodFill.FillColor = Color.FromArgb(0, 0, 0);
            filterFloodFill.StartingPoint = new IntPoint(1, 1);
            // apply the filter
            filterFloodFill.ApplyInPlace(image);

            // lock image
            BitmapData colorData = image.LockBits(ImageLockMode.ReadWrite);

            // step 3 - locating objects
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.BackgroundThreshold = medianBGColor;
            blobCounter.MinHeight = 50;
            blobCounter.MinWidth = 10;
            blobCounter.MaxHeight = 500;
            blobCounter.MaxWidth = 200;
            blobCounter.ProcessImage(colorData);

            Blob[] blobs = blobCounter.GetObjectsInformation();

            // step 4 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            var collection = new List<Rectangle>();

            for (int i = 0; i < blobs.Length; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);

                IntPoint lt, br;
                PointsCloud.GetBoundingRectangle(edgePoints, out lt, out br);
                var bounds = new Rectangle(lt.X, lt.Y, br.X - lt.X, br.Y - lt.Y);

                collection.Add(bounds);
            }

            var heroes = collection.OrderBy(_ => _.X).Skip(1).Take(10).ToList();
            int top = (int)Math.Floor(heroes.Average(_ => _.Top));
            int bottom = (int)Math.Floor(heroes.Average(_ => _.Bottom));
            int width = (int)Math.Floor(heroes.Average(_ => _.Width));
            int height = bottom - top;
            int offset = 5;

            foreach (var item in heroes)
            {
                var index = heroes.IndexOf(item) + 1;

                var bounds = new Rectangle(item.X, item.Y, item.Width, item.Height);
                if (bounds.Top != top)
                    bounds.Location = new System.Drawing.Point(item.X + offset, top);

                if (bounds.Bottom != bottom)
                    bounds.Size = new Size(width - offset, height);

                Drawing.Rectangle(colorData, bounds, Color.HotPink);

                Crop filterExtract = new Crop(bounds);
                var heroImage = filterExtract.Apply(input);

                heroImage.Save(@"C:\Users\Jamie Webster\source\repos\HGV\Bloodstone\src\data\Test2\Hero[" + index + "].png");
            }

            //image.UnlockBits(colorData);
            //image.Save(@"C:\Users\Jamie Webster\source\repos\HGV\Bloodstone\src\data\Test2\Output.png");
        }

        private static void Test1()
        {
            var input = (Bitmap)Bitmap.FromFile(@"C:\Users\Webstar\Desktop\Test\Input.png");
            
            Crop filterCrop = new Crop(new Rectangle(0, 0, input.Width, 100));
            var image = filterCrop.Apply(input);

            var stat = new ImageStatistics(image);
            var medianBGColor = Color.FromArgb(stat.Red.Median, stat.Green.Median, stat.Blue.Median);
            var bgColor = Color.FromArgb(30, 40, 50); //var bgColor = Color.FromArgb(48, 68, 68);

            // create filter
            PointedColorFloodFill filterFloodFill = new PointedColorFloodFill();
            // configure the filter
            filterFloodFill.Tolerance = bgColor;
            filterFloodFill.FillColor = Color.FromArgb(0, 0, 0);
            filterFloodFill.StartingPoint = new IntPoint(1,1);
            // apply the filter
            filterFloodFill.ApplyInPlace(image);

            //image.Save(@"C:\Users\Webstar\Desktop\Test\Test1.png");

            // lock image
            BitmapData colorData = image.LockBits(ImageLockMode.ReadWrite);

            // step 3 - locating objects
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.BackgroundThreshold = medianBGColor;
            blobCounter.MinHeight = 50;
            blobCounter.MinWidth = 10;
            blobCounter.MaxHeight = 500;
            blobCounter.MaxWidth = 200;
            blobCounter.ProcessImage(colorData);

            Blob[] blobs = blobCounter.GetObjectsInformation();

            // step 4 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            var collection = new List<Rectangle>();

            for (int i = 0; i < blobs.Length; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);

                IntPoint lt, br;
                PointsCloud.GetBoundingRectangle(edgePoints, out lt, out br);
                var bounds = new Rectangle(lt.X, lt.Y, br.X - lt.X, br.Y - lt.Y);

                collection.Add(bounds);
            }
          
            var heroes = collection.OrderBy(_ => _.X).Skip(1).Take(10).ToList();
            foreach (var item in heroes)
            {
                var index = heroes.IndexOf(item) + 1;

                var bounds = new Rectangle(item.Center().X - 25, item.Center().Y - 25, 50, 50);

                Drawing.Rectangle(colorData, bounds, Color.HotPink);

                Crop filterExtract = new Crop(bounds);
                var heroImg = filterExtract.Apply(input);
                heroImg.Save(@"C:\Users\Webstar\Desktop\Test\Hero[" + index + "].png");
            }

            image.UnlockBits(colorData);
            image.Save(@"C:\Users\Webstar\Desktop\Test\Output.png");

            // 10 vs 120
        }

        private static void SplitInput()
        {
            var input = (Bitmap)Bitmap.FromFile("../../data/input.jpg");

            // define quadrilateral's corners
            List<IntPoint> corners = new List<IntPoint>();
            corners.Add(new IntPoint(719,339));     // top/left
            corners.Add(new IntPoint(1200,339));    // top/right
            corners.Add(new IntPoint(1252,837));    // bottom/right
            corners.Add(new IntPoint(667,837));     // bottom/left
            // create filter
            var filter =new SimpleQuadrilateralTransformation(corners, 650, 750);
            // apply the filter
            Bitmap newImage = filter.Apply(input);
            newImage.Save("../../data/output[1].jpg");
        }

        private static void ProcessFeatures()
        {
            var input = (Bitmap)Bitmap.FromFile("../../data/input.jpg");

            // Create a new SURF with the default parameter values:
            var surf = new SpeededUpRobustFeaturesDetector(threshold: 0.0002f, octaves: 5, initial: 2);

            // Use it to extract the SURF point descriptors from the Lena image:
            var descriptors = surf.Transform(input);

            // We can obtain the actual double[] descriptors using

            var features = descriptors.Select(_ => _.Descriptor);

            // Now those descriptors can be used to represent the image itself, such
            // as for example, in the Bag-of-Visual-Words approach for classification.
        }

        private static void ProcessDelta()
        {
            var captures = System.IO.Directory.GetFiles("../../data/captures/");
            for (int i = 1; i < captures.Count(); i++)
            {
                var filelhs = captures[i-1];
                var filerhs = captures[i];

                var inputA = (Bitmap)Bitmap.FromFile(filelhs);
                var inputB = (Bitmap)Bitmap.FromFile(filerhs);

                // create filter
                Subtract filter = new Subtract(inputA);

                // apply the filter
                Bitmap delta = filter.Apply(inputB);

                delta.Save($"../../data/deltas/delta[{i:00000}].png");

                inputA.Dispose();
                inputB.Dispose();
                delta.Dispose();
            }
        }

        private static void PrintScreen()
        {
            Bitmap printscreen = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(printscreen as System.Drawing.Image);
            var count = 1;

            while(true)
            {
                var input = Console.ReadKey();
                if (input.Key == ConsoleKey.Spacebar)
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
                    printscreen.Save($"../../data/captures/capture[{count++:00000}].jpg", ImageFormat.Jpeg);
                }
                else if (input.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }   
        }

        private static void SubtractFrombackground()
        {
            var input1 = (Bitmap) Bitmap.FromFile("../../data/capture-mask.png");
            var input2 = (Bitmap) Bitmap.FromFile("../../data/capture.png");
            
            // create filter
            Subtract filter = new Subtract(input1);

            // apply the filter
            Bitmap output = filter.Apply(input2);

            output.Save("../../data/input.png");

            input1.Dispose();
            input1.Dispose();
            output.Dispose();
        }

        private static void CropIcons()
        {
            var input = (Bitmap)Bitmap.FromFile("../../data/input.png");

            // lock image
            BitmapData inputData = input.LockBits(ImageLockMode.ReadWrite);

            // step 3 - locating objects
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 10;
            blobCounter.MinWidth = 10;
            blobCounter.MaxHeight = 150;
            blobCounter.MaxWidth = 150;
            blobCounter.ProcessImage(inputData);

            Blob[] blobs = blobCounter.GetObjectsInformation();

            // step 4 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            for (int i = 0; i < blobs.Length; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                Drawing.Polyline(inputData, edgePoints, Color.White);

                List<IntPoint> corners;
                if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                {
                    if (corners.Count == 4)
                    {
                        int xMin = corners.Min(s => s.X);
                        int yMin = corners.Min(s => s.Y);
                        int xMax = corners.Max(s => s.X);
                        int yMax = corners.Max(s => s.Y);
                        var rec = new Rectangle(xMin, yMin, xMax - xMin, yMax - yMin);

                        Drawing.Rectangle(inputData, rec, Color.HotPink);
                        //Drawing.Polyline(inputData, corners, Color.HotPink);

                        // create filter
                        //Crop filter = new Crop(rec);

                        // apply the filter
                        //Bitmap croped = filter.Apply(inputData);
                        //croped.Save($"../../data/output/icon[{i}].png");
                    }
                }
            }

            input.UnlockBits(inputData);
            input.Save("../../data/output.png");
            input.Dispose();
        }

        private static void ProcessIcons()
        {
            var templates = System.IO.Directory.GetFiles("../../data/source/");
            foreach (var t in templates)
            {
                var fi = new System.IO.FileInfo(t);
                var templateName = fi.Name;

                var icons = System.IO.Directory.GetFiles("../../data/output/");
                foreach (var icon in icons)
                {
                    // Get input
                    var input = (Bitmap)Bitmap.FromFile(icon);

                    // Get template scalled correctly to match
                    var template = (Bitmap)Bitmap.FromFile(t);
                    template = template.Clone(new Rectangle(0, 0, template.Width, template.Height), PixelFormat.Format24bppRgb);

                    ResizeBilinear resizefilter = new ResizeBilinear(input.Width, input.Height);
                    Bitmap resized = resizefilter.Apply(template);

                    // create template matching algorithm's instance
                    // use zero similarity to make sure algorithm will provide anything
                    ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.9f);

                    // compare two images
                    TemplateMatch[] matchings = tm.ProcessImage(input, resized);

                    if (matchings.Count() > 0)
                    {
                        Console.WriteLine($"{templateName} is a match");
                    }

                    input.Dispose();
                    template.Dispose();
                }
            }
        }
    }
}
