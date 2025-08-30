using SafeHorizons.Api.Dto;
using System.Text.Json;

namespace SafeHorizons.Api.Services;

public class AIAkellaService(IHttpClientFactory httpClientFactory)
{
    private const string ApiUrl = "https://tatneft.guru/api/http";

    /// <summary>
    /// Получает ответ от AI Akella API
    /// </summary>
    /// <param name="prompt">Текст запроса пользователя</param>
    /// <param name="accessToken">Токен доступа (обязательный)</param>
    /// <param name="modelType">Тип модели (по умолчанию: qwen2-5-72b)</param>
    /// <param name="collectionUuid">Идентификатор коллекции</param>
    /// <param name="dialogueUuid">Идентификатор диалога</param>
    /// <param name="isClearMarkdown">Очищать markdown разметку</param>
    /// <returns>Ответ от API</returns>
    public async Task<AIAkellaApiResponse> GetResponse(
        string prompt,
        string accessToken,
        string modelType = "qwen2-5-72b",
        string? collectionUuid = null,
        string? dialogueUuid = null,
        string? domainUuid = null,
        string? userHash = null,
        bool isClearMarkdown = false)
    {
        using var formData = new MultipartFormDataContent();

        // Обязательные параметры
        formData.Add(new StringContent(accessToken), "accesstoken");
        formData.Add(new StringContent(prompt), "text");
        formData.Add(new StringContent(modelType), "lm_model_type");

        // Необязательные параметры
        if (!string.IsNullOrEmpty(collectionUuid))
            formData.Add(new StringContent(collectionUuid), "collection_uuid");

        if (!string.IsNullOrEmpty(dialogueUuid))
            formData.Add(new StringContent(dialogueUuid), "dialogue_uuid");

        if (!string.IsNullOrEmpty(domainUuid))
            formData.Add(new StringContent(domainUuid), "domain_uuid");

        if (!string.IsNullOrEmpty(userHash))
            formData.Add(new StringContent(userHash), "userHash");

        formData.Add(new StringContent(isClearMarkdown.ToString().ToLower()), "isClearMarkdown");

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(360);
            var response = await httpClient.PostAsync(ApiUrl, formData);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<AIAkellaApiResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return apiResponse ?? throw new InvalidOperationException("Не удалось десериализовать ответ API");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Ошибка HTTP запроса: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Ошибка парсинга JSON: {ex.Message}", ex);
        }
    }
}