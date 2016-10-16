using AForge.Imaging;
using AForge.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareImages
{
    public static class BitmapExtensions
    {
        public static bool Contains(this Bitmap template, Bitmap bmp, float threshold = 1.0f)
        {
            var etm = new ExhaustiveTemplateMatching(threshold);

            var tm = etm.ProcessImage(template, bmp);
            if (tm.Length == 0)
                return false;
            else
                return true;
        }
    }
}
