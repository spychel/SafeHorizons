using SkiaSharp;
using Svg.Skia;
using System.Xml.Linq;

namespace SafeHorizons.Api.Services;

public static class OperatingFileService
{
    public static MemoryStream ConvertXmlToStream(XDocument xmlDocument)
    {
        var stream = new MemoryStream();
        xmlDocument.Save(stream);
        stream.Position = 0;
        return stream;
    }

    public static MemoryStream ConvertSvgToPngStream(XDocument svgDocument)
    {
        using var svgStream = new MemoryStream();
        svgDocument.Save(svgStream);
        svgStream.Position = 0;

        var svg = new SKSvg();
        svg.Load(svgStream);

        var width = (int)svg.Picture.CullRect.Width;
        var height = (int)svg.Picture.CullRect.Height;

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);
        canvas.DrawPicture(svg.Picture);
        canvas.Flush();

        var imageStream = new MemoryStream();
        bitmap.Encode(imageStream, SKEncodedImageFormat.Png, 100);
        imageStream.Position = 0;

        return imageStream;
    }

    public static async Task SaveFileAsync(Stream stream, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        stream.Position = 0;
        await stream.CopyToAsync(fileStream);
    }
}
