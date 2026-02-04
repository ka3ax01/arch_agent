using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArchAgent.LLM;

public sealed class OllamaClient
{
    private readonly HttpClient _httpClient;

    public OllamaClient(TimeSpan timeout)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = timeout
        };
    }

    public async Task<string> GenerateAsync(string model, string prompt)
    {
        const int maxAttempts = 2;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var payload = new
                {
                    model,
                    prompt,
                    stream = false
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync("/api/generate", content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("response", out var resp) && resp.ValueKind == JsonValueKind.String)
                {
                    return resp.GetString() ?? string.Empty;
                }

                throw new InvalidOperationException("Ollama response missing 'response' field.");
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Ollama request failed after retries.");
    }
}
