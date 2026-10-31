using Godot;
using System;

public class DiggingTileData
{
    // === Координаты тайла в сетке ===
    public int X { get; set; }
    public int Y { get; set; }
    
    // === Состояние тайла ===
    public TileStage Stage { get; set; } = TileStage.Intact;
    public TileAction Action { get; set; } = TileAction.None;
    
    // === HP (для копания и извлечения) ===
    public float CurrentHP { get; set; }
    public float MaxHP { get; set; }
    public float ExtractionProgress { get; set; }
    
    // === Лут, привязанный к стадии обнаружения ===
    
    /// ID ресурса (пустая строка, если лута нет)
    public string LootResourceId { get; set; } = "";
    
    /// Количество (для минералов может быть > 1)
    public int LootAmount { get; set; } = 0;
    
    /// На какой стадии лут обнаруживается (Dug1, Dug2 или FullyDug)
    public TileStage LootRevealedAtStage { get; set; } = TileStage.Intact;
    
    /// Является ли лут костью (требует извлекателя)
    public bool LootIsBone { get; set; } = false;
    
    /// Уже ли лут собран/извлечён
    public bool LootCollected { get; set; } = false;
}