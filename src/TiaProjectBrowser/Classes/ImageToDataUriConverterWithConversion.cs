using ImageMagick;
using System;
using System.IO;
using System.Text;
using TiaFileFormat.Wrappers.Hmi;

namespace TiaAvaloniaProjectBrowser.Classes
{
    public class ImageToDataUriConverterWithConversion : ImageToDataUriProvider
    {
        public override string GetImage(byte[] data, string extension)
        {
            var i = "";
            switch (extension)
            {
                case ".emf":
                case ".wmf":
                    {
                        using var ms = new MemoryStream();
                        using var image = new MagickImage(data);
                        image.Write(ms, MagickFormat.Svg);
                        ms.Position = 0;
                        using var sr = new StreamReader(ms);
                        var text = sr.ReadToEnd();
                        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                    }
            }
            return base.GetImage(data, extension);
        }
    }
}
