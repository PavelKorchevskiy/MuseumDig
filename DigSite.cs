using Godot;

public partial class DigSite : Node2D
{
	[Export] public int GridWidth = 5;
	[Export] public int GridHeight = 5;
	[Export] public int TileSize = 64;
	
	public override void _Ready()
	{
		GenerateGrid();
	}
	
	private void GenerateGrid()
	{
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				var tile = new Tile();
				tile.Position = new Vector2(x * TileSize, y * TileSize);
				tile.Size = new Vector2(TileSize - 2, TileSize - 2);
				tile.Color = new Color(0.6f, 0.4f, 0.2f);
				AddChild(tile);
			}
		}
	}
}
