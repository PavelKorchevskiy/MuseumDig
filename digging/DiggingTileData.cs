using Godot;
using System;

public class DiggingTileData
{
    public int X { get; set; }
    public int Y { get; set; }
    public TileStage Stage { get; set; } = TileStage.Intact;
    public TileAction Action { get; set; } = TileAction.None;
    
    public float CurrentHP { get; set; }
    public float MaxHP { get; set; }
    public float ExtractionProgress { get; set; } // От 0 до MaxHP
    
    public string HiddenResourceId { get; set; } = "";
    public int HiddenAmount { get; set; } = 0;
    public bool IsFindingExtractable { get; set; } = false;
}