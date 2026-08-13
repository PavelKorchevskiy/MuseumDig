using Godot;

public static class IsoUtils
{
    // Размер изометрического тайла
    public const int TileWidth = 64;
    public const int TileHeight = 32;
    
    /// <summary>
    /// Преобразует координаты сетки в экранные координаты (изометрия)
    /// </summary>
    public static Vector2 GridToIso(int gridX, int gridY)
    {
        float screenX = (gridX - gridY) * (TileWidth / 2f);
        float screenY = (gridX + gridY) * (TileHeight / 2f);
        return new Vector2(screenX, screenY);
    }
    
    /// <summary>
    /// Преобразует экранные координаты в координаты сетки
    /// </summary>
    public static Vector2I IsoToGrid(float screenX, float screenY)
    {
        float gridX = (screenX / (TileWidth / 2f) + screenY / (TileHeight / 2f)) / 2f;
        float gridY = (screenY / (TileHeight / 2f) - screenX / (TileWidth / 2f)) / 2f;
        return new Vector2I(Mathf.FloorToInt(gridX), Mathf.FloorToInt(gridY));
    }
    
    /// <summary>
    /// Вычисляет Z-порядок для правильной отрисовки (дальние объекты рисуются первыми)
    /// </summary>
    public static int GetZOrder(int gridX, int gridY)
    {
        return gridX + gridY;
    }
}