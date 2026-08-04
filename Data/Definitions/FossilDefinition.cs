using Godot;

[GlobalClass]
public partial class FossilDefinition : ResourceDefinition
{
	// ID коллекции, к которой принадлежит (например, "triceratops")
	// Пустая строка, если это одиночный ресурс (зуб)
	[Export] public string CollectionId = "";
	
	// Индекс части в коллекции (0, 1, 2). -1 если не часть коллекции.
	[Export] public int PieceIndex = -1;
	
	// Всего частей в коллекции
	[Export] public int TotalPieces = 1;
	
	// Можно ли выставлять в музее отдельно (зубы — да, части скелета — нет)
	[Export] public bool CanExhibitAlone = false;
	
	public override bool HasQuality => true;
	
	public override float GetQualityMultiplier(Quality quality)
	{
		return quality switch
		{
			Quality.Damaged => 0.3f,
			Quality.Good => 1.0f,
			_ => 1.0f
		};
	}
}
