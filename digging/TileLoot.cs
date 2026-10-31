public class TileLoot
{
    public string ResourceId { get; set; } = "";
    public int Amount { get; set; } = 0;
    public TileStage RevealedAtStage { get; set; } // На какой стадии обнаруживается
    public bool IsBone { get; set; }               // Требует извлекателя
    public bool Collected { get; set; } = false;    // Уже собран/извлечён
}