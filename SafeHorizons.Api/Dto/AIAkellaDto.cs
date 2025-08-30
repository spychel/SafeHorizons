namespace SafeHorizons.Api.Dto;

/// <summary>
/// Корневой объект ответа API AI Akella
/// </summary>
public class AIAkellaApiResponse
{
    /// <summary>
    /// Основной результат выполнения запроса
    /// </summary>
    public AIAkellaApiResult? Result { get; set; }
}

/// <summary>
/// Результат API-запроса к AI Akella
/// </summary>
public class AIAkellaApiResult
{
    /// <summary>
    /// Текстовое сообщение от AI-ассистента
    /// </summary>
    public string? Message { get; set; }
}
