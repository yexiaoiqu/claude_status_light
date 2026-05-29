using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ClaudeStatusLight;

public static class IconGenerator
{
    public static Icon CreateTrafficLightIcon(ClaudeState state)
    {
        var size = 16;
        var bitmap = new Bitmap(size, size);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            // Draw three circles (traffic light)
            var circleSize = 4;
            var spacing = 1;
            var startX = (size - (circleSize * 3 + spacing * 2)) / 2;

            // Red circle (top)
            var redColor = (state == ClaudeState.Standby || state == ClaudeState.Error)
                ? System.Drawing.Color.FromArgb(220, 50, 50)
                : System.Drawing.Color.FromArgb(80, 30, 30);
            using (var brush = new SolidBrush(redColor))
            {
                g.FillEllipse(brush, startX, 1, circleSize, circleSize);
            }

            // Yellow circle (middle)
            var yellowColor = (state == ClaudeState.NeedInput || state == ClaudeState.Thinking)
                ? System.Drawing.Color.FromArgb(240, 200, 40)
                : System.Drawing.Color.FromArgb(80, 70, 20);
            using (var brush = new SolidBrush(yellowColor))
            {
                g.FillEllipse(brush, startX, 1 + circleSize + spacing, circleSize, circleSize);
            }

            // Green circle (bottom)
            var greenColor = (state == ClaudeState.Done || state == ClaudeState.JustDone)
                ? System.Drawing.Color.FromArgb(50, 200, 80)
                : System.Drawing.Color.FromArgb(20, 70, 30);
            using (var brush = new SolidBrush(greenColor))
            {
                g.FillEllipse(brush, startX, 1 + (circleSize + spacing) * 2, circleSize, circleSize);
            }
        }

        var hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        return icon;
    }

    public static void SaveIconToFile(Icon icon, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        icon.Save(stream);
    }
}
