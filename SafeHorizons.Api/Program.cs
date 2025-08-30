using SafeHorizons.Api;
using SafeHorizons.Api.BackgroundServices;
using SafeHorizons.Api.Services;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var botToken = builder.Configuration["BotToken"]!;
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("BotSettings"));
builder.Services.AddScoped<AIAkellaService>();
builder.Services.AddScoped<TelegramBotService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFileCleanupService, FileCleanupService>();
builder.Services.AddHostedService<FileCleanupBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.MapControllers();

app.Run();
