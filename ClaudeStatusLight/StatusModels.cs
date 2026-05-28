using System.Text.Json.Serialization;

namespace ClaudeStatusLight;

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
}
