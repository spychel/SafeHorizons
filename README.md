# 📋 SafeHorizons.Api - Документация проекта

## 🚀 Обзор проекта

**SafeHorizons.Api** - ASP.NET Core Web API приложение для генерации диаграмм алгоритмов с интеграцией Telegram Bot.

## 📁 Структура проекта

```
SafeHorizons.Api/
├── 📁 BackgroundServices/    # Фоновые сервисы
├── 📁 Controllers/           # API контроллеры
├── 📁 Dto/                   # Модели данных
├── 📁 Services/              # Бизнес-логика
├── 📁 wwwroot/               # Статические файлы
├── 📄 Program.cs             # Конфигурация приложения
├── 📄 appsettings.json       # Конфигурационные настройки
└── 📄 SafeHorizons.Api.csproj
```

## ⚙️ Конфигурация

### Основные настройки в `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "BotToken": "your_telegram_bot_token",
  "BotSettings": {
    "AccessToken": "akella_access_token",
    "ExternalUrl": "https://your-domain.com",
    "UserHash": "akella_user_hash",
    "Prompt": "system_prompt_text"
  },
  "FileCleanup": {
    "CleanupIntervalMinutes": 15,
    "FileMaxAgeMinutes": 30
  },
  "AllowedHosts": "*"
}
```

## 🎯 Основные функции

### 1. 🤖 Telegram Bot Integration
- Прием сообщений от пользователей
- Генерация диаграмм алгоритмов
- Отправка изображений и документов
- Ссылки для редактирования диаграмм

### 2. 📊 Генерация диаграмм
- **SVG генерация** - для preview изображений
- **Draw.io XML генерация** - для редактирования
- **Конвертация SVG → PNG** - для Telegram

### 3. 🗃️ Управление файлами
- Автоматическое сохранение в `wwwroot`
- API для доступа к файлам
- Фоновая очистка старых файлов

## 🚀 Запуск проекта

### 1. Установка зависимостей
```bash
dotnet restore
```

### 2. Настройка окружения
```bash
# Development
cp appsettings.Development.example.json appsettings.Development.json
# Edit with your actual values
```

### 3. Запуск
```bash
dotnet run
# или
dotnet watch run
```

## 📋 Требования к окружению

### Необходимые переменные окружения:
- `BotToken` - Токен Telegram бота
- `BotSettings:AccessToken` - Токен Akella API
- `BotSettings:UserHash` - Хэш пользователя Akella
- `BotSettings:ExternalUrl` - Внешний URL приложения, необходим для Телеграм бота
- `BotSettings:Prompt` - Промпт для AI позволяющий обрабатывать вхоодящие сообщения в нужно формате

### Опциональные:
- `FileCleanup:CleanupIntervalMinutes` - Интервал очистки (по умолчанию: 15)
- `FileCleanup:FileMaxAgeMinutes` - Время жизни файлов (по умолчанию: 30)

## 🔌 API Endpoints

### 📄 Files Controller
- `GET /api/files/get/{fileName}` - Получить файл
- `GET /api/files/list` - Список файлов
- `DELETE /api/files/delete/{fileName}` - Удалить файл

### 🧹 Cleanup Controller
- `GET /api/cleanup/status` - Статус очистки
- `POST /api/cleanup/run` - Запустить очистку

### 🔍 Debug Controller
- `GET /api/debug/check-file/{fileName}` - Проверить файл
- `GET /api/debug/files` - Список файлов (debug)

## 🛠️ Технологии

- **ASP.NET Core 8.0** - Web API framework
- **Telegram.Bot** - Telegram Bot API
- **SkiaSharp** - SVG rendering and PNG conversion
- **IHostedService** - Background tasks
- **Dependency Injection** - Service management

## 📊 Логирование

Уровни логирования:
- `Information` - Старт/остановка сервисов
- `Warning` - Предупреждения и пропущенные файлы
- `Error` - Ошибки обработки
- `Debug` - Детальная отладочная информация

## 🔒 Безопасность

### Валидация настроек:
```csharp
// Автоматическая проверка обязательных настроек
private void ValidateSettings()
{
    if (string.IsNullOrEmpty(_botSettings.BotToken))
        throw new ArgumentException("BotToken is required");
    // ... другие проверки
}
```

### Очистка файлов:
- Проверка расширений (только `.drawio`)
- Защита от path traversal атак
- Регулярная автоматическая очистка

## 🐛 Отладка

### Локальная разработка:
```json
{
  "BotSettings": {
    "ExternalUrl": "https://localhost:7027"
  },
  "FileCleanup": {
    "CleanupIntervalMinutes": 5,
    "FileMaxAgeMinutes": 10
  }
}
```