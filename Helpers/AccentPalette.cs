using System.Collections.Generic;
using sy_ftp.Models;

namespace sy_ftp.Helpers;

/// <summary>Palette of accent presets shown in the Settings window.</summary>
public static class AccentPalette
{
    public static IReadOnlyList<AccentColorOption> Options { get; } =
    [
        new("Blue",      "#2296F5"),  // 默认强调色，移到第一位
        new("Indigo",    "#4050B5"),
        new("Sky",       "#0EA5E9"),
        new("Cyan",      "#00BCD4"),
        new("Teal",      "#009688"),
        new("Emerald",   "#10B981"),
        new("Green",     "#4CAF50"),
        new("Lime",      "#84CC16"),
        new("Amber",     "#FF9800"),
        new("Orange",    "#F97316"),
        new("Red",       "#EF5350"),
        new("Rose",      "#F43F5E"),
        new("Pink",      "#E91E63"),
        new("Fuchsia",   "#D946EF"),
        new("Purple",    "#7C4DFF"),
        new("Violet",    "#8B5CF6"),
        new("Slate",     "#64748B"),
        new("Graphite",  "#374151"),
    ];
}
