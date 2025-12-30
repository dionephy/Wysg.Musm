using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Net.Http;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.LlamaClient.Models;
using Wysg.Musm.LlamaClient.Services;

namespace Wysg.Musm.LlamaClient.ViewModels;

/// <summary>
/// Display model for chat messages in the UI.
/// </summary>
public class ChatMessageViewModel : INotifyPropertyChanged
{
    private string _role = "user";
    private string _content = string.Empty;
    private bool _isStreaming;
    private DateTime _timestamp = DateTime.Now;
    private string? _toolCallId;
    private List<ToolCall>? _toolCalls;

    public string Role
    {
        get => _role;
        set { _role = value; OnPropertyChanged(); }
    }

    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        set { _isStreaming = value; OnPropertyChanged(); }
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set { _timestamp = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Tool call identifier (used when role == "tool").
    /// </summary>
    public string? ToolCallId
    {
        get => _toolCallId;
        set { _toolCallId = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Tool calls issued by this assistant message (when role == "assistant").
    /// </summary>
    public List<ToolCall>? ToolCalls
    {
        get => _toolCalls;
        set { _toolCalls = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ChatMessageViewModel() { }

    public ChatMessageViewModel(string role, string content)
    {
        _role = role;
        _content = content;
    }

    public ChatMessage ToChatMessage()
    {
        return new ChatMessage(Role, Content)
        {
            ToolCallId = ToolCallId,
            ToolCalls = ToolCalls
        };
    }
}

/// <summary>
/// Main ViewModel for the Llama Client application.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly LlamaApiService _apiService;
    private readonly PresetService _presetService;
    private readonly McpService _mcpService;
    private PresetCollection _presetCollection = new();
    private CancellationTokenSource? _streamingCts;

    #region Backing Fields

    private string _endpoint = "http://127.0.0.1:8000";
    private string _selectedModel = "nvidia/Llama-3.3-70B-Instruct-FP4";
    private ObservableCollection<string> _availableModels = [];
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private string _systemPrompt = "You are a helpful assistant.";
    private ObservableCollection<ChatMessageViewModel> _chatMessages = [];
    private string _userInput = string.Empty;
    private string _streamingOutput = string.Empty;
    private double _temperature = 0.7;
    private double _topP = 1.0;
    private int _maxTokens = 2048;
    private string _stopSequences = string.Empty;
    private bool _useStreaming = true;
    private ObservableCollection<Preset> _presets = [];
    private Preset? _selectedPreset;
    private bool _isGenerating;
    private string _statusMessage = "Ready";
    private string _lastRequestJson = string.Empty;
    private ObservableCollection<McpServerConfig> _mcpServers = [];
    private ObservableCollection<McpTool> _availableTools = [];
    private bool _enableMcpTools;

    #endregion

    #region Properties

    public string Endpoint
    {
        get => _endpoint;
        set { _endpoint = value; OnPropertyChanged(); }
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set { _selectedModel = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        set { _availableModels = value; OnPropertyChanged(); }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set { _connectionStatus = value; OnPropertyChanged(); }
    }

    public string SystemPrompt
    {
        get => _systemPrompt;
        set { _systemPrompt = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ChatMessageViewModel> ChatMessages
    {
        get => _chatMessages;
        set { _chatMessages = value; OnPropertyChanged(); }
    }

    public string UserInput
    {
        get => _userInput;
        set { _userInput = value; OnPropertyChanged(); SendMessageCommand.NotifyCanExecuteChanged(); }
    }

    public string StreamingOutput
    {
        get => _streamingOutput;
        set { _streamingOutput = value; OnPropertyChanged(); }
    }

    public double Temperature
    {
        get => _temperature;
        set { _temperature = value; OnPropertyChanged(); }
    }

    public double TopP
    {
        get => _topP;
        set { _topP = value; OnPropertyChanged(); }
    }

    public int MaxTokens
    {
        get => _maxTokens;
        set { _maxTokens = value; OnPropertyChanged(); }
    }

    public string StopSequences
    {
        get => _stopSequences;
        set { _stopSequences = value; OnPropertyChanged(); }
    }

    public bool UseStreaming
    {
        get => _useStreaming;
        set { _useStreaming = value; OnPropertyChanged(); }
    }

    public ObservableCollection<Preset> Presets
    {
        get => _presets;
        set { _presets = value; OnPropertyChanged(); }
    }

    public Preset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            _selectedPreset = value;
            OnPropertyChanged();
            if (value != null)
            {
                ApplyPreset(value);
                _presetCollection.ActivePresetId = value.Id;
            }
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        set { _isGenerating = value; OnPropertyChanged(); SendMessageCommand.NotifyCanExecuteChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string LastRequestJson
    {
        get => _lastRequestJson;
        set { _lastRequestJson = value; OnPropertyChanged(); }
    }

    public ObservableCollection<McpServerConfig> McpServers
    {
        get => _mcpServers;
        set { _mcpServers = value; OnPropertyChanged(); }
    }

    public ObservableCollection<McpTool> AvailableTools
    {
        get => _availableTools;
        set { _availableTools = value; OnPropertyChanged(); }
    }

    public bool EnableMcpTools
    {
        get => _enableMcpTools;
        set { _enableMcpTools = value; OnPropertyChanged(); }
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand ListModelsCommand { get; }
    public IAsyncRelayCommand SendMessageCommand { get; }
    public IRelayCommand StopGenerationCommand { get; }
    public IRelayCommand ClearChatCommand { get; }
    public IRelayCommand CopyAsJsonCommand { get; }
    public IAsyncRelayCommand ReplayFromClipboardCommand { get; }
    public IAsyncRelayCommand SavePresetCommand { get; }
    public IAsyncRelayCommand NewPresetCommand { get; }
    public IAsyncRelayCommand DuplicatePresetCommand { get; }
    public IAsyncRelayCommand DeletePresetCommand { get; }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public MainViewModel()
    {
        _apiService = new LlamaApiService();
        _presetService = new PresetService();
        _mcpService = new McpService();

        // Initialize commands
        ListModelsCommand = new AsyncRelayCommand(ListModelsAsync);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
        StopGenerationCommand = new RelayCommand(StopGeneration);
        ClearChatCommand = new RelayCommand(ClearChat);
        CopyAsJsonCommand = new RelayCommand(CopyAsJson);
        ReplayFromClipboardCommand = new AsyncRelayCommand(ReplayFromClipboardAsync);
        SavePresetCommand = new AsyncRelayCommand(SavePresetAsync);
        NewPresetCommand = new AsyncRelayCommand(NewPresetAsync);
        DuplicatePresetCommand = new AsyncRelayCommand(DuplicatePresetAsync);
        DeletePresetCommand = new AsyncRelayCommand(DeletePresetAsync);

        // Keep MCP tool list in sync
        _mcpService.ToolsUpdated += (_, tools) =>
        {
            AvailableTools = new ObservableCollection<McpTool>(tools);
        };
        AvailableTools = new ObservableCollection<McpTool>(_mcpService.AllTools);
    }

    /// <summary>
    /// Initializes the ViewModel (call from Window.Loaded).
    /// </summary>
    public async Task InitializeAsync()
    {
        StatusMessage = "Loading presets...";
        _presetCollection = await _presetService.LoadPresetsAsync();
        Presets = new ObservableCollection<Preset>(_presetCollection.Presets);

        var activePreset = _presetCollection.Presets.FirstOrDefault(p => p.Id == _presetCollection.ActivePresetId)
            ?? _presetCollection.Presets.FirstOrDefault();

        if (activePreset != null)
        {
            _selectedPreset = activePreset;
            OnPropertyChanged(nameof(SelectedPreset));
            ApplyPreset(activePreset);
        }

        StatusMessage = "Ready";
    }

    #region Command Implementations

    private async Task ListModelsAsync()
    {
        try
        {
            StatusMessage = "Fetching models...";
            _apiService.BaseUrl = Endpoint;

            var response = await _apiService.ListModelsAsync();
            if (response?.Data != null)
            {
                AvailableModels.Clear();
                foreach (var model in response.Data)
                {
                    AvailableModels.Add(model.Id);
                }

                if (AvailableModels.Count > 0 && !AvailableModels.Contains(SelectedModel))
                {
                    SelectedModel = AvailableModels[0];
                }

                IsConnected = true;
                ConnectionStatus = $"Connected ({AvailableModels.Count} models)";
                StatusMessage = $"Found {AvailableModels.Count} model(s)";
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Connection failed";
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = new ChatMessageViewModel("user", UserInput.Trim());
        ChatMessages.Add(userMessage);

        UserInput = string.Empty;

        try
        {
            IsGenerating = true;
            StatusMessage = "Generating response...";
            _apiService.BaseUrl = Endpoint;

            _streamingCts?.Dispose();
            _streamingCts = new CancellationTokenSource();
            var token = _streamingCts.Token;

            // Build base history (includes system prompt and existing chat + new user message)
            var history = BuildMessageHistory();

            var request = CreateRequest(history);
            LastRequestJson = _apiService.SerializeRequest(request);

            var assistantMessage = new ChatMessageViewModel("assistant", "") { IsStreaming = true };
            ChatMessages.Add(assistantMessage);

            // Streaming with potential tool calls
            var toolCallBuilders = new Dictionary<int, ToolCallBuilder>();
            var receivedToolCalls = false;

            await foreach (var chunk in _apiService.ChatStreamAsync(request, token))
            {
                if (chunk.Choices.Count == 0) continue;
                var choice = chunk.Choices[0];
                var delta = choice.Delta;

                // Accumulate content
                if (!string.IsNullOrEmpty(delta.Content))
                {
                    assistantMessage.Content += delta.Content;
                }

                // Accumulate tool calls
                if (delta.ToolCalls != null && delta.ToolCalls.Count > 0)
                {
                    receivedToolCalls = true;
                    foreach (var tc in delta.ToolCalls)
                    {
                        if (!toolCallBuilders.TryGetValue(tc.Index, out var builder))
                        {
                            builder = new ToolCallBuilder(tc.Index);
                            toolCallBuilders[tc.Index] = builder;
                        }
                        builder.Merge(tc);
                    }
                }

                // Stop if model signaled end
                if (!string.IsNullOrEmpty(choice.FinishReason) && choice.FinishReason != "tool_calls")
                {
                    break;
                }

                if (!string.IsNullOrEmpty(choice.FinishReason) && choice.FinishReason == "tool_calls")
                {
                    break;
                }
            }

            if (receivedToolCalls && toolCallBuilders.Count > 0)
            {
                assistantMessage.ToolCalls = toolCallBuilders.Values
                    .OrderBy(b => b.Index)
                    .Select(b => b.ToToolCall())
                    .ToList();

                assistantMessage.Content = string.IsNullOrWhiteSpace(assistantMessage.Content)
                    ? "(Tool call requested)"
                    : assistantMessage.Content;

                assistantMessage.IsStreaming = false;

                // Execute tools and add tool messages
                foreach (var tc in assistantMessage.ToolCalls)
                {
                    var execResult = await _mcpService.ExecuteToolAsync(tc.Function.Name, tc.Function.Arguments);
                    var toolMsg = new ChatMessageViewModel("tool", execResult.Success ? execResult.Result : $"Error: {execResult.Error ?? "unknown"}")
                    {
                        ToolCallId = tc.Id
                    };
                    ChatMessages.Add(toolMsg);
                }

                // Build follow-up request with tool responses
                var followupHistory = BuildMessageHistory();
                var followupRequest = CreateRequest(followupHistory);
                LastRequestJson = _apiService.SerializeRequest(followupRequest);

                var finalAssistant = new ChatMessageViewModel("assistant", "") { IsStreaming = true };
                ChatMessages.Add(finalAssistant);

                await foreach (var chunk in _apiService.ChatStreamAsync(followupRequest, token))
                {
                    if (chunk.Choices.Count == 0) continue;
                    var choice = chunk.Choices[0];
                    var delta = choice.Delta;
                    if (!string.IsNullOrEmpty(delta.Content))
                    {
                        finalAssistant.Content += delta.Content;
                    }

                    if (!string.IsNullOrEmpty(choice.FinishReason))
                    {
                        break;
                    }
                }

                finalAssistant.IsStreaming = false;
                StatusMessage = "Response complete";
            }
            else
            {
                assistantMessage.IsStreaming = false;
                StatusMessage = "Response complete";
            }
        }
        catch (HttpRequestException httpEx)
        {
            StatusMessage = $"HTTP error: {httpEx.Message}";
            MessageBox.Show($"HTTP error: {httpEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generation cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to get response: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
            _streamingCts?.Dispose();
            _streamingCts = null;
        }
    }

    private bool CanSendMessage() => !IsGenerating && !string.IsNullOrWhiteSpace(UserInput);

    private ChatCompletionRequest CreateRequest(List<ChatMessage> messages)
    {
        var stopList = StopSequences
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var request = new ChatCompletionRequest
        {
            Model = SelectedModel,
            Messages = messages,
            Temperature = Temperature,
            TopP = TopP > 0 && TopP < 1 ? TopP : null,
            MaxTokens = MaxTokens,
            Stop = stopList.Count > 0 ? stopList : null,
            Stream = UseStreaming
        };

        if (EnableMcpTools && AvailableTools.Count > 0)
        {
            request.Tools = AvailableTools
                .Where(t => t.IsEnabled)
                .Select(t => t.ToOpenAiTool())
                .ToList();
        }

        return request;
    }

    private List<ChatMessage> BuildMessageHistory()
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(SystemPrompt))
        {
            messages.Add(ChatMessage.System(SystemPrompt));
        }

        foreach (var msg in ChatMessages)
        {
            messages.Add(msg.ToChatMessage());
        }

        return messages;
    }

    private void StopGeneration()
    {
        _streamingCts?.Cancel();
        _apiService.CancelCurrentRequest();
        StatusMessage = "Stopping...";
    }

    private void ClearChat()
    {
        ChatMessages.Clear();
        StreamingOutput = string.Empty;
        StatusMessage = "Chat cleared";
    }

    private void CopyAsJson()
    {
        if (!string.IsNullOrEmpty(LastRequestJson))
        {
            Clipboard.SetText(LastRequestJson);
            StatusMessage = "Request JSON copied to clipboard";
        }
    }

    private async Task ReplayFromClipboardAsync()
    {
        try
        {
            var json = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(json))
            {
                MessageBox.Show("Clipboard is empty", "Replay", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var request = _apiService.DeserializeRequest(json);
            if (request == null)
            {
                MessageBox.Show("Invalid JSON in clipboard", "Replay", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Restore messages to chat
            ChatMessages.Clear();
            foreach (var msg in request.Messages)
            {
                if (msg.Role == "system")
                {
                    SystemPrompt = msg.Content;
                }
                else
                {
                    ChatMessages.Add(new ChatMessageViewModel(msg.Role, msg.Content));
                }
            }

            // Restore parameters
            Temperature = request.Temperature;
            MaxTokens = request.MaxTokens;
            TopP = request.TopP ?? 1.0;
            SelectedModel = request.Model;
            UseStreaming = request.Stream;

            StatusMessage = "Request loaded from clipboard";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to replay: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Preset Commands

    private async Task SavePresetAsync()
    {
        if (SelectedPreset == null)
            return;

        UpdatePresetFromCurrentSettings(SelectedPreset);
        await _presetService.UpdatePresetAsync(_presetCollection, SelectedPreset);
        StatusMessage = $"Preset '{SelectedPreset.Name}' saved";
    }

    private async Task NewPresetAsync()
    {
        var preset = new Preset
        {
            Name = $"Preset {Presets.Count + 1}"
        };
        UpdatePresetFromCurrentSettings(preset);

        await _presetService.AddPresetAsync(_presetCollection, preset);
        Presets.Add(preset);
        SelectedPreset = preset;
        StatusMessage = $"Created preset '{preset.Name}'";
    }

    private async Task DuplicatePresetAsync()
    {
        if (SelectedPreset == null)
            return;

        var clone = await _presetService.DuplicatePresetAsync(_presetCollection, SelectedPreset.Id);
        Presets.Add(clone);
        SelectedPreset = clone;
        StatusMessage = $"Created preset '{clone.Name}'";
    }

    private async Task DeletePresetAsync()
    {
        if (SelectedPreset == null || Presets.Count <= 1)
            return;

        var result = MessageBox.Show(
            $"Delete preset '{SelectedPreset.Name}'?",
            "Delete Preset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        var presetToDelete = SelectedPreset;
        var index = Presets.IndexOf(presetToDelete);

        await _presetService.DeletePresetAsync(_presetCollection, presetToDelete.Id);
        Presets.Remove(presetToDelete);

        SelectedPreset = Presets.Count > index ? Presets[index] : Presets.LastOrDefault();
        StatusMessage = $"Deleted preset '{presetToDelete.Name}'";
    }

    private void ApplyPreset(Preset preset)
    {
        Endpoint = preset.Endpoint;
        SelectedModel = preset.Model;
        SystemPrompt = preset.SystemPrompt;
        Temperature = preset.Temperature;
        TopP = preset.TopP;
        MaxTokens = preset.MaxTokens;
        StopSequences = string.Join(", ", preset.StopSequences);
        UseStreaming = preset.UseStreaming;
        _apiService.BaseUrl = preset.Endpoint;
    }

    private void UpdatePresetFromCurrentSettings(Preset preset)
    {
        preset.Endpoint = Endpoint;
        preset.Model = SelectedModel;
        preset.SystemPrompt = SystemPrompt;
        preset.Temperature = Temperature;
        preset.TopP = TopP;
        preset.MaxTokens = MaxTokens;
        preset.StopSequences = StopSequences
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        preset.UseStreaming = UseStreaming;
        preset.ModifiedAt = DateTime.UtcNow;
    }

    #endregion

    #region Helpers

    // BuildChatRequest removed in favor of CreateRequest/BuildMessageHistory

    #endregion

    public void Cleanup()
    {
        _streamingCts?.Cancel();
        _streamingCts?.Dispose();
        _apiService.Dispose();
    }
}

internal sealed class ToolCallBuilder
{
    public int Index { get; }
    public string? Id { get; private set; }
    public string? Name { get; private set; }
    public StringBuilder Arguments { get; } = new();

    public ToolCallBuilder(int index)
    {
        Index = index;
    }

    public void Merge(ToolCallDelta delta)
    {
        if (!string.IsNullOrWhiteSpace(delta.Id))
        {
            Id = delta.Id;
        }
        if (!string.IsNullOrWhiteSpace(delta.Function?.Name))
        {
            Name = delta.Function.Name;
        }
        if (!string.IsNullOrEmpty(delta.Function?.Arguments))
        {
            Arguments.Append(delta.Function.Arguments);
        }
    }

    public ToolCall ToToolCall()
    {
        return new ToolCall
        {
            Id = Id ?? Guid.NewGuid().ToString(),
            Type = "function",
            Function = new FunctionCall
            {
                Name = Name ?? "unknown_tool",
                Arguments = Arguments.ToString()
            }
        };
    }
}
