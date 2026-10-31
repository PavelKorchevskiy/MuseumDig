using Godot;
using System;
using System.Collections.Generic;

public partial class DigSiteBackground : Node
{
    private const float TileWidth = 128f;
    private const float TileHeight = 74f;
    private const int BackgroundVariantCount = 6;
    private const int PaddingTiles = 6;
    
    // === НОВОЕ: Эмпирическая корректировка для стыковки с игровым полем ===
    // Если поле "ниже и правее на один тайл", двигаем фон вверх и влево
    private static readonly Vector2 AlignmentOffset = new Vector2(64f, 64f);
    
    private Node2D _parent;

     public void Setup(Node2D parent, LocationDefinition location)
    {
        _parent = parent;
        
        int gridWidth = location.GridWidth;
        int gridHeight = location.GridHeight;

        // Фоновые тайлы
        for (int gx = -PaddingTiles; gx < gridWidth + PaddingTiles; gx++)
        {
            for (int gy = -PaddingTiles; gy < gridHeight + PaddingTiles; gy++)
            {
                if (gx >= 0 && gx < gridWidth && gy >= 0 && gy < gridHeight)
                    continue;

                var bgTile = CreateBackgroundTile(gx, gy, gridWidth, gridHeight);
                _parent.AddChild(bgTile);
            }
        }

        
        // Слой intact_1 по периметру ===
        AddPerimeterIntactTiles(gridWidth, gridHeight);

        // Забор по периметру (поверх intact_1)
        AddBorderFence(gridWidth, gridHeight);
        
        // Угловые тайлы (поверх всего)
        AddCornerFences(gridWidth, gridHeight);

        AddVignette();
    }

    private Sprite2D CreateBackgroundTile(int gx, int gy, int gridWidth, int gridHeight)
    {
        var sprite = new Sprite2D();
        sprite.ZIndex = gx + gy;

        int variant = (HashCoordinates(gx, gy, 100) % BackgroundVariantCount) + 1;
        string path = $"res://assets/tiles/back_{variant}.png";
        
        if (ResourceLoader.Exists(path))
        {
            sprite.Texture = GD.Load<Texture2D>(path);
        }

        // Базовая позиция по формуле из Tile.UpdatePosition()
        float isoX = (gx - gy) * (TileWidth / 2f);
        float isoY = (gx + gy) * (TileHeight / 2f);
        float verticalOffset = (128f - TileHeight) / 2f;
        
        // === ПРИМЕНЯЕМ КОРРЕКТИРОВКУ ===
        sprite.Position = new Vector2(isoX, isoY - verticalOffset) + AlignmentOffset;

        int ring = GetRingDistance(gx, gy, gridWidth, gridHeight);
        
        if (ring >= PaddingTiles - 1)
        {
            sprite.Modulate = new Color(1f, 1f, 1f, 0.4f);
        }
        else if (ring == 1)
        {
            sprite.Modulate = new Color(1f, 1f, 1f, 0.8f);
        }
        else
        {
            sprite.Modulate = new Color(1f, 1f, 1f, 1f);
        }

        return sprite;
    }

    private int GetRingDistance(int gx, int gy, int gridWidth, int gridHeight)
    {
        int distLeft = Math.Max(0, -gx);
        int distRight = Math.Max(0, gx - gridWidth + 1);
        int distTop = Math.Max(0, -gy);
        int distBottom = Math.Max(0, gy - gridHeight + 1);
        return Math.Max(Math.Max(distLeft, distRight), Math.Max(distTop, distBottom));
    }

    private static int HashCoordinates(int x, int y, int salt)
    {
        long hash = x * 73856093L ^ y * 19349663L ^ salt * 83492791L;
        return Mathf.Abs((int)(hash & 0x7FFFFFFF));
    }

    private void AddVignette()
    {
        var vignette = new CanvasLayer();
        vignette.Layer = 5;

        var colorRect = new ColorRect();
        colorRect.AnchorRight = 1;
        colorRect.AnchorBottom = 1;
        colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;

        var shaderCode = @"
shader_type canvas_item;

void fragment() {
    vec2 uv = UV * 2.0 - 1.0;
    float dist = length(uv);
    float vignette = smoothstep(0.4, 1.4, dist);
    COLOR = vec4(0.0, 0.0, 0.0, vignette * 0.7);
}";

        var shader = new Shader();
        shader.Code = shaderCode;
        
        var material = new ShaderMaterial();
        material.Shader = shader;
        colorRect.Material = material;

        vignette.AddChild(colorRect);
        _parent.AddChild(vignette);
    }

    private void AddBorderFence(int gridWidth, int gridHeight)
    {
        string borderPath = "res://assets/tiles/border.png";
        if (!ResourceLoader.Exists(borderPath))
        {
            GD.PrintErr("[Background] border.png не найден!");
            return;
        }
        
        var borderTexture = GD.Load<Texture2D>(borderPath);

        // 4 границы: (startX, startY, endX, endY, flipH)
        var borders = new (int startX, int startY, int endX, int endY, bool flipH)[]
        {
            // Северная граница: y = -1, x от 0 до gridWidth-1
            (0, -1, gridWidth - 1, -1, false),
            
            // Восточная граница: x = gridWidth, y от 0 до gridHeight-1
            (gridWidth, 0, gridWidth, gridHeight - 1, true),
            
            // Южная граница: y = gridHeight, x от 0 до gridWidth-1
            (0, gridHeight, gridWidth - 1, gridHeight, false),
            
            // Западная граница: x = -1, y от 0 до gridHeight-1
            (-1, 0, -1, gridHeight - 1, true),
        };

        foreach (var (startX, startY, endX, endY, flipH) in borders)
        {
            // === ИСПРАВЛЕНО: используем Math.Sign для корректного определения шага ===
            int stepX = Math.Sign(endX - startX);  // Было: endX >= startX ? 1 : -1
            int stepY = Math.Sign(endY - startY);  // Было: endY >= startY ? 1 : -1
            
            int x = startX;
            int y = startY;
            int iterations = 0;
            const int maxIterations = 100; // Защита от бесконечного цикла
            
            while (iterations < maxIterations)
            {
                AddBorderTile(x, y, flipH, borderTexture);
                
                if (x == endX && y == endY) break;
                
                x += stepX;
                y += stepY;
                iterations++;
            }
            
            if (iterations >= maxIterations)
            {
                GD.PrintErr($"[Background] ⚠️ Превышен лимит итераций для границы ({startX},{startY}) → ({endX},{endY})");
            }
        }
    }

private void AddBorderTile(int gx, int gy, bool flipH, Texture2D texture)
    {
        var sprite = new Sprite2D();
        sprite.Texture = texture;
        sprite.FlipH = flipH;
        
        // === ИЗМЕНЕНО: +1 поверх intact_1 (было +1, но теперь это корректно) ===
        sprite.ZIndex = gx + gy + 1;

        float isoX = (gx - gy) * (TileWidth / 2f);
        float isoY = (gx + gy) * (TileHeight / 2f);
        float verticalOffset = (128f - TileHeight) / 2f;
        
        sprite.Position = new Vector2(isoX, isoY - verticalOffset) + AlignmentOffset;
        
        _parent.AddChild(sprite);
    }

    private void AddCornerFences(int gridWidth, int gridHeight)
    {
        string topCornerPath = "res://assets/tiles/border_edge.png";
        string sideCornerPath = "res://assets/tiles/border_edge_side.png";
        
        if (!ResourceLoader.Exists(topCornerPath) || !ResourceLoader.Exists(sideCornerPath))
        {
            GD.PrintErr("[Background] Не найдены текстуры углов!");
            return;
        }
        
        var topCornerTexture = GD.Load<Texture2D>(topCornerPath);
        var sideCornerTexture = GD.Load<Texture2D>(sideCornerPath);

        // 4 угла: (gx, gy, texture, flipH)
        // Верхние углы — используют border_edge.png без изменений
        // Нижний левый — border_edge_side.png без изменений
        // Нижний правый — border_edge_side.png с отражением по горизонтали
        var corners = new (float gx, float gy, Texture2D texture, bool flipH, bool FlipV)[]
        {
            // Северо-западный угол (верх-лево)
            (-1, -1, topCornerTexture, false, false),
            
            // Северо-восточный угол (верх-право)
            (gridWidth, -1, sideCornerTexture, true, false),
            
            // Юго-западный угол (низ-лево)
            (-1, gridHeight, sideCornerTexture, false, false),
            
            // Юго-восточный угол (низ-право) — отражён по горизонтали
            (gridWidth - 0.6f, gridHeight - 0.6f, topCornerTexture, false, true),
        };

        foreach (var (gx, gy, texture, flipH, FlipV) in corners)
        {
            AddCornerTile(gx, gy, texture, flipH, FlipV);
        }
    }

    private void AddCornerTile(float gx, float gy, Texture2D texture, bool flipH, bool FlipV)
    {
        var sprite = new Sprite2D();
        sprite.Texture = texture;
        sprite.FlipH = flipH;
        sprite.FlipV = FlipV;
        sprite.RotationDegrees = 0f;
        
        // Z-индекс выше забора, чтобы угол был поверх
        sprite.ZIndex = (int)(gx + gy + 2);

        float isoX = (gx - gy) * (TileWidth / 2f);
        float isoY = (gx + gy) * (TileHeight / 2f);
        float verticalOffset = (128f - TileHeight) / 2f;
        
        sprite.Position = new Vector2(isoX, isoY - verticalOffset) + AlignmentOffset;
        
        _parent.AddChild(sprite);
    }

        private void AddPerimeterIntactTiles(int gridWidth, int gridHeight)
    {
        string intactPath = "res://assets/tiles/intact_1.png";
        if (!ResourceLoader.Exists(intactPath))
        {
            GD.PrintErr("[Background] intact_1.png не найден!");
            return;
        }
        
        var intactTexture = GD.Load<Texture2D>(intactPath);

        // Собираем все позиции по периметру (границы + углы)
        var perimeterPositions = new List<(int gx, int gy)>();
        
        // Северная граница
        for (int gx = 0; gx < gridWidth; gx++)
            perimeterPositions.Add((gx, -1));
        
        // Восточная граница
        for (int gy = 0; gy < gridHeight; gy++)
            perimeterPositions.Add((gridWidth, gy));
        
        // Южная граница
        for (int gx = 0; gx < gridWidth; gx++)
            perimeterPositions.Add((gx, gridHeight));
        
        // Западная граница
        for (int gy = 0; gy < gridHeight; gy++)
            perimeterPositions.Add((-1, gy));
        
        // 4 угла
        perimeterPositions.Add((-1, -1));
        perimeterPositions.Add((gridWidth, -1));
        perimeterPositions.Add((-1, gridHeight));
        perimeterPositions.Add((gridWidth, gridHeight));

        // Размещаем тайл intact_1 в каждой позиции
        foreach (var (gx, gy) in perimeterPositions)
        {
            var sprite = new Sprite2D();
            sprite.Texture = intactTexture;
            
            // Тот же Z-индекс, что и у фоновых тайлов (одинаковый уровень)
            sprite.ZIndex = gx + gy;

            float isoX = (gx - gy) * (TileWidth / 2f);
            float isoY = (gx + gy) * (TileHeight / 2f);
            float verticalOffset = (128f - TileHeight) / 2f;
            
            sprite.Position = new Vector2(isoX, isoY - verticalOffset) + AlignmentOffset;
            
            _parent.AddChild(sprite);
        }
    }
}