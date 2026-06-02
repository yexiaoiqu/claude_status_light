using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClaudeStatusLight;

public enum ToolType
{
    Claude,
    Trae,
    Copilot,
    Unknown
}

public enum ClaudeState
{
    Standby,     // 红灯常亮 - 待机
    Error,       // 红灯闪烁 - 出现问题
    NeedInput,   // 黄灯常亮 - 需要交互
    Thinking,    // 黄灯闪烁 - 思考中
    Done,        // 绿灯常亮 - 完成
    JustDone     // 绿灯闪烁 - 刚完成
}

public enum LightMode { Off, On, Blink }

public class StatusData
{
    [JsonPropertyName("state")]
    public string State { get; set; } = "standby";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("tool")]
    public string? Tool { get; set; }

    public ClaudeState GetClaudeState()
    {
        return State?.ToLower() switch
        {
            "thinking" => ClaudeState.Thinking,
            "just_done" => ClaudeState.JustDone,
            "done" => ClaudeState.Done,
            "need_input" => ClaudeState.NeedInput,
            "error" => ClaudeState.Error,
            _ => ClaudeState.Standby
        };
    }

    public ToolType GetToolType()
    {
        return Tool?.ToLower() switch
        {
            "claude" => ToolType.Claude,
            "trae" => ToolType.Trae,
            "copilot" => ToolType.Copilot,
            _ => ToolType.Unknown
        };
    }
}

public class ToolConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("statusFile")]
    public string StatusFile { get; set; } = "";

    [JsonPropertyName("toolType")]
    public string ToolType { get; set; } = "";
}

public class StateDisplayConfig
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "off";

    [JsonPropertyName("lights")]
    public List<string> Lights { get; set; } = new();
}

public class AppConfig
{
    [JsonPropertyName("tools")]
    public List<ToolConfig> Tools { get; set; } = new();

    [JsonPropertyName("autoDetect")]
    public bool AutoDetect { get; set; } = true;

    [JsonPropertyName("activeToolTimeout")]
    public int ActiveToolTimeoutSeconds { get; set; } = 60;

    [JsonPropertyName("stateDisplay")]
    public Dictionary<string, StateDisplayConfig> StateDisplay { get; set; } = new();
}

public class WindowPosition
{
    [JsonPropertyName("left")]
    public double Left { get; set; }

    [JsonPropertyName("top")]
    public double Top { get; set; }
}

public static class StateDisplay
{
    // 红灯模式
    public static LightMode GetRedMode(ClaudeState state) => state switch
    {
        ClaudeState.Standby => LightMode.On,
        ClaudeState.Error => LightMode.Blink,
        _ => LightMode.Off
    };

    // 黄灯模式
    public static LightMode GetYellowMode(ClaudeState state) => state switch
    {
        ClaudeState.NeedInput => LightMode.On,
        ClaudeState.Thinking => LightMode.Blink,
        _ => LightMode.Off
    };

    // 绿灯模式
    public static LightMode GetGreenMode(ClaudeState state) => state switch
    {
        ClaudeState.Done => LightMode.On,
        ClaudeState.JustDone => LightMode.Blink,
        _ => LightMode.Off
    };

    public static string GetLabel(ClaudeState state) => state switch
    {
        ClaudeState.Standby => "待机中",
        ClaudeState.Error => "出现问题",
        ClaudeState.NeedInput => "需要交互",
        ClaudeState.Thinking => "思考中...",
        ClaudeState.Done => "已完成",
        ClaudeState.JustDone => "刚刚完成",
        _ => "未知"
    };

    public static string GetToolDisplayName(ToolType tool) => tool switch
    {
        ToolType.Claude => "Claude",
        ToolType.Trae => "Trae",
        ToolType.Copilot => "Copilot",
        _ => "Unknown"
    };
}

public static class DefaultColors
{
    public static readonly Dictionary<string, StateDisplayConfig> DisplayConfigs = new()
    {
        ["standby"] = new() { Color = "#DC3232", Mode = "on", Lights = ["red"] },
        ["error"] = new() { Color = "#DC3232", Mode = "blink", Lights = ["red"] },
        ["need_input"] = new() { Color = "#F0C828", Mode = "on", Lights = ["yellow"] },
        ["thinking"] = new() { Color = "#F0C828", Mode = "blink", Lights = ["yellow"] },
        ["done"] = new() { Color = "#32C850", Mode = "on", Lights = ["green"] },
        ["just_done"] = new() { Color = "#32C850", Mode = "blink", Lights = ["green"] }
    };
}
