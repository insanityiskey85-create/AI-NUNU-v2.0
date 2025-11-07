using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AICompanionPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public string ModelName { get; set; } = "nunu-super-AI:12b";
        public string OllamaEndpoint { get; set; } = "http://localhost:11434";
        public bool EnableMemory { get; set; } = true;
        public int MemoryLimit { get; set; } = 1000; // Increased default
        public bool EnableEmotions { get; set; } = true;
        public string PersonaFilePath { get; set; } = "persona.txt";
        public float Temperature { get; set; } = 0.7f;
        public int MaxHistoryMessages { get; set; } = 550; // Increased default

        public void Save()
        {
            AICompanionPlugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
