using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using QuizGame.Models;

namespace QuizGame.Services;

public class OpenAiQuizService(HttpClient httpClient, IConfiguration configuration)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;

    public async Task<List<Question>> GenerateQuestionsAsync(int count, string topic, string difficulty)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:Model"] ?? "gpt-4.1-mini";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "PUT_YOUR_OPENAI_API_KEY_HERE")
            throw new InvalidOperationException("OpenAI API key is missing.");

        var prompt = $@"
Generate exactly {count} multiple-choice quiz questions.
Topic: {topic}
Difficulty: {difficulty}

Rules:
- Return JSON only
- Do not use markdown
- Do not wrap the response in ```json
- Each question must have 4 options
- Exactly 1 correct answer
- Avoid duplicates

Return JSON:
{{
  ""questions"": [
    {{
      ""text"": ""string"",
      ""options"": [""string"", ""string"", ""string"", ""string""],
      ""correctAnswer"": ""string""
    }}
  ]
}}
";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { model, input = prompt }),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI request failed: {body}");

        using var doc = JsonDocument.Parse(body);
        var outputText = ExtractOutputText(doc.RootElement);
        outputText = CleanJson(outputText);

        JsonDocument quizDoc;
        try
        {
            quizDoc = JsonDocument.Parse(outputText);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                $"OpenAI did not return valid JSON. Raw output:\n{outputText}");
        }

        using (quizDoc)
        {
            var result = new List<Question>();
            var index = 1;

            foreach (var q in quizDoc.RootElement.GetProperty("questions").EnumerateArray())
            {
                var options = q.GetProperty("options")
                    .EnumerateArray()
                    .Select(x => x.GetString() ?? "")
                    .ToList();

                if (options.Count != 4)
                    continue;

                result.Add(new Question
                {
                    OrderIndex = index++,
                    Text = q.GetProperty("text").GetString() ?? string.Empty,
                    OptionA = options[0],
                    OptionB = options[1],
                    OptionC = options[2],
                    OptionD = options[3],
                    CorrectAnswer = q.GetProperty("correctAnswer").GetString() ?? string.Empty
                });
            }

            return result;
        }
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var outputArray))
            throw new InvalidOperationException("OpenAI response did not contain output.");

        foreach (var outputItem in outputArray.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentArray))
                continue;

            foreach (var contentItem in contentArray.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var textElement))
                    return textElement.GetString()
                        ?? throw new InvalidOperationException("OpenAI text output was empty.");
            }
        }

        throw new InvalidOperationException("Could not extract text from OpenAI response.");
    }

    private static string CleanJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("OpenAI returned empty text.");

        text = text.Trim();

        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text[7..].Trim();
        else if (text.StartsWith("```"))
            text = text[3..].Trim();

        if (text.EndsWith("```"))
            text = text[..^3].Trim();

        return text;
    }
}