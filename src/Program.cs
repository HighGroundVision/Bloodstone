using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;

using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Geometry;

namespace HGV.Bloodstone
{
    class Program
    {
        static void Main(string[] args)
        {
            SubtractFrombackground();

            CropIcons();

            ProcessIcons();
        }

        private static void SubtractFrombackground()
        {
            var input1 = (Bitmap) Bitmap.FromFile("../../data/capture-mask.png");
            var input2 = (Bitmap) Bitmap.FromFile("../../data/capture.png");
            
            // create filter
            Subtract filter = new Subtract(input1);

            // apply the filter
            Bitmap resultImage = filter.Apply(input2);

            resultImage.Save("../../data/input.png");
        }

        private static void CropIcons()
        {
            var input = (Bitmap)Bitmap.FromFile("../../data/input.png");

            // lock image
            BitmapData inputData = input.LockBits(ImageLockMode.ReadWrite);

            // step 3 - locating objects
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 25;
            blobCounter.MinWidth = 25;
            blobCounter.MaxHeight = 150;
            blobCounter.MaxWidth = 150;
            blobCounter.ProcessImage(inputData);

            Blob[] blobs = blobCounter.GetObjectsInformation();

            // step 4 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            for (int i = 0; i < blobs.Length; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
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

                        // create filter
                        Crop filter = new Crop(rec);

                        // apply the filter
                        Bitmap croped = filter.Apply(inputData);
                        croped.Save($"../../data/output/icon[{i}].png");
                    }
                }
            }

            input.UnlockBits(inputData);
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
                        break;
                    }

                }
            }
        }
    }
}
