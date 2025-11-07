using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AICompanionPlugin.Models;

namespace AICompanionPlugin.Services
{
    public class ConversationService
    {
        private readonly Dictionary<string, List<ConversationEntry>> conversations;
        private readonly int maxHistoryLength = 1000; // Increased limit for "infinite" conversations

        public ConversationService()
        {
            conversations = new Dictionary<string, List<ConversationEntry>>();
        }

        public void AddToConversation(string playerName, string userMessage, string aiMessage)
        {
            if (!conversations.ContainsKey(playerName))
            {
                conversations[playerName] = new List<ConversationEntry>();
            }

            conversations[playerName].Add(new ConversationEntry
            {
                PlayerName = playerName,
                UserMessage = userMessage,
                AIMessage = aiMessage,
                Timestamp = DateTime.Now
            });

            // Much higher limit for "infinite" conversation history
            if (conversations[playerName].Count > maxHistoryLength)
            {
                // Remove oldest entries but keep a reasonable amount
                var toRemove = conversations[playerName].Count - maxHistoryLength;
                conversations[playerName].RemoveRange(0, toRemove);
            }
        }

        public string GetConversationHistory(string playerName, int messageCount = 100) // Increased default
        {
            if (!conversations.ContainsKey(playerName))
                return "";

            var recentConversations = conversations[playerName]
                .OrderByDescending(c => c.Timestamp)
                .Take(messageCount)
                .OrderBy(c => c.Timestamp);

            var sb = new StringBuilder();
            foreach (var conv in recentConversations)
            {
                if (!string.IsNullOrEmpty(conv.UserMessage))
                {
                    sb.AppendLine($"[{conv.Timestamp:HH:mm}] {conv.PlayerName}: {conv.UserMessage}");
                }
                if (!string.IsNullOrEmpty(conv.AIMessage))
                {
                    sb.AppendLine($"[{conv.Timestamp:HH:mm}] AI: {conv.AIMessage}");
                }
            }

            return sb.ToString().Trim();
        }

        // Method to get all conversation entries (for "infinite" history)
        public List<ConversationEntry> GetAllConversationEntries(string playerName)
        {
            if (!conversations.ContainsKey(playerName))
                return new List<ConversationEntry>();

            return conversations[playerName]
                .OrderBy(c => c.Timestamp)
                .ToList();
        }

        // Method to get conversation entries with custom limit
        public List<ConversationEntry> GetConversationEntries(string playerName, int limit = 1000)
        {
            if (!conversations.ContainsKey(playerName))
                return new List<ConversationEntry>();

            return conversations[playerName]
                .OrderBy(c => c.Timestamp)
                .TakeLast(limit)
                .ToList();
        }

        public void ClearConversation(string playerName)
        {
            if (conversations.ContainsKey(playerName))
            {
                conversations[playerName].Clear();
            }
        }

        // Method to get conversation count for a player
        public int GetConversationCount(string playerName)
        {
            if (!conversations.ContainsKey(playerName))
                return 0;
            return conversations[playerName].Count;
        }
    }
}
