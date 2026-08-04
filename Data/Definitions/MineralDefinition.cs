using Godot;

[GlobalClass]
public partial class MineralDefinition : ResourceDefinition
{
	// Сколько штук выпадает за раз (для золота — несколько кусков)
	[Export] public int MinDropAmount = 1;
	[Export] public int MaxDropAmount = 3;
	
	public override bool HasQuality => false;
}
