using Godot;

public partial class DigSite : Node2D
{
	[Export] public int TileSize = 64;
	
	public override void _Ready()
	{
		GenerateGrid();
		
		// Подписываемся на сигнал смены локации
		if (LocationSystem.Instance != null)
		{
			LocationSystem.Instance.Connect(
				LocationSystem.SignalName.LocationChanged,
				Callable.From(OnLocationChanged)
			);
		}
	}
	
	private void OnLocationChanged()
	{
		GD.Print("[DigSite] Location changed, regenerating grid...");
		RegenerateGrid();
	}
	
	private void RegenerateGrid()
	{
		// Удаляем все старые тайлы
		foreach (var child in GetChildren())
		{
			if (child is Tile)
			{
				child.QueueFree();
			}
		}
		
		// Генерируем новую сетку
		GenerateGrid();
	}
	
	private void GenerateGrid()
	{
		var location = LocationSystem.Instance.GetCurrentLocation();
		if (location == null)
		{
			GD.PrintErr("Current location not found!");
			return;
		}
		
		int width = location.GridWidth;
		int height = location.GridHeight;
		float baseHp = location.BaseTileHp;
		float hpGrowth = location.TileHpGrowthPerRow;
		
		GD.Print($"[DigSite] Generating grid {width}x{height} for {location.DisplayName}");
		
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var tile = new Tile();
				tile.Position = new Vector2(x * TileSize, y * TileSize);
				tile.CustomMinimumSize = new Vector2(TileSize, TileSize);
				tile.Size = new Vector2(TileSize, TileSize);
				
				tile.Initialize(y, baseHp, hpGrowth);
				
				AddChild(tile);
			}
		}
	}
}
