using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using System;
using System.IO;
using System.Threading.Tasks;
using AICompanionPlugin.Services;
using AICompanionPlugin.Windows;

namespace AICompanionPlugin
{
    public sealed class AICompanionPlugin : IDalamudPlugin
    {
        public string Name => "AI Companion Plugin";

        private const string CommandName = "/aicompanion";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        private WindowSystem WindowSystem { get; init; }

        // Make services public so they can be accessed from Windows namespace
        public OllamaService OllamaService { get; init; } = null!;
        public MemoryService MemoryService { get; init; } = null!;
        public PersonaService PersonaService { get; init; } = null!;
        public ConversationService ConversationService { get; init; } = null!;
        private MainWindow MainWindow { get; init; } = null!;

        public AICompanionPlugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Initialize services
            OllamaService = new OllamaService();
            MemoryService = new MemoryService(PluginInterface.ConfigDirectory.FullName);
            PersonaService = new PersonaService(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName ?? "", "persona.txt"));
            ConversationService = new ConversationService();

            // Initialize window system
            WindowSystem = new WindowSystem("AICompanionPlugin");
            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open AI Companion settings or chat with your AI companion"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += OpenMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += OpenMainUI;
        }

        private void OnCommand(string command, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                OpenMainUI();
                return;
            }

            // Handle chat commands
            _ = HandleChatCommand(args);
        }

        private async Task HandleChatCommand(string message)
        {
            try
            {
                var playerName = ClientState.LocalPlayer?.Name.TextValue ?? "Player";

                // Get conversation context
                var conversationHistory = ConversationService.GetConversationHistory(playerName);
                // FIX: Convert memories to string format
                var memories = MemoryService.GetMemoriesAsString(playerName);
                var persona = PersonaService.GetPersonaTemplate();

                // Build prompt with all context
                var fullPrompt = PersonaService.BuildPrompt(persona, playerName, message, conversationHistory, memories);

                // Get AI response
                var response = await OllamaService.GetAIResponse(fullPrompt, Configuration.ModelName);

                if (!string.IsNullOrEmpty(response))
                {
                    // Save to memory and conversation
                    MemoryService.AddMemory(playerName, $"User: {message}");
                    MemoryService.AddMemory(playerName, $"AI: {response}");
                    ConversationService.AddToConversation(playerName, message, response);

                    // Display response in chat
                    ChatGui.Print($"[AI] {response}");
                }
                else
                {
                    ChatGui.PrintError("[AI] Failed to get response from Ollama");
                }
            }
            catch (Exception ex)
            {
                ChatGui.PrintError($"[AI] Error: {ex.Message}");
            }
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void OpenMainUI()
        {
            MainWindow.IsOpen = true;
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenMainUI;

            WindowSystem.RemoveAllWindows();

            OllamaService?.Dispose();
            MemoryService?.Dispose();
        }
    }
}
