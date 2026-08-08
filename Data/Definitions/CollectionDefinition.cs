using Godot;
using Godot.Collections;
using System.Collections.Generic;

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
	public List<FossilDefinition> Pieces = new List<FossilDefinition>();
	
	// Множитель дохода за полный сбор
	[Export] public float CollectionBonus = 2.0f;
}
