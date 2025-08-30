using Microsoft.AspNetCore.Mvc;
using SafeHorizons.Api.Services;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SafeHorizons.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramWebhookController(
    ITelegramBotClient botClient,
    TelegramBotService telegramBotService) :
    ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;

            await botClient.SendTextMessageAsync(
                chatId,
                "Запрос получен, начинаю обработку, ожидайте...");

            _ = telegramBotService.ProcessUserRequestAsync(update);

            return Ok();
        }

        return Ok();
    }
}