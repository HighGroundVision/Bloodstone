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
            var iconBounds = Rectangle.Empty;
            var offset = 0;
            
            if(image.Width == 1920 && image.Height == 1080)
            {
                iconBounds = new Rectangle(284, 777, 89, 81);
                offset = 114;
            }
            else  if (image.Width == 1600 && image.Height == 1050)
            {
                iconBounds = new Rectangle(183, 756, 86, 79);
                offset = 110;
            }

            if (iconBounds.IsEmpty == true)
                throw new ArgumentOutOfRangeException(nameof(iconBounds));
            
            for (int i = 0; i < 12; i++)
            {
                var left = iconBounds.Left + (offset * i);
                var filter = new Crop(new Rectangle(left, iconBounds.Top, iconBounds.Width, iconBounds.Height));
                yield return filter.Apply(image);
            }
        }

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
            var source = Image.FromFile(path);
            var orginal = new Bitmap(source);
            return orginal.Clone(new Rectangle(0, 0, orginal.Width, orginal.Height), PixelFormat.Format24bppRgb);
        }
    }
}
