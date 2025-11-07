using Dalamud.Interface.Windowing;
using Dalamud.Interface.Colors;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AICompanionPlugin.Models;

namespace AICompanionPlugin.Windows
{
    public class MainWindow : Window
    {
        private readonly AICompanionPlugin plugin;
        private string testMessage = "";
        private string lastResponse = "";
        private bool isTesting = false;
        private string chatMessage = "";
        private Vector2 chatScrollPosition = Vector2.Zero;
        private bool autoScroll = true;
        private int messagesToShow = 100; // Configurable message limit for performance

        public MainWindow(AICompanionPlugin plugin) : base("AI Nunu")
        {
            this.plugin = plugin;
            this.IsOpen = false;

            var size = new Vector2(700, 600);
            this.Size = size;
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("AICompanionTabs"))
            {
                if (ImGui.BeginTabItem("Chat"))
                {
                    DrawChatTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Configuration"))
                {
                    DrawConfigurationTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Test AI"))
                {
                    DrawTestTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Status"))
                {
                    DrawStatusTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawChatTab()
        {
            var playerName = AICompanionPlugin.ClientState.LocalPlayer?.Name.TextValue ?? "Player";

            // Chat controls
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Total Messages: {plugin.ConversationService.GetConversationCount(playerName)}");
            ImGui.SameLine();
            ImGui.Checkbox("Auto-scroll", ref autoScroll);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            ImGui.SliderInt("Show Last", ref messagesToShow, 10, 500, "%d msgs");

            // Create a child window for the chat area
            var chatSize = new Vector2(0, -ImGui.GetFrameHeightWithSpacing() - 15);
            if (ImGui.BeginChild("ChatArea", chatSize, true))
            {
                // Display chat messages
                DisplayChatMessages(playerName);

                // Auto-scroll to bottom
                if (autoScroll)
                {
                    ImGui.SetScrollHereY(1.0f);
                }
            }
            ImGui.EndChild();

            // Message input area
            ImGui.Separator();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Message:");
            ImGui.SameLine();

            var messageInputWidth = ImGui.GetContentRegionAvail().X - 100;
            ImGui.SetNextItemWidth(messageInputWidth);
            if (ImGui.InputText("##ChatMessage", ref chatMessage, 1000, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                SendMessage(playerName);
            }

            ImGui.SameLine();
            if (ImGui.Button("Send", new Vector2(80, 0)))
            {
                SendMessage(playerName);
            }

            // Clear chat button
            ImGui.SameLine();
            if (ImGui.Button("Clear Chat"))
            {
                plugin.ConversationService.ClearConversation(playerName);
            }
        }

        private void DisplayChatMessages(string playerName)
        {
            // Get conversation entries with configurable limit
            var conversationEntries = plugin.ConversationService.GetConversationEntries(playerName, messagesToShow);

            // Display messages
            foreach (var entry in conversationEntries)
            {
                if (!string.IsNullOrEmpty(entry.UserMessage))
                {
                    ImGui.TextColored(ImGuiColors.TankBlue, $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.PlayerName}: {entry.UserMessage}");
                }
                if (!string.IsNullOrEmpty(entry.AIMessage))
                {
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] AI: {entry.AIMessage}");
                }
            }
        }

        private async void SendMessage(string playerName)
        {
            if (string.IsNullOrWhiteSpace(chatMessage)) return;

            var messageToSend = chatMessage.Trim();
            chatMessage = ""; // Clear input

            try
            {
                // Add user message to conversation immediately for responsiveness
                plugin.ConversationService.AddToConversation(playerName, messageToSend, "");

                // Get conversation context (increased limit for better context)
                var conversationHistory = plugin.ConversationService.GetConversationHistory(playerName, plugin.Configuration.MaxHistoryMessages * 2);
                var memories = plugin.MemoryService.GetMemoriesAsString(playerName);
                var persona = plugin.PersonaService.GetPersonaTemplate();

                // Build prompt with all context
                var fullPrompt = plugin.PersonaService.BuildPrompt(persona, playerName, messageToSend, conversationHistory, memories);

                // Get AI response
                var response = await plugin.OllamaService.GetAIResponse(fullPrompt, plugin.Configuration.ModelName);

                if (!string.IsNullOrEmpty(response))
                {
                    // Save to memory and conversation
                    plugin.MemoryService.AddMemory(playerName, $"User: {messageToSend}");
                    plugin.MemoryService.AddMemory(playerName, $"AI: {response}");
                    // Update the conversation with AI response
                    plugin.ConversationService.AddToConversation(playerName, messageToSend, response);
                }
                else
                {
                    plugin.ConversationService.AddToConversation(playerName, messageToSend, "Error: Failed to get response from AI");
                }
            }
            catch (Exception ex)
            {
                plugin.ConversationService.AddToConversation(playerName, messageToSend, $"Error: {ex.Message}");
            }
        }

        private void DrawConfigurationTab()
        {
            ImGui.Text("AI Companion Configuration");
            ImGui.Spacing();

            // Create temporary variables for input fields
            var modelName = plugin.Configuration.ModelName ?? "";
            var ollamaEndpoint = plugin.Configuration.OllamaEndpoint ?? "";

            if (ImGui.InputText("Model Name", ref modelName, 100))
            {
                plugin.Configuration.ModelName = modelName;
            }

            if (ImGui.InputText("Ollama Endpoint", ref ollamaEndpoint, 200))
            {
                plugin.Configuration.OllamaEndpoint = ollamaEndpoint;
            }

            var temperature = plugin.Configuration.Temperature;
            if (ImGui.SliderFloat("Temperature", ref temperature, 0.0f, 1.0f))
            {
                plugin.Configuration.Temperature = temperature;
            }

            var maxHistoryMessages = plugin.Configuration.MaxHistoryMessages;
            if (ImGui.SliderInt("Context Messages", ref maxHistoryMessages, 5, 200)) // Increased limit
            {
                plugin.Configuration.MaxHistoryMessages = maxHistoryMessages;
            }

            var enableMemory = plugin.Configuration.EnableMemory;
            if (ImGui.Checkbox("Enable Memory System", ref enableMemory))
            {
                plugin.Configuration.EnableMemory = enableMemory;
            }

            if (plugin.Configuration.EnableMemory)
            {
                var memoryLimit = plugin.Configuration.MemoryLimit;
                if (ImGui.SliderInt("Memory Limit", ref memoryLimit, 10, 500)) // Increased limit
                {
                    plugin.Configuration.MemoryLimit = memoryLimit;
                }
            }

            var enableEmotions = plugin.Configuration.EnableEmotions;
            if (ImGui.Checkbox("Enable Emotions", ref enableEmotions))
            {
                plugin.Configuration.EnableEmotions = enableEmotions;
            }

            ImGui.Spacing();
            if (ImGui.Button("Save Configuration"))
            {
                plugin.Configuration.Save();
                ImGui.TextColored(ImGuiColors.HealerGreen, "Configuration saved!");
            }

            ImGui.Spacing();
            if (ImGui.Button("Clear All Conversations"))
            {
                // This would clear all conversations for all players
                // You might want to add a confirmation dialog here
            }
        }

        private void DrawTestTab()
        {
            ImGui.Text("Test your AI Companion");
            ImGui.Spacing();

            ImGui.InputText("Test Message", ref testMessage, 500);

            if (ImGui.Button("Send to AI") && !string.IsNullOrEmpty(testMessage) && !isTesting)
            {
                _ = TestAI();
            }

            if (isTesting)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "Processing...");
            }

            if (!string.IsNullOrEmpty(lastResponse))
            {
                ImGui.Separator();
                ImGui.Text("AI Response:");
                ImGui.TextWrapped(lastResponse);
            }
        }

        private void DrawStatusTab()
        {
            ImGui.Text($"Plugin Version: {AICompanionPlugin.PluginInterface.Manifest.AssemblyVersion}");
            ImGui.Text($"Dalamud API Level: {AICompanionPlugin.PluginInterface.Manifest.DalamudApiLevel}");
            ImGui.Text($"Target Framework: .NET 9.0");

            ImGui.Spacing();
            ImGui.Text("Ollama Status: Checking...");
            _ = CheckOllamaStatus();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Conversation Statistics:");
            var playerName = AICompanionPlugin.ClientState.LocalPlayer?.Name.TextValue ?? "Player";
            ImGui.Text($"Messages with {playerName}: {plugin.ConversationService.GetConversationCount(playerName)}");
        }

        private async Task TestAI()
        {
            isTesting = true;
            lastResponse = "";

            try
            {
                var playerName = AICompanionPlugin.ClientState.LocalPlayer?.Name.TextValue ?? "TestUser";
                var response = await plugin.OllamaService.GetAIResponse($"Test message: {testMessage}", plugin.Configuration.ModelName);
                lastResponse = response;
            }
            catch (Exception ex)
            {
                lastResponse = $"Error: {ex.Message}";
            }
            finally
            {
                isTesting = false;
            }
        }

        private async Task CheckOllamaStatus()
        {
            var ollamaService = new Services.OllamaService();
            var isRunning = await ollamaService.IsOllamaRunning();
            var statusText = isRunning ? "Running" : "Not Running";
            var statusColor = isRunning ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed;

            ImGui.TextColored(statusColor, $"Ollama Status: {statusText}");

            if (!isRunning)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "Please make sure Ollama is running on your system.");
                ImGui.Text("Download from: https://ollama.ai");
            }

            ollamaService.Dispose();
        }
    }
}
