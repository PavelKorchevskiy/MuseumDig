using Godot;

[GlobalClass]
public partial class LootEntry : Resource
{
	[Export] public ResourceDefinition Resource;
	
	// Шанс выпадения из одного блока (0.0 - 1.0)
	[Export] public float DropChance = 0.1f;
}
