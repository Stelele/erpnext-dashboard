using Domain.CompanySettings;

namespace Application.ChartColors;

public static class ChartColorData
{
    private static readonly Dictionary<PrimaryColor, string[]> Palettes = new()
    {
        [PrimaryColor.Black] = ["#1a1a2e", "#16213e", "#0f3460", "#533483", "#e94560", "#f5a623", "#7ec8e3", "#a0aec0"],
        [PrimaryColor.Red] = ["#ef4444", "#dc2626", "#f97316", "#eab308", "#0ea5e9", "#10b981", "#8b5cf6", "#94a3b8"],
        [PrimaryColor.Orange] = ["#f97316", "#ea580c", "#eab308", "#ef4444", "#0ea5e9", "#8b5cf6", "#10b981", "#94a3b8"],
        [PrimaryColor.Amber] = ["#f59e0b", "#d97706", "#eab308", "#f97316", "#3b82f6", "#8b5cf6", "#ef4444", "#94a3b8"],
        [PrimaryColor.Yellow] = ["#eab308", "#ca8a04", "#f97316", "#f59e0b", "#3b82f6", "#8b5cf6", "#ef4444", "#94a3b8"],
        [PrimaryColor.Lime] = ["#84cc16", "#65a30d", "#22c55e", "#eab308", "#8b5cf6", "#ef4444", "#3b82f6", "#94a3b8"],
        [PrimaryColor.Green] = ["#00DC82", "#60A5FA", "#F87171", "#C084FC", "#FACC15", "#38BDF8", "#2DD4BF", "#94A3B8"],
        [PrimaryColor.Emerald] = ["#00DC82", "#60A5FA", "#F87171", "#C084FC", "#FACC15", "#38BDF8", "#2DD4BF", "#94A3B8"],
        [PrimaryColor.Teal] = ["#14b8a6", "#0d9488", "#06b6d4", "#10b981", "#ef4444", "#f59e0b", "#8b5cf6", "#94a3b8"],
        [PrimaryColor.Cyan] = ["#06b6d4", "#0891b2", "#0ea5e9", "#14b8a6", "#ef4444", "#f97316", "#8b5cf6", "#94a3b8"],
        [PrimaryColor.Sky] = ["#0ea5e9", "#0284c7", "#3b82f6", "#06b6d4", "#f97316", "#ef4444", "#eab308", "#94a3b8"],
        [PrimaryColor.Blue] = ["#3b82f6", "#2563eb", "#6366f1", "#0ea5e9", "#f97316", "#ef4444", "#eab308", "#94a3b8"],
        [PrimaryColor.Indigo] = ["#6366f1", "#4f46e5", "#8b5cf6", "#3b82f6", "#f59e0b", "#ef4444", "#10b981", "#94a3b8"],
        [PrimaryColor.Violet] = ["#8b5cf6", "#7c3aed", "#a855f7", "#6366f1", "#f59e0b", "#10b981", "#ef4444", "#94a3b8"],
        [PrimaryColor.Purple] = ["#a855f7", "#9333ea", "#c084fc", "#8b5cf6", "#f59e0b", "#10b981", "#0ea5e9", "#94a3b8"],
        [PrimaryColor.Fuchsia] = ["#d946ef", "#c026d3", "#e879f9", "#a855f7", "#10b981", "#f59e0b", "#0ea5e9", "#94a3b8"],
        [PrimaryColor.Pink] = ["#ec4899", "#db2777", "#f472b6", "#d946ef", "#10b981", "#0ea5e9", "#f59e0b", "#94a3b8"],
        [PrimaryColor.Rose] = ["#f43f5e", "#e11d48", "#fb7185", "#ec4899", "#10b981", "#0ea5e9", "#f59e0b", "#94a3b8"],
    };

    public static string[]? GetColors(PrimaryColor primaryColor) =>
        Palettes.TryGetValue(primaryColor, out var colors) ? colors : null;
}
