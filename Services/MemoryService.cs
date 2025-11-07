using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AICompanionPlugin.Models;

namespace AICompanionPlugin.Services
{
    public class MemoryService : IDisposable
    {
        private readonly string memoryDirectory;
        private readonly Dictionary<string, List<MemoryEntry>> playerMemories;
        private readonly object lockObject = new object();

        public MemoryService(string pluginDirectory)
        {
            memoryDirectory = Path.Combine(pluginDirectory, "memories");
            Directory.CreateDirectory(memoryDirectory);
            playerMemories = [];
            LoadAllMemories();
        }

        public void AddMemory(string playerName, string content, float importance = 0.5f)
        {
            lock (lockObject)
            {
                if (!playerMemories.ContainsKey(playerName))
                {
                    playerMemories[playerName] = [];
                }

                playerMemories[playerName].Add(new MemoryEntry
                {
                    PlayerName = playerName,
                    Content = content,
                    Importance = importance,
                    Timestamp = DateTime.Now
                });

                // Keep only recent memories
                if (playerMemories[playerName].Count > 100)
                {
                    playerMemories[playerName] = playerMemories[playerName]
                        .OrderByDescending(m => m.Timestamp)
                        .Take(100)
                        .ToList();
                }

                SaveMemories(playerName);
            }
        }

        public List<MemoryEntry> GetRelevantMemories(string playerName, int limit = 10)
        {
            lock (lockObject)
            {
                if (!playerMemories.ContainsKey(playerName))
                    return [];

                return playerMemories[playerName]
                    .OrderByDescending(m => m.Importance)
                    .ThenByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToList();
            }
        }

        public string GetMemoriesAsString(string playerName, int limit = 10)
        {
            var memories = GetRelevantMemories(playerName, limit);
            return string.Join("\n", memories.Select(m => $"[{m.Timestamp:HH:mm}] {m.Content}"));
        }

        private void LoadAllMemories()
        {
            lock (lockObject)
            {
                var memoryFiles = Directory.GetFiles(memoryDirectory, "*.json");
                foreach (var file in memoryFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var memories = JsonSerializer.Deserialize<List<MemoryEntry>>(json);
                        if (memories != null && memories.Count > 0)
                        {
                            var playerName = Path.GetFileNameWithoutExtension(file);
                            playerMemories[playerName] = memories;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load memories from {file}: {ex.Message}");
                    }
                }
            }
        }

        private void SaveMemories(string playerName)
        {
            try
            {
                var filePath = Path.Combine(memoryDirectory, $"{playerName}.json");
                var json = JsonSerializer.Serialize(playerMemories[playerName], new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save memories for {playerName}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // Save all memories on dispose
            foreach (var playerName in playerMemories.Keys)
            {
                SaveMemories(playerName);
            }
        }
    }
}
