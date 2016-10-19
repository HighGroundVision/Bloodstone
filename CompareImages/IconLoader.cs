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

namespace CompareImages
{
    public static class ImageLoader
    {
        
        public static Dictionary<int, Bitmap> LoadIcons(string path, Size resizeTo)
        {
            var icons = new Dictionary<int, Bitmap>();
            var filter = new ResizeBilinear(resizeTo.Width, resizeTo.Height);

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var info = new FileInfo(file);

                var id = int.Parse(info.Name.Replace(info.Extension, ""));
                var bitmap = LoadIcon(file, filter);

                icons.Add(id, bitmap);
            }

            return icons;
        }

        public static Bitmap LoadIcon(string path, ResizeBilinear filter)
        {
            var bitmap = LoadBitmap(path);
            return filter.Apply(bitmap);
        }

        public static Bitmap LoadBitmap(string path)
        {
            var source = System.Drawing.Image.FromFile(path);
            var orginal = new Bitmap(source);
            return orginal.Clone(new Rectangle(0, 0, orginal.Width, orginal.Height), PixelFormat.Format24bppRgb);
        }

        public static IEnumerable<Rectangle> ExtractUltimatesBounds(Bitmap _bitmapSourceImage)
        {
            var _grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
            var _bitmapGreyImage = _grayscaleFilter.Apply(_bitmapSourceImage);

            //create a edge detector instance
            var _differeceEdgeDetectorFilter = new DifferenceEdgeDetector();
            var _bitmapEdgeImage = _differeceEdgeDetectorFilter.Apply(_bitmapGreyImage);

            var _thresholdFilter = new Threshold(35);
            var _bitmapBinaryImage = _thresholdFilter.Apply(_bitmapEdgeImage);

            //Create a instance of blob counter algorithm
            var _blobCounter = new BlobCounter();
            //Configure Filter
            _blobCounter.MinWidth = 50;
            _blobCounter.MinHeight = 50;
            _blobCounter.MaxWidth = 100;
            _blobCounter.MaxHeight = 100;
            _blobCounter.FilterBlobs = true;

            _blobCounter.ProcessImage(_bitmapBinaryImage);
            var _blobPoints = _blobCounter.GetObjectsInformation();

            var _shapeChecker = new SimpleShapeChecker();

            var min = new IntPoint();
            var max = new IntPoint();

            for (int i = 0; i < _blobPoints.Length; i++)
            {
                List<IntPoint> _edgePoint = _blobCounter.GetBlobsEdgePoints(_blobPoints[i]);
                List<IntPoint> _corners = null;

                if (_shapeChecker.IsQuadrilateral(_edgePoint, out _corners))
                {
                    PointsCloud.GetBoundingRectangle(_corners, out min, out max);
                    var rect = new Rectangle(min.X + 8, min.Y + 8, max.X - min.X - 16, max.Y - min.Y - 16);
                    yield return rect;
                }
            }
        }

        public static IEnumerable<Bitmap> ExtractUltimates(Bitmap image, IEnumerable<Rectangle> bounds)
        {
            foreach (var rect in bounds)
            {
                var filter = new Crop(rect);
                yield return filter.Apply(image);
            }
        }

    }
}
