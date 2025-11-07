using System;
using System.Collections.Generic;

namespace AICompanionPlugin.Models
{
    public class MemoryEntry
    {
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public float Importance { get; set; } = 0.5f;
        public string PlayerName { get; set; } = "";
    }

    public class ConversationEntry
    {
        public string PlayerName { get; set; } = "";
        public string UserMessage { get; set; } = "";
        public string AIMessage { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class EmotionalState
    {
        public Dictionary<string, float> Emotions { get; set; } = new()
        {
            {"happiness", 0.5f},
            {"sadness", 0.1f},
            {"anger", 0.1f},
            {"excitement", 0.3f},
            {"curiosity", 0.4f}
        };
    }
}
