using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AICompanionPlugin.Models;

namespace AICompanionPlugin.Services
{
    public class OllamaService : IDisposable
    {
        private readonly HttpClient httpClient;
        private const string DEFAULT_ENDPOINT = "http://localhost:11434";

        public OllamaService()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<string> GetAIResponse(string prompt, string modelName = "nunu-super-AI:12b")
        {
            try
            {
                var request = new OllamaRequest
                {
                    Model = modelName,
                    Prompt = prompt,
                    Stream = false,
                    Options = new OllamaOptions
                    {
                        Temperature = AICompanionPlugin.PluginInterface?.GetPluginConfig() is Configuration config ? config.Temperature : 0.7f
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{DEFAULT_ENDPOINT}/api/generate", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseString);
                    return ollamaResponse?.Response ?? "";
                }

                return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<bool> IsOllamaRunning()
        {
            try
            {
                var response = await httpClient.GetAsync($"{DEFAULT_ENDPOINT}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
