using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ImageMagick;
using System;
using System.Globalization;
using System.IO;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.Wrappers.Images;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class ToImageConverter : IValueConverter
    {
        private static ImagesConverter imagesConverter = new ImagesConverter();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var sb = value as StorageBusinessObject;
            var img = (TiaFileFormat.Wrappers.Images.Image)imagesConverter.Convert(sb, null);
            if (img.ImageType == ImageType.SVG)
            {
                var svg = new Avalonia.Svg.Skia.Svg((Uri)null);
                svg.Source = System.Text.Encoding.UTF8.GetString(img.Data);
                return svg;
            }
            else if (img.ImageType == ImageType.EMF || img.ImageType == ImageType.WMF)
            {
                using var ms = new MemoryStream();
                using var image = new MagickImage(img.Data);
                image.Write(ms, MagickFormat.Svg);
                ms.Position = 0;
                using var sr = new StreamReader(ms);
                var text = sr.ReadToEnd();
                var svg = new Avalonia.Svg.Skia.Svg((Uri)null);
                svg.Source = text;
                return svg;
            }
            else
            {
                var imgCtl = new Avalonia.Controls.Image();
                using var ms = new MemoryStream(img.Data);
                ms.Position = 0;

                imgCtl.Source = new Bitmap(ms);
                return imgCtl;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
