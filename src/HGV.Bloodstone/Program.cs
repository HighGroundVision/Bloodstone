using AForge;
using AForge.Imaging;
using AForge.Imaging.ColorReduction;
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

namespace HGV.Bloodstone
{
    public struct DataPackage
    {
        public float Similarity { get; set; }
        public Rectangle Bounds { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var image = (Bitmap)Bitmap.FromFile(@"C:\Users\Webstar\Downloads\Capture1.png");

            int h = (int)(image.Height * 0.075);
            int x = (int)(image.Width * 0.110);
            int w = (int)(image.Width * 0.325);

            // create filter
            Crop filter = new Crop(new Rectangle(x, 10, w, h));
            // apply the filter
            Bitmap newImage = filter.Apply(image);

            newImage.Save(@"C:\Users\Webstar\Downloads\Output1.png");

            // create template matching algorithm's instance
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.8f);

            var sourceImage = (Bitmap)Bitmap.FromFile(@"C:\Users\Webstar\Downloads\Output1.png");

            var height = sourceImage.Height - 10;
            var raito = height / 94.0f;
            var width = (int)(71 * raito);
            var parts = sourceImage.Width / 10;

            // create filter
            var resizeFilter = new ResizeBicubic(width, height);

            var collection = new List<DataPackage>();
            var files = Directory.GetFiles(@"C:\Users\Webstar\Downloads\heroes");
            foreach (var file in files)
            {
                var template = (Bitmap)Bitmap.FromFile(file);

                // apply the filter
                var resizedTemplate = resizeFilter.Apply(template);

                // find all matchings with specified above similarity
                var matchings = tm.ProcessImage(sourceImage, resizedTemplate);
                foreach (var match in matchings)
                {
                    var fi = new FileInfo(file);

                    var data = new DataPackage()
                    {
                        Similarity = match.Similarity,
                        Bounds = match.Rectangle,
                        Name = fi.Name,
                        Index = (int)(match.Rectangle.X / parts)
                    };
                    collection.Add(data);
                }
            }

            // Sort
            var groups = collection.GroupBy(_ => _.Index).OrderBy(_ => _.Key);
            foreach (var group in groups)
            {
                Console.WriteLine("{0}", group.Key);

                var items = group.OrderByDescending(_ => _.Similarity).Take(10).ToList();
                foreach (var item in items)
                {
                    Console.WriteLine("{0} - {1}", item.Name, item.Similarity);
                }
 
            }
        }
    }
}
