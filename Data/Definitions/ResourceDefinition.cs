using Godot;

[GlobalClass]
public partial class ResourceDefinition : Resource
{
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public string Description = "";
	[Export] public ResourceType Type = ResourceType.Bone;
	[Export] public Rarity Rarity = Rarity.Common;
	
	// Базовая цена продажи
	[Export] public int BaseSellPrice = 10;
	
	// Базовый пассивный доход в музее (в секунду)
	[Export] public int BaseMuseumIncome = 1;
	
	// Имеет ли этот ресурс качество?
	public virtual bool HasQuality => false;
	
	// Множитель дохода в зависимости от качества
	public virtual float GetQualityMultiplier(Quality quality) => 1.0f;
	
	// Множитель дохода в зависимости от редкости
	public float GetRarityMultiplier()
	{
		return Rarity switch
		{
			Rarity.Common => 1.0f,
			Rarity.Uncommon => 2.0f,
			Rarity.Rare => 5.0f,
			Rarity.Epic => 15.0f,
			Rarity.Legendary => 50.0f,
			_ => 1.0f
		};
	}
}
