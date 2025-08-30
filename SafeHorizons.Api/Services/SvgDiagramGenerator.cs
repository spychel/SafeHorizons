using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SafeHorizons.Api.Services
{
    public class SvgDiagramGenerator
    {
        private const int BlockWidth = 760;
        private const int BlockX = 40;
        private const int Spacing = 32;
        private const int PaddingTop = 30;
        private const int PaddingBottom = 30;
        private const int PaddingLeft = 30;
        private const int PaddingRight = 15;

        private const int VerticalSpacing = 32;

        private const int LineHeightHeader = 22;
        private const int LineHeightText = 18;

        private const int FontSizeHeader = 14;
        private const int FontSizeText = 12;

        public XDocument GenerateLinearDiagram(string[] steps, string caption)
        {
            int width = 840;
            int currentY = 60;

            XNamespace ns = "http://www.w3.org/2000/svg";
            var svg = new XElement(ns + "svg",
                new XAttribute("xmlns", ns),
                new XAttribute("width", width)
                //new XAttribute("viewBox", $"0 0 {width} 1200") // приблизительно
            );

            var totalHeight = 0;

            // Заголовок диаграммы
            svg.Add(new XElement(ns + "text",
                new XAttribute("x", width / 2),
                new XAttribute("y", 30),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("font-family", "Tahoma"),
                new XAttribute("font-size", "16"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("fill", "#363634"),
                caption
            ));

            svg.Add(new XElement(ns + "defs",
                new XElement(ns + "marker",
                    new XAttribute("id", "arrowhead"),
                    new XAttribute("markerWidth", "10"),
                    new XAttribute("markerHeight", "7"),
                    new XAttribute("refX", "5"),
                    new XAttribute("refY", "3.5"),
                    new XAttribute("orient", "auto"),
                    new XElement(ns + "polygon",
                        new XAttribute("points", "0 0, 10 3.5, 0 7"),
                        new XAttribute("fill", "#545451")
                    )
                )
            ));

            var centers = new List<(int cx, int cy)>();

            foreach (var step in steps)
            {
                // Разделяем шаг на заголовок (если ** есть) и описание
                string header = null;
                string text = step;

                var headerMatch = Regex.Match(step, @"^\*\*(.+?)\*\*");
                if (headerMatch.Success)
                {
                    header = headerMatch.Groups[1].Value;
                    text = step.Substring(headerMatch.Length).TrimStart();
                }

                // Рассчитываем высоту блока
                int boxHeight = PaddingTop;
                var headerLines = header != null
                    ? WrapText(header, "Tahoma", FontSizeHeader, BlockWidth)
                    : new List<string>();

                var textLines = WrapText(text, "Tahoma", FontSizeText, BlockWidth - PaddingRight);

                boxHeight += headerLines.Count * LineHeightHeader + textLines.Count * LineHeightText + PaddingTop;

                int centerX = BlockX + BlockWidth / 2;
                int centerY = currentY + boxHeight;

                // Прямоугольник
                svg.Add(new XElement(ns + "rect",
                    new XAttribute("x", BlockX),
                    new XAttribute("y", currentY),
                    new XAttribute("width", BlockWidth),
                    new XAttribute("height", boxHeight),
                    new XAttribute("rx", 8),
                    new XAttribute("fill", "#C2E7EF"),
                    new XAttribute("stroke", "#CEE2E9"),
                    new XAttribute("stroke-width", "1")
                ));

                // Текст
                var textElement = new XElement(ns + "text",
                    new XAttribute("x", centerX),
                    new XAttribute("y", currentY + PaddingTop),
                    new XAttribute("text-anchor", "middle"),
                    new XAttribute("font-family", "Tahoma"),
                    new XAttribute("fill", "#363634")
                );

                int dy = 0;
                // Заголовок жирный
                foreach (var line in headerLines)
                {
                    textElement.Add(new XElement(ns + "tspan",
                        new XAttribute("x", centerX),
                        new XAttribute("dy", dy == 0 ? "0" : LineHeightHeader),
                        new XAttribute("font-size", FontSizeHeader),
                        new XAttribute("font-weight", "bold"),
                        line
                    ));
                    dy += LineHeightHeader;
                }

                // Описание
                int leftX = BlockX + PaddingLeft;
                int firstLineIndent = 20;   // красная строка
                bool firstLine = true;

                foreach (var line in textLines)
                {
                    textElement.Add(new XElement(ns + "tspan",
                        new XAttribute("x", leftX),
                        new XAttribute("dy", firstLine ? LineHeightText * 2 : dy == 0 ? "0" : LineHeightText),
                        firstLine ? new XAttribute("dx", firstLineIndent) : null,
                        new XAttribute("font-size", FontSizeText),
                        new XAttribute("text-anchor", "start"),
                        new XAttribute("font-family", "Tahoma"),
                        new XAttribute("font-weight", "normal"),
                        line
                    ));
                    dy += LineHeightText;
                    firstLine = false;
                }

                svg.Add(textElement);
                centers.Add((centerX, currentY + boxHeight));
                currentY += boxHeight + Spacing;
            }

            // Стрелки
            for (int i = 0; i < centers.Count - 1; i++)
            {
                int nextBoxHeight = CalculateHeightFromText(steps[i + 1]);
                svg.Add(new XElement(ns + "line",
                    new XAttribute("x1", centers[i].cx),
                    new XAttribute("y1", centers[i].cy),
                    new XAttribute("x2", centers[i + 1].cx),
                    new XAttribute("y2", centers[i + 1].cy - nextBoxHeight - 11),
                    new XAttribute("stroke", "#545451"),
                    new XAttribute("stroke-width", "2"),
                    new XAttribute("marker-end", "url(#arrowhead)")
                ));
            }

            for (int i = 0; i < steps.Count(); i++)
            {
                totalHeight += CalculateHeightFromText(steps[i]);
                if (i < steps.Count() - 1)
                    totalHeight += VerticalSpacing;
            }
            totalHeight += PaddingTop + PaddingBottom;

            svg.SetAttributeValue("viewBox", $"0 0 {width} {totalHeight}");

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), svg);
        }

        public static List<string> WrapText(string text, string fontFamily, float fontSize, float maxWidth)
        {
            var lines = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            maxWidth = (float)(maxWidth * 1.28); 

            string currentLine = "";
            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;

                // измеряем ширину этой строки
                float lineWidth = MeasureTextWidth(testLine, fontFamily, fontSize);

                if (lineWidth > maxWidth)
                {
                    if (!string.IsNullOrWhiteSpace(currentLine))
                        lines.Add(currentLine.Trim());

                    currentLine = word; // начинаем новую строку с текущего слова
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentLine))
                lines.Add(currentLine.Trim());

            return lines;
        }

        private static float MeasureTextWidth(string text, string fontFamily, float fontSize)
        {
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            using (var font = new System.Drawing.Font(fontFamily, fontSize))
            {
                var size = g.MeasureString(text, font);
                return size.Width;
            }
        }

        private int CalculateHeightFromText(string text)
        {
            // Проверяем, есть ли жирный заголовок (**текст**)
            var headerMatch = Regex.Match(text, @"^\*\*(.+?)\*\*");

            int headerLines = 0;
            int textLines = 0;

            if (headerMatch.Success)
            {
                headerLines = WrapText(headerMatch.Groups[1].Value, "Tahoma", FontSizeHeader, BlockWidth - 40).Count;

                string description = text.Substring(headerMatch.Length).TrimStart();
                textLines = WrapText(description, "Tahoma", FontSizeText, BlockWidth - 40).Count;
            }
            else
            {
                textLines = WrapText(text, "Tahoma", FontSizeText, BlockWidth - 40).Count;
            }

            return PaddingTop + headerLines * LineHeightHeader + textLines * LineHeightText + PaddingTop;
        }
    }
}
