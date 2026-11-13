using Godot;
using System.Collections.Generic;

/// <summary>
/// Наборы тайлов раскопок по типам местности.
///
/// Набор по умолчанию (луг) лежит прямо в res://assets/tiles/, остальные — в
/// подпапках: res://assets/tiles/desert/ и т.д. Какой набор у локации,
/// задаётся в LocationDefinition.TileSet.
///
/// Имена файлов во всех наборах одинаковые: intact_N, dug1_N, dug2_N,
/// empty_N, back_N, border, border_edge, border_edge_side. Сколько вариантов
/// (N) есть у набора, определяется по файлам — достаточно положить в папку
/// intact_9.png, и он начнёт использоваться без правок кода.
///
/// Если в наборе нет какого-то файла, берётся такой же из набора по умолчанию.
/// </summary>
public static class DigTileSet
{
    private const string Root = "res://assets/tiles/";
    private const int MaxVariants = 64;

    private static readonly Dictionary<string, int> _countCache = new();

    private static string Folder(string tileSet) =>
        string.IsNullOrEmpty(tileSet) ? Root : $"{Root}{tileSet}/";

    /// <summary>Набор тайлов локации ("" — набор по умолчанию).</summary>
    public static string ForLocation(string locationId)
    {
        return GameData.GetLocation(locationId)?.TileSet ?? "";
    }

    /// <summary>Сколько вариантов prefix_1, prefix_2, ... есть в наборе (подряд, начиная с 1).</summary>
    public static int Count(string tileSet, string prefix)
    {
        string key = $"{tileSet}|{prefix}";
        if (_countCache.TryGetValue(key, out int cached)) return cached;

        int count = 0;
        while (count < MaxVariants && ResourceLoader.Exists($"{Folder(tileSet)}{prefix}_{count + 1}.png"))
        {
            count++;
        }

        _countCache[key] = count;
        return count;
    }

    /// <summary>
    /// Путь к варианту тайла. hash — любое неотрицательное число (обычно хеш
    /// координат): из него выбирается номер варианта среди имеющихся в наборе.
    /// </summary>
    public static string Variant(string tileSet, string prefix, int hash)
    {
        int count = Count(tileSet, prefix);
        if (count > 0) return $"{Folder(tileSet)}{prefix}_{(hash % count) + 1}.png";

        // В наборе таких тайлов нет — берём из набора по умолчанию
        if (!string.IsNullOrEmpty(tileSet)) return Variant("", prefix, hash);

        // Запасной вариант для одиночного файла без номера (empty.png)
        return $"{Root}{prefix}.png";
    }

    /// <summary>Путь к одиночному файлу набора (border.png и т.п.).</summary>
    public static string Single(string tileSet, string fileName)
    {
        string path = Folder(tileSet) + fileName;
        if (ResourceLoader.Exists(path) || string.IsNullOrEmpty(tileSet)) return path;
        return Root + fileName;
    }
}
