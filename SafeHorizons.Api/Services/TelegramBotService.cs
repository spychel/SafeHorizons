using Microsoft.Extensions.Options;
using SafeHorizons.Api.Dto;
using System.Text.Json;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SafeHorizons.Api.Services;
public class TelegramBotService
{
    private readonly SvgDiagramGenerator _svgDiagramGenerator = new();
    private readonly DrawIoXmlGenerator _drawIoXmlGenerator = new();
    private readonly string PBOTOS_DOMAIN = "c70bc22c-289a-4853-af52-4eef9fb951ee";
    private readonly ITelegramBotClient _botClient;
    private readonly AIAkellaService _aIAkellaService;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly BotSettings _botSettings;

    public TelegramBotService(
        ITelegramBotClient botClient,
        AIAkellaService aIAkellaService,
        ILogger<TelegramBotService> logger,
        IOptions<BotSettings> botSettings)
    {
        _botClient = botClient;
        _aIAkellaService = aIAkellaService;
        _logger = logger;
        _botSettings = botSettings.Value;

        ValidateSettings();
    }

    /// <summary>
    /// Обработка запроса пользователя
    /// </summary>
    public async Task ProcessUserRequestAsync(Update update)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        var userText = message.Text;

        try
        {
            var akellaAccessToken = _botSettings.AkellaAccessToken;
            var akellaUserHash = _botSettings.AkellaUserHash;
            var prompt = _botSettings.Prompt;

            await _botClient.SendTextMessageAsync(
                chatId,
                "▶ Спросил у Акеллы. Жду ответа...",
                replyToMessageId: message.MessageId);

            var pbotosResponse = await _aIAkellaService.GetResponse(
                prompt: prompt + userText,
                accessToken: akellaAccessToken,
                userHash: akellaUserHash,
                domainUuid: PBOTOS_DOMAIN,
                isClearMarkdown: true
            );

            await _botClient.SendTextMessageAsync(
                chatId,
                "▶ Ответ получен. Обрабатываю...",
                replyToMessageId: message.MessageId);

            var responseMessage = pbotosResponse.Result?.Message;

            if (responseMessage != null)
            {
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                var algorithmData = JsonSerializer.Deserialize<AlgorithmData>(responseMessage, options);

                if (algorithmData?.Steps?.Any() != true)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Не удалось обработать ваш запрос.",
                        replyToMessageId: message.MessageId);
                }
                else
                {
                    await ProcessResponseAsync(chatId, algorithmData.Steps.ToArray(), algorithmData!.Caption, message.MessageId);
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Не удалось обработать ваш запрос.",
                    replyToMessageId: message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {ChatId}", chatId);
            await _botClient.SendTextMessageAsync(
                chatId,
                "❌ Произошла ошибка при обработке вашего запроса.",
                replyToMessageId: message.MessageId);
        }

    }

    /// <summary>
    /// Обработка ответа AI
    /// </summary>
    public async Task ProcessResponseAsync(long chatId, string[]? steps, string caption, int requestMessageId)
    {
        if (IsInvalidSteps(steps))
        {
            await SendErrorMessageAsync(chatId, "Ошибка генерации изображения", requestMessageId);
            return;
        }

        try
        {
            var (svgDocument, drawioDocument) = GenerateDiagrams(steps!, caption);
            var drawioStream = OperatingFileService.ConvertXmlToStream(drawioDocument);
            var imageStream = OperatingFileService.ConvertSvgToPngStream(svgDocument);

            await SendPhotoAsync(chatId, imageStream, caption, requestMessageId);

            var fileName = await SaveFileAsync(drawioStream);

            await SendDocumentAsync(chatId, fileName, drawioStream, caption, requestMessageId);

            await SendEditLinkIfAvailableAsync(chatId, fileName);
        }
        catch (Exception ex)
        {
            await SendErrorMessageAsync(chatId, $"Ошибка генерации изображения: {ex.Message}", requestMessageId);
        }
    }

    private static bool IsInvalidSteps(string[]? steps)
    {
        return steps is null || steps.Length == 0;
    }

    private (XDocument svgDocument, XDocument drawioDocument) GenerateDiagrams(string[] steps, string caption)
    {
        var svgDoc = _svgDiagramGenerator.GenerateLinearDiagram(steps, caption);
        var drawioDoc = _drawIoXmlGenerator.GenerateLinearDiagram(steps, caption);

        return (svgDoc, drawioDoc);
    }

    private async Task SendPhotoAsync(long chatId, Stream imageStream, string caption, int requestMessageId)
    {
        await using (imageStream)
        {
            await _botClient.SendPhotoAsync(
                chatId: chatId,
                photo: new InputFileStream(imageStream, "algorithm.png"),
                caption: caption,
                replyToMessageId: requestMessageId
            );
        }
    }

    private async Task SendDocumentAsync(long chatId, string fileName, Stream drawioStream, string caption, int requestMessageId)
    {
        drawioStream.Position = 0;
        await using var sendStream = new MemoryStream();
        await drawioStream.CopyToAsync(sendStream);
        sendStream.Position = 0;

        await _botClient.SendDocumentAsync(
            chatId: chatId,
            document: new InputFileStream(sendStream, fileName),
            caption: caption,
            replyToMessageId: requestMessageId
        );
    }

    private static async Task<string> SaveFileAsync(Stream drawioStream)
    {
        var fileName = $"{Guid.NewGuid()}.drawio";
        var filePath = Path.Combine("wwwroot", fileName);

        await OperatingFileService.SaveFileAsync(drawioStream, filePath);

        return fileName;
    }

    private async Task SendEditLinkIfAvailableAsync(long chatId, string fileName)
    {
        var externalHostUrl = _botSettings.ExternalUrl;

        if (!string.IsNullOrEmpty(externalHostUrl))
        {
            string fileUrl = $"https://app.diagrams.net/?url={externalHostUrl}/api/files/get/{fileName}";

            await _botClient.SendTextMessageAsync(chatId, $"Редактировать файл: {fileUrl}");
        }
    }

    private async Task SendErrorMessageAsync(long chatId, string message, int requestMessageId)
    {
        await _botClient.SendTextMessageAsync(chatId, message, replyToMessageId: requestMessageId);
    }

    private void ValidateSettings()
    {
        var missingSettings = new List<string>();

        if (string.IsNullOrEmpty(_botSettings.AkellaAccessToken))
            missingSettings.Add(nameof(_botSettings.AkellaAccessToken));

        if (string.IsNullOrEmpty(_botSettings.AkellaUserHash))
            missingSettings.Add(nameof(_botSettings.AkellaUserHash));

        if (string.IsNullOrEmpty(_botSettings.Prompt))
            missingSettings.Add(nameof(_botSettings.Prompt));

        if (missingSettings.Any())
        {
            throw new ArgumentException(
                $"Отсутствуют обязательные настройки: {string.Join(", ", missingSettings)}. " +
                "Проверьте appsettings.json файл."
            );
        }
    }

}

