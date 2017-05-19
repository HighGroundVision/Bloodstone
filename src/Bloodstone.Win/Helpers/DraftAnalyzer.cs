using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.AD.AutoDrafter
{
    public class DraftAnalyzer
    {
        public IDictionary<int, Bitmap> ExtractUltimates(Bitmap template, Bitmap draft)
        {
            var bounds = ExtractBounds(template).ToList();

            var groupedBounds = bounds
                .GroupBy(_ => _.Location.Y, new FuzzyLocationComparer())
                .Where(_ => _.Count() > 1)
                .ToList();

            var ultimateBounds = groupedBounds
                .Where(_ => _.Count() == 12)
                .OrderBy(_ => _.Key)
                .Last()
                .OrderBy(_ => _.X)
                .ToList();

            var icons = ExtractImages(draft, ultimateBounds).ToList();

            var abilities = icons.AsParallel().ToDictionary(icon => MatchIcon(icon), _ => _);
            return abilities;
        }

        private IEnumerable<Rectangle> ExtractBounds(Bitmap source)
        {
            var grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
            var thresholdFilter = new Threshold(40);
            var invertFilter = new Invert();

            var bitmap = grayscaleFilter.Apply(source);
            thresholdFilter.ApplyInPlace(bitmap);
            invertFilter.ApplyInPlace(bitmap);

            //Create a instance of blob counter algorithm
            var blobCounter = new BlobCounter();
            //Configure Filter
            blobCounter.MinWidth = 25;
            blobCounter.MinHeight = 25;
            blobCounter.MaxWidth = 200;
            blobCounter.MaxHeight = 200;
            blobCounter.FilterBlobs = true;

            blobCounter.ProcessImage(bitmap);
            var _blobPoints = blobCounter.GetObjectsInformation();

            var shapeChecker = new SimpleShapeChecker();

            var min = new IntPoint();
            var max = new IntPoint();

            for (int i = 0; i < _blobPoints.Length; i++)
            {
                List<IntPoint> _edgePoint = blobCounter.GetBlobsEdgePoints(_blobPoints[i]);
                List<IntPoint> _corners = null;

                if (shapeChecker.IsQuadrilateral(_edgePoint, out _corners))
                {
                    PointsCloud.GetBoundingRectangle(_corners, out min, out max);
                    var rect = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
                    yield return rect;
                }
            }

        }

        private IEnumerable<Bitmap> ExtractImages(Bitmap image, IEnumerable<Rectangle> bounds)
        {
            foreach (var rect in bounds)
            {
                var filter = new Crop(rect);
                yield return filter.Apply(image);
            }
        }

        public Dictionary<int, Bitmap> LoadIcons(Size resizeTo)
        {
            var icons = new Dictionary<int, Bitmap>();
            var filter = new ResizeBilinear(resizeTo.Width, resizeTo.Height);

            var files = Directory.GetFiles("Icons");
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var id = int.Parse(info.Name.Replace(info.Extension, ""));

                var orginal = (Bitmap)System.Drawing.Image.FromFile(file);
                var bitmap = orginal.Clone(new Rectangle(0, 0, orginal.Width, orginal.Height), PixelFormat.Format24bppRgb);

                icons.Add(id, filter.Apply(bitmap));
            }

            return icons;
        }

        private int MatchIcon(Bitmap icon)
        {
            var abilities = LoadIcons(icon.Size);
            var threshold = 1.0f;

            var collection = new List<int>();
            do
            {
                collection = abilities.Where(_ => ImageContains(icon, _.Value, threshold) == true).Select(_ => _.Key).ToList();
                threshold -= 0.05f;
            }
            while (collection.Count == 0 && threshold > 0.7f);

            return collection.FirstOrDefault();
        }

        public bool ImageContains(Bitmap template, Bitmap bmp, float threshold = 1.0f)
        {
            var etm = new ExhaustiveTemplateMatching(threshold);

            var tm = etm.ProcessImage(template, bmp);
            if (tm.Length == 0)
                return false;
            else
                return true;
        }
    }

    public class FuzzyLocationComparer : IEqualityComparer<int>
    {
        public bool Equals(int lhs, int rhs)
        {
            if (lhs == rhs)
                return true;
            else if ((lhs + 1) == rhs)
                return true;
            else if ((lhs + 2) == rhs)
                return true;
            else if ((lhs - 1) == rhs)
                return true;
            else if ((lhs - 2) == rhs)
                return true;

            return false;
        }

        public int GetHashCode(int obj)
        {
            return 0;
        }
    }
}
