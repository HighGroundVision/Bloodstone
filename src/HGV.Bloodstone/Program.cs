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
            var image = (Bitmap)Bitmap.FromFile(@"C:\Users\Jamie Webster\Downloads\Test1.png");

            // create filter
            Crop filter = new Crop(new Rectangle(85, 10, 465, 50));
            // apply the filter
            Bitmap newImage = filter.Apply(image);

            newImage.Save(@"C:\Users\Jamie Webster\Downloads\Output.png");
        }

        /*
        static void Main(string[] args)
        {
            // create template matching algorithm's instance
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.80f);

            var sourceImage = (Bitmap)Bitmap.FromFile(@"C:\Users\Jamie Webster\Downloads\Inputs\Input5.png");

            var height = sourceImage.Height - 5;
            var raito = height / 94.0f;
            var width = (int)(71 * raito);
            var parts = sourceImage.Width / 11;

            // create filter
            var resizeFilter = new ResizeBicubic(width, height);

            var collection = new List<DataPackage>();
            var files = Directory.GetFiles(@"C:\Users\Jamie Webster\Downloads\heroes");
            foreach (var file in files)
            {
                var template = (Bitmap)Bitmap.FromFile(file);

                // apply the filter
                var resizedTemplate = resizeFilter.Apply(template);

                // find all matchings with specified above similarity
                var matchings = tm.ProcessImage(sourceImage, resizedTemplate);
                var match = matchings.OrderByDescending(_ => _.Similarity).FirstOrDefault();
                if(match != null)
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
            var groups = collection.GroupBy(_ => _.Index).OrderBy(_ => _.Key).Skip(1);
            foreach (var group in groups)
            {
                var item = group.OrderByDescending(_ => _.Similarity).FirstOrDefault();
                Console.WriteLine("{0}: {1} - {2}", group.Key, item.Name, item.Similarity);
            }
        }
        */
    }
}
