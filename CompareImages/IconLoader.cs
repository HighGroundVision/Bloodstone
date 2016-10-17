using AForge.Imaging.Filters;
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
        public static IEnumerable<Bitmap> ExtractUltimates(Bitmap image)
        {
            // Needs to be based on Rez...!

            //var defaultBounds = new Rectangle(225, 660, 95, 95); // 1600 * 920
            var defaultBounds = new Rectangle(284, 777, 89, 81); // 1920 * 1080

            for (int i = 0; i < 12; i++)
            {
                var left = defaultBounds.Left + (114 * i);
                var filter = new Crop(new Rectangle(left, defaultBounds.Top, defaultBounds.Width, defaultBounds.Height));
                yield return filter.Apply(image);
            }
        }

        public static Dictionary<int, Bitmap> LoadIcons(string path)
        {
            var icons = new Dictionary<int, Bitmap>();

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var info = new FileInfo(file);

                var id = int.Parse(info.Name.Replace(info.Extension, ""));
                icons.Add(id, LoadIcon(file));
            }

            return icons;
        }

        public static Bitmap LoadIcon(string path)
        {
            // Needs to be based on Rez...!

            //var filter = new ResizeBilinear(74, 67); // 1600 * 920
            var filter = new ResizeBilinear(89, 81);  // 1920 * 1080

            return filter.Apply(LoadBitmap(path));
        }

        public static Bitmap LoadBitmap(string path)
        {
            var source = Image.FromFile(path);
            var orginal = new Bitmap(source);
            return orginal.Clone(new Rectangle(0, 0, orginal.Width, orginal.Height), PixelFormat.Format24bppRgb);
        }
    }
}
