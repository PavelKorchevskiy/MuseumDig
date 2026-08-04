using Godot;

public partial class GameDataLoader : Node
{
	public static GameDataLoader Instance { get; private set; }
	
	public override void _Ready()
	{
		Instance = this;
		
		GD.Print("=== GameDataLoader: Initializing game data... ===");
		GameData.Initialize();
		GD.Print("=== GameDataLoader: Game data ready! ===");
	}
}
