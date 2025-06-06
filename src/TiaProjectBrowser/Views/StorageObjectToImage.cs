using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ImageMagick;
using Siemens.Simatic.Hmi.Utah.Globalization;
using System;
using System.IO;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.ExtensionMethods;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class StorageObjectToImage
    {
        public static Control ConvertToImage(StorageBusinessObject sb)
        {
            try
            {
                const int imgSize = 32;

                if (sb?.GetChild<HmiInternalImageAttributes>() != null)
                {
                    var imgDataAttr = sb.GetChild<HmiInternalImageAttributes>();
                    var imgData = imgDataAttr.Thumbnail?.Data;
                    if (imgData == null || imgData?.Length == 0)
                    {
                        imgData = imgDataAttr.GenuineContent.Data;
                    }
                    var format = imgDataAttr?.FileExtension;

                    if (imgDataAttr.FileExtension == ".svg")
                    {
                        return new Avalonia.Svg.Skia.Svg((Uri)null) { Source = imgDataAttr.GenuineContent.DataAsString.RemoveBOM(), Height = imgSize };
                    }
                    else if (imgDataAttr.FileExtension == ".wmf" || imgDataAttr.FileExtension == ".emf")
                    {
                        using var ms = new MemoryStream();
                        using var image = new MagickImage(imgData.Value.ToArray());
                        image.Write(ms, MagickFormat.Svg);
                        ms.Position = 0;
                        using var sr = new StreamReader(ms);
                        var text = sr.ReadToEnd();
                        return new Avalonia.Svg.Skia.Svg((Uri)null) { Source = text, Height = imgSize };
                    }
                    else
                    {
                        var img = new Image() { MaxWidth = 50, Height = imgSize };
                        using var ms = new MemoryStream(imgData.Value.ToArray());
                        img.Source = new Bitmap(ms);
                        return img;
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
