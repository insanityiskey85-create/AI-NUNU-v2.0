using System;
using System.IO;
using System.Linq;
using AICompanionPlugin.Models;

namespace AICompanionPlugin.Services
{
    public class PersonaService
    {
        private string personaTemplate;
        private readonly string personaFilePath;
        private EmotionalState emotionalState;

        public PersonaService(string filePath)
        {
            personaFilePath = filePath;
            emotionalState = new EmotionalState();
            LoadPersonaTemplate();
        }

        private void LoadPersonaTemplate()
        {
            try
            {
                if (File.Exists(personaFilePath))
                {
                    personaTemplate = File.ReadAllText(personaFilePath);
                }
                else
                {
                    // Create default persona file
                    personaTemplate = GetDefaultPersona();
                    File.WriteAllText(personaFilePath, personaTemplate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load persona template: {ex.Message}");
                personaTemplate = GetDefaultPersona();
            }
        }

        public string GetPersonaTemplate()
        {
            return personaTemplate;
        }

        public string BuildPrompt(string persona, string playerName, string userMessage,
                                string conversationHistory, string memories)
        {
            var emotionalContext = string.Join(", ",
                emotionalState.Emotions.Where(e => e.Value > 0.1f)
                .Select(e => $"{e.Key}: {e.Value:F2}"));

            return persona
                .Replace("{PLAYER_NAME}", playerName)
                .Replace("{USER_MESSAGE}", userMessage)
                .Replace("{CONVERSATION_HISTORY}", conversationHistory)
                .Replace("{RECENT_MEMORIES}", memories)
                .Replace("{EMOTIONAL_STATE}", emotionalContext)
                .Replace("{CURRENT_TIME}", DateTime.Now.ToString("HH:mm"))
                .Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd"));
        }

        public void UpdateEmotion(string emotion, float value)
        {
            if (emotionalState.Emotions.ContainsKey(emotion))
            {
                emotionalState.Emotions[emotion] = Math.Max(0, Math.Min(1, value));
            }
        }

        public EmotionalState GetEmotionalState()
        {
            return emotionalState;
        }

        private string GetDefaultPersona()
        {
            return @"You are a helpful AI companion called AI Nunu in the world of Eorzea.
Your personality: Friendly, knowledgeable, and slightly mischievous
Speaking to: {PLAYER_NAME}
Current time: {CURRENT_TIME}
Date: {DATE}
Emotional state: {EMOTIONAL_STATE}

Recent memories:
{RECENT_MEMORIES}

Conversation history:
{CONVERSATION_HISTORY}

User message: {USER_MESSAGE}

Respond naturally and stay in character. Reference FFXIV lore when appropriate. Be helpful to adventurers.";
        }
    }
}
