using Godot;

public partial class GameDataLoader : Node
{
	public static GameDataLoader Instance { get; private set; }
	
	public override void _Ready()
	{
		Instance = this;
		// Инициализируем DevMode первым
    	var _ = DevMode.Instance;
		
		GD.Print("=== GameDataLoader: Initializing game data... ===");
		GameData.Initialize();
		GD.Print("=== GameDataLoader: Game data ready! ===");
	}
}
