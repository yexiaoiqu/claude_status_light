using System.Windows.Media;

namespace ClaudeStatusLight;

public static class ColorExtensions
{
    public static System.Drawing.Color ToWinFormsColor(this Color wpfColor)
        => System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);

    public static Color ToWpfColor(this System.Drawing.Color winFormsColor)
        => Color.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);
}
