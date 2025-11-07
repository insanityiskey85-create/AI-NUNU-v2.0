using System.Text.Json.Serialization;

namespace AICompanionPlugin.Models
{
    public class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "nunu-super-AI:12b";

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = "";

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("options")]
        public OllamaOptions Options { get; set; } = new();
    }

    public class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonPropertyName("top_p")]
        public float TopP { get; set; } = 0.9f;

        [JsonPropertyName("stop")]
        public string[]? Stop { get; set; }
    }

    public class OllamaResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("response")]
        public string Response { get; set; } = "";

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("context")]
        public long[] Context { get; set; } = [];

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }
}
