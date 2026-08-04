using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CollectionDefinition : Resource
{
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public string Description = "";
	
	// К какой локации привязана
	[Export] public string LocationId = "";
	
	[Export] public Rarity Rarity = Rarity.Rare;
	
	// Список частей коллекции
	public Array<FossilDefinition> Pieces = new Array<FossilDefinition>();
	
	// Множитель дохода за полный сбор
	[Export] public float CollectionBonus = 2.0f;
}
