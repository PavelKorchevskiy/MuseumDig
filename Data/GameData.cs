using System.Collections.Generic;

public static class GameData
{
	// ===== КАТАЛОГИ РЕСУРСОВ =====
	
	private static Dictionary<string, ResourceDefinition> _resources;
	private static Dictionary<string, CollectionDefinition> _collections;
	private static Dictionary<string, LocationDefinition> _locations;
	private static Dictionary<ToolType, ToolDefinition> _tools;
	
	public static void Initialize()
{
	if (_resources != null) return;
	
	_resources = new Dictionary<string, ResourceDefinition>();
	_collections = new Dictionary<string, CollectionDefinition>();
	_locations = new Dictionary<string, LocationDefinition>();
	_tools = new Dictionary<ToolType, ToolDefinition>();
	
	RegisterFossils();
	RegisterMinerals();
	RegisterLocations();
	RegisterTools();
}
	
	// ===== ГЕТТЕРЫ =====
	
	public static ResourceDefinition GetResource(string id)
	{
		Initialize();
		return _resources.TryGetValue(id, out var def) ? def : null;
	}
	
	public static CollectionDefinition GetCollection(string id)
	{
		Initialize();
		return _collections.TryGetValue(id, out var def) ? def : null;
	}
	
	public static LocationDefinition GetLocation(string id)
	{
		Initialize();
		return _locations.TryGetValue(id, out var def) ? def : null;
	}
	
	public static ToolDefinition GetTool(ToolType type)
	{
		Initialize();
		return _tools.TryGetValue(type, out var def) ? def : null;
	}
	
	public static List<LocationDefinition> GetAllLocations()
	{
		Initialize();
		return new List<LocationDefinition>(_locations.Values);
	}
	
	public static List<CollectionDefinition> GetAllCollections()
	{
		Initialize();
		return new List<CollectionDefinition>(_collections.Values);
	}
	
	// ===== РЕГИСТРАЦИЯ ОКАМЕНЕЛОСТЕЙ =====
	
	private static void RegisterFossils()
	{
		// ========== КАНАДА: ТРИЦЕРАТОПС ==========
		RegisterCollectionPieces("triceratops", "Triceratops", "canada",
			"The massive skull with distinctive horns and frill.",
			"The sturdy torso with bony frill.",
			"The long tail with bony spikes.",
			Rarity.Uncommon, 50, 40, 30, 5, 4, 3);
		
		// Зуб динозавра (Канада, отдельный)
		RegisterStandaloneFossil("dino_tooth", "Dinosaur Tooth",
			"A sharp tooth from an unknown dinosaur.",
			ResourceType.Tooth, Rarity.Common, 15, 2);
		
		// ========== ПУСТЫНЯ ГОБИ: ПРОТОЦЕРАТОПС ==========
		RegisterCollectionPieces("protoceratops", "Protoceratops", "gobi",
			"The parrot-like beak skull of Protoceratops.",
			"The compact body with distinctive frill.",
			"The short tail of a young ceratopsian.",
			Rarity.Rare, 70, 55, 45, 7, 5, 4);
		
		// ========== ПУСТЫНЯ ГОБИ: ВЕЛОЦЕРАПТОР ==========
		RegisterCollectionPieces("velociraptor", "Velociraptor", "gobi",
			"The skull of the famous turkey-sized predator.",
			"The agile body with sickle claws.",
			"The stiff tail used for balance.",
			Rarity.Rare, 90, 75, 60, 9, 7, 6);
		
		// ========== ПУСТЫНЯ ГОБИ: ТЕРИЗИНОЗАВР ==========
		RegisterCollectionPieces("therizinosaurus", "Therizinosaurus", "gobi",
			"The small skull of this bizarre herbivore.",
			"The massive body with huge claws.",
			"The short tail of a giant sloth-like dinosaur.",
			Rarity.Rare, 100, 85, 70, 10, 8, 7);
		
		// Окаменелые яйца (Гоби, отдельный)
		RegisterStandaloneFossil("dino_egg", "Fossilized Dinosaur Egg",
			"A perfectly preserved dinosaur egg from the Gobi Desert.",
			ResourceType.Bone, Rarity.Uncommon, 40, 4);
		
		// ========== ГЕОПАРК УНДОРИЯ: ИХТИОЗАВР ==========
		RegisterCollectionPieces("ichthyosaurus", "Ichthyosaurus", "undoria",
			"The dolphin-like skull of an ichthyosaur.",
			"The streamlined body built for swimming.",
			"The tail fluke of a marine reptile.",
			Rarity.Rare, 80, 65, 50, 8, 6, 5);
		
		// ========== ГЕОПАРК УНДОРИЯ: ПЛЕЗИОЗАВР ==========
		RegisterCollectionPieces("plesiosaurus", "Plesiosaurus", "undoria",
			"The small skull on a long neck.",
			"The broad body with four flippers.",
			"The short tail of a plesiosaur.",
			Rarity.Rare, 95, 80, 65, 9, 7, 6);
		
		// Аммониты (Ундория, отдельный)
		RegisterStandaloneFossil("ammonite", "Ammonite Fossil",
			"A beautifully preserved spiral shell from the Jurassic sea.",
			ResourceType.Bone, Rarity.Uncommon, 35, 3);
	}
	
	// ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ РЕГИСТРАЦИИ =====
	
	private static void RegisterCollectionPieces(
		string collectionId, string piece1Name, string piece2Name, string piece3Name,
		string desc1, string desc2, string desc3,
		Rarity rarity, int price1, int price2, int price3,
		int income1, int income2, int income3)
	{
		// Это вспомогательный метод, но нам нужны разные имена для частей.
		// Поэтому используем более простую версию ниже.
	}
	
	// Упрощённая регистрация частей коллекции
	private static void RegisterCollectionPieces(
		string collectionId, string displayName, string locationId,
		string desc1, string desc2, string desc3,
		Rarity rarity, int price1, int price2, int price3,
		int income1, int income2, int income3)
	{
		string[] partNames = { "Skull", "Body", "Tail" };
		string[] descriptions = { desc1, desc2, desc3 };
		int[] prices = { price1, price2, price3 };
		int[] incomes = { income1, income2, income3 };
		
		var pieces = new FossilDefinition[3];
		
		for (int i = 0; i < 3; i++)
		{
			var fossil = new FossilDefinition
			{
				Id = $"{collectionId}_{partNames[i].ToLower()}",
				DisplayName = $"{partNames[i]} of {displayName}",
				Description = descriptions[i],
				Type = ResourceType.Bone,
				Rarity = rarity,
				BaseSellPrice = prices[i],
				BaseMuseumIncome = incomes[i],
				CollectionId = collectionId,
				PieceIndex = i,
				TotalPieces = 3,
				CanExhibitAlone = false
			};
			_resources[fossil.Id] = fossil;
			pieces[i] = fossil;
		}
		
		// Создаём коллекцию
		var collection = new CollectionDefinition
		{
			Id = collectionId,
			DisplayName = displayName,
			Description = GetCollectionDescription(collectionId),
			LocationId = locationId,
			Rarity = rarity,
			CollectionBonus = GetCollectionBonus(rarity)
		};
		
		foreach (var piece in pieces)
		{
			collection.Pieces.Add(piece);
		}
		
		_collections[collection.Id] = collection;
	}
	
	private static void RegisterStandaloneFossil(
		string id, string displayName, string description,
		ResourceType type, Rarity rarity, int sellPrice, int museumIncome)
	{
		var fossil = new FossilDefinition
		{
			Id = id,
			DisplayName = displayName,
			Description = description,
			Type = type,
			Rarity = rarity,
			BaseSellPrice = sellPrice,
			BaseMuseumIncome = museumIncome,
			CollectionId = "",
			PieceIndex = -1,
			TotalPieces = 1,
			CanExhibitAlone = true
		};
		_resources[fossil.Id] = fossil;
	}
	
	private static string GetCollectionDescription(string collectionId)
	{
		return collectionId switch
		{
			"triceratops" => "A majestic horned dinosaur from the late Cretaceous of Canada.",
			"protoceratops" => "A sheep-sized ceratopsian from the sands of the Gobi Desert.",
			"velociraptor" => "A swift, feathered predator made famous by movies.",
			"therizinosaurus" => "A bizarre giant herbivore with meter-long claws.",
			"ichthyosaurus" => "A dolphin-like marine reptile from the Jurassic seas of Undoria.",
			"plesiosaurus" => "An elegant long-necked predator of the ancient Volga sea.",
			_ => "A mysterious collection."
		};
	}
	
	private static float GetCollectionBonus(Rarity rarity)
	{
		return rarity switch
		{
			Rarity.Common => 1.5f,
			Rarity.Uncommon => 2.0f,
			Rarity.Rare => 2.5f,
			Rarity.Epic => 3.0f,
			Rarity.Legendary => 5.0f,
			_ => 1.0f
		};
	}
	
	// ===== РЕГИСТРАЦИЯ МИНЕРАЛОВ =====
	
	private static void RegisterMinerals()
	{
		var gold = new MineralDefinition
		{
			Id = "gold_nugget",
			DisplayName = "Gold Nugget",
			Description = "A shiny piece of gold.",
			Type = ResourceType.Gold,
			Rarity = Rarity.Common,
			BaseSellPrice = 25,
			BaseMuseumIncome = 0,
			MinDropAmount = 1,
			MaxDropAmount = 3
		};
		_resources[gold.Id] = gold;
		
		var gem = new MineralDefinition
		{
			Id = "precious_gem",
			DisplayName = "Precious Gem",
			Description = "A sparkling gemstone.",
			Type = ResourceType.Gem,
			Rarity = Rarity.Uncommon,
			BaseSellPrice = 60,
			BaseMuseumIncome = 0,
			MinDropAmount = 1,
			MaxDropAmount = 1
		};
		_resources[gem.Id] = gem;
	}
	
	// ===== РЕГИСТРАЦИЯ ЛОКАЦИЙ =====
	
	private static void RegisterLocations()
	{
		// ========== КАНАДА ==========
		var canada = new LocationDefinition
		{
			Id = "canada",
			DisplayName = "Canada (Alberta)",
			Description = "Dinosaur Provincial Park. Land of horned giants.",
			UnlockCost = 0,
			RequiredPlayerLevel = 1,
			GridWidth = 8,
			GridHeight = 12,
			BaseTileHp = 3,
			TileHpGrowthPerRow = 1.15f
		};
		
		canada.LootTable.Add(CreateLootEntry("triceratops_skull", 0.03f));
		canada.LootTable.Add(CreateLootEntry("triceratops_body", 0.03f));
		canada.LootTable.Add(CreateLootEntry("triceratops_tail", 0.03f));
		canada.LootTable.Add(CreateLootEntry("dino_tooth", 0.08f));
		canada.LootTable.Add(CreateLootEntry("gold_nugget", 0.12f));
		canada.LootTable.Add(CreateLootEntry("precious_gem", 0.04f));
		
		_locations[canada.Id] = canada;
		
		// ========== ПУСТЫНЯ ГОБИ ==========
		var gobi = new LocationDefinition
		{
			Id = "gobi",
			DisplayName = "Gobi Desert",
			Description = "Mongolia/China. The graveyard of dinosaurs.",
			UnlockCost = 1500,
			RequiredPlayerLevel = 3,
			GridWidth = 10,
			GridHeight = 14,
			BaseTileHp = 5,
			TileHpGrowthPerRow = 1.20f
		};
		
		// Протоцератопс
		gobi.LootTable.Add(CreateLootEntry("protoceratops_skull", 0.025f));
		gobi.LootTable.Add(CreateLootEntry("protoceratops_body", 0.025f));
		gobi.LootTable.Add(CreateLootEntry("protoceratops_tail", 0.025f));
		
		// Велоцераптор
		gobi.LootTable.Add(CreateLootEntry("velociraptor_skull", 0.02f));
		gobi.LootTable.Add(CreateLootEntry("velociraptor_body", 0.02f));
		gobi.LootTable.Add(CreateLootEntry("velociraptor_tail", 0.02f));
		
		// Теризинозавр
		gobi.LootTable.Add(CreateLootEntry("therizinosaurus_skull", 0.015f));
		gobi.LootTable.Add(CreateLootEntry("therizinosaurus_body", 0.015f));
		gobi.LootTable.Add(CreateLootEntry("therizinosaurus_tail", 0.015f));
		
		// Яйца
		gobi.LootTable.Add(CreateLootEntry("dino_egg", 0.06f));
		
		// Минералы
		gobi.LootTable.Add(CreateLootEntry("gold_nugget", 0.12f));
		gobi.LootTable.Add(CreateLootEntry("precious_gem", 0.05f));
		
		_locations[gobi.Id] = gobi;
		
		// ========== ГЕОПАРК УНДОРИЯ ==========
		var undoria = new LocationDefinition
		{
			Id = "undoria",
			DisplayName = "Undoria Geopark",
			Description = "Ulyanovsk, Russia. Jurassic sea fossils on the Volga.",
			UnlockCost = 1500,
			RequiredPlayerLevel = 3,
			GridWidth = 10,
			GridHeight = 14,
			BaseTileHp = 5,
			TileHpGrowthPerRow = 1.20f
		};
		
		// Ихтиозавр
		undoria.LootTable.Add(CreateLootEntry("ichthyosaurus_skull", 0.025f));
		undoria.LootTable.Add(CreateLootEntry("ichthyosaurus_body", 0.025f));
		undoria.LootTable.Add(CreateLootEntry("ichthyosaurus_tail", 0.025f));
		
		// Плезиозавр
		undoria.LootTable.Add(CreateLootEntry("plesiosaurus_skull", 0.02f));
		undoria.LootTable.Add(CreateLootEntry("plesiosaurus_body", 0.02f));
		undoria.LootTable.Add(CreateLootEntry("plesiosaurus_tail", 0.02f));
		
		// Аммониты
		undoria.LootTable.Add(CreateLootEntry("ammonite", 0.08f));
		
		// Минералы
		undoria.LootTable.Add(CreateLootEntry("gold_nugget", 0.12f));
		undoria.LootTable.Add(CreateLootEntry("precious_gem", 0.05f));
		
		_locations[undoria.Id] = undoria;
	}
	
	// ===== РЕГИСТРАЦИЯ ИНСТРУМЕНТОВ =====
	
	private static void RegisterTools()
	{
		var shovel = new ToolDefinition
		{
			Type = ToolType.Shovel,
			DisplayName = "Shovel",
			Description = "Fast digging, but can damage fossils.",
			Damage = 2,
			CanDamageFossil = true,
			UseDelay = 0.25f,
			UpgradeCost = 100
		};
		
		var pickaxe = new ToolDefinition
		{
			Type = ToolType.Pickaxe,
			DisplayName = "Pickaxe",
			Description = "Slower but safe for fossils.",
			Damage = 1,
			CanDamageFossil = false,
			UseDelay = 0.5f,
			UpgradeCost = 150
		};
		
		_tools[shovel.Type] = shovel;
		_tools[pickaxe.Type] = pickaxe;
	}
	
	// ===== УТИЛИТЫ =====
	
	private static LootEntry CreateLootEntry(string resourceId, float chance)
	{
		return new LootEntry
		{
			Resource = _resources[resourceId],
			DropChance = chance
		};
	}
}
