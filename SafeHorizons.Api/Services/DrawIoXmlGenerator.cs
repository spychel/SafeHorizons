using System.Text;
using System.Xml.Linq;

namespace SafeHorizons.Api.Services;

public class DrawIoXmlGenerator
{
    private readonly Random _random = new Random();

    public XDocument GenerateLinearDiagram(string[] steps, string caption)
    {
        // Базовый namespace для mxGraph
        XNamespace mxNamespace = "";

        // Создаем корневой элемент mxfile
        var mxFile = new XElement(mxNamespace + "mxfile",
            new XAttribute("host", "Electron"),
            new XAttribute("agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) draw.io/24.6.1 Chrome/124.0.6367.207 Electron/30.0.6 Safari/537.36"),
            new XAttribute("modified", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
            new XAttribute("etag", GenerateEtag()),
            new XAttribute("version", "24.6.1"),
            new XAttribute("type", "device")
        );

        // Создаем диаграмму
        var diagram = new XElement(mxNamespace + "diagram",
            new XAttribute("name", "Страница — 1"),
            new XAttribute("id", GenerateId())
        );

        // Создаем модель графа
        var graphModel = new XElement(mxNamespace + "mxGraphModel",
            new XAttribute("dx", "1046"),
            new XAttribute("dy", "781"),
            new XAttribute("grid", "0"),
            new XAttribute("gridSize", "10"),
            new XAttribute("guides", "1"),
            new XAttribute("tooltips", "1"),
            new XAttribute("connect", "1"),
            new XAttribute("arrows", "1"),
            new XAttribute("fold", "1"),
            new XAttribute("page", "1"),
            new XAttribute("pageScale", "1"),
            new XAttribute("pageWidth", "827"),
            new XAttribute("pageHeight", "1169"),
            new XAttribute("background", "#FFFFFF"),
            new XAttribute("math", "0"),
            new XAttribute("shadow", "0")
        );

        // Корневой элемент
        var root = new XElement(mxNamespace + "root");

        // Базовые ячейки
        root.Add(new XElement(mxNamespace + "mxCell", new XAttribute("id", "0")));
        root.Add(new XElement(mxNamespace + "mxCell",
            new XAttribute("id", "1"),
            new XAttribute("parent", "0")));

        // Добавляем заголовок
        var captionId = GenerateId();
        var captionCell = new XElement(mxNamespace + "mxCell",
            new XAttribute("id", captionId),
            new XAttribute("value", caption),
            new XAttribute("style", "text;html=1;align=center;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontFamily=Tahoma;fontStyle=1;fontSize=16;fontColor=#363634;imageWidth=24;"),
            new XAttribute("vertex", "1"),
            new XAttribute("parent", "1"),
            new XElement(mxNamespace + "mxGeometry",
                new XAttribute("x", "40"),
                new XAttribute("y", "30"),
                new XAttribute("width", "760"),
                new XAttribute("height", "30"),
                new XAttribute("as", "geometry")
            )
        );
        root.Add(captionCell);

        // Добавляем шаги
        var stepCells = new List<XElement>();
        var stepIds = new List<string>();
        var startY = 85;

        for (int i = 0; i < steps.Length; i++)
        {
            var stepId = GenerateId();
            stepIds.Add(stepId);

            var stepText = ProcessStepText(steps[i]);
            var height = CalculateHeight(steps[i]); // Динамический расчет высоты

            var stepCell = new XElement(mxNamespace + "mxCell",
                new XAttribute("id", stepId),
                new XAttribute("value", stepText),
                new XAttribute("style", "rounded=1;html=1;labelBackgroundColor=none;fillColor=#C2E7EF;strokeColor=#CEE2E9;spacingLeft=20;spacingRight=20;spacingBottom=10;spacingTop=10;whiteSpace=wrap;"),
                new XAttribute("vertex", "1"),
                new XAttribute("parent", "1"),
                new XElement(mxNamespace + "mxGeometry",
                    new XAttribute("x", "40"),
                    new XAttribute("y", startY.ToString()),
                    new XAttribute("width", "760"),
                    new XAttribute("height", height.ToString()),
                    new XAttribute("as", "geometry")
                )
            );

            stepCells.Add(stepCell);
            startY += height + 32; // Высота блока + отступ
        }

        // Добавляем шаги в корень
        foreach (var stepCell in stepCells)
        {
            root.Add(stepCell);
        }

        // Добавляем стрелки между шагами
        for (int i = 0; i < stepIds.Count - 1; i++)
        {
            var edgeId = GenerateId();
            var edge = new XElement(mxNamespace + "mxCell",
                new XAttribute("id", edgeId),
                new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;exitX=0.5;exitY=1;exitDx=0;exitDy=0;entryX=0.5;entryY=0;entryDx=0;entryDy=0;curved=1;strokeColor=#545451;sourcePerimeterSpacing=0;strokeWidth=4;"),
                new XAttribute("edge", "1"),
                new XAttribute("parent", "1"),
                new XAttribute("source", stepIds[i]),
                new XAttribute("target", stepIds[i + 1]),
                new XElement(mxNamespace + "mxGeometry",
                    new XAttribute("relative", "1"),
                    new XAttribute("as", "geometry")
                )
            );
            root.Add(edge);
        }

        // Собираем документ
        graphModel.Add(root);
        diagram.Add(graphModel);
        mxFile.Add(diagram);

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), mxFile);
    }

    private string ProcessStepText(string step)
    {
        // Обрабатываем жирный текст: **текст** -> <b>текст</b>
        var processedText = System.Text.RegularExpressions.Regex.Replace(
            step,
            @"\*\*(.*?)\*\*",
            "<b>$1</b>"
        );

        // Разделяем на строки и обрабатываем
        var lines = processedText.Split(new[] { "&#xA;", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return string.Empty;

        var builder = new StringBuilder();
        builder.Append("<p style=\"line-height: 140%;\">");

        // Первая строка - заголовок
        builder.Append($"<font color=\"#363634\" size=\"1\"><b style=\"font-size: 14px;\">{lines[0]}</b></font>");
        builder.Append("</p>");

        // Остальные строки - описание
        if (lines.Length > 1)
        {
            builder.Append("<p style=\"line-height: 140%;\">");
            builder.Append($"<span style=\"color: rgb(54, 54, 52); text-align: left; background-color: initial;\">");

            for (int i = 1; i < lines.Length; i++)
            {
                if (i > 1) builder.Append(" ");
                builder.Append(lines[i]);
            }

            builder.Append("</span>");
            builder.Append("</p>");
        }

        builder.Append("<p></p>");

        return builder.ToString();
    }

    private int CalculateHeight(string text)
    {
        // Простая эвристика для расчета высоты блока
        var lineCount = text.Split(new[] { "&#xA;", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
        var baseHeight = 80; // Базовая высота
        var additionalHeight = (lineCount - 1) * 20; // Дополнительная высота для многострочного текста

        return Math.Max(baseHeight, baseHeight + additionalHeight);
    }

    private string GenerateId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 16);
    }

    private string GenerateEtag()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        return new string(Enumerable.Repeat(chars, 16)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}