using Godot;
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
        if (_resources != null) return; // Уже инициализировано

        _resources = new Dictionary<string, ResourceDefinition>();
        _collections = new Dictionary<string, CollectionDefinition>();
        _locations = new Dictionary<string, LocationDefinition>();
        _tools = new Dictionary<ToolType, ToolDefinition>();

        RegisterFossils();
        RegisterMinerals();
        RegisterLocations();
        RegisterTools();

        GD.Print($"[GameData] Loaded: {_resources.Count} resources, {_collections.Count} collections, {_locations.Count} locations");
    }

    // ===== ГЕТТЕРЫ =====
    public static ResourceDefinition GetResource(string id) => _resources.TryGetValue(id, out var def) ? def : null;
    public static CollectionDefinition GetCollection(string id) => _collections.TryGetValue(id, out var def) ? def : null;
    public static LocationDefinition GetLocation(string id) => _locations.TryGetValue(id, out var def) ? def : null;
    public static ToolDefinition GetTool(ToolType type) => _tools.TryGetValue(type, out var def) ? def : null;
    
    public static List<LocationDefinition> GetAllLocations() => new List<LocationDefinition>(_locations.Values);
    public static List<CollectionDefinition> GetAllCollections() => new List<CollectionDefinition>(_collections.Values);

    // ===== РЕГИСТРАЦИЯ ОКАМЕНЕЛОСТЕЙ =====
    private static void RegisterFossils()
    {
        // Группируем по коллекциям для удобства
        RegisterTriceratops();
        RegisterProtoceratops();
        RegisterVelociraptor();
        RegisterTherizinosaurus();
        RegisterIchthyosaurus();
        RegisterPlesiosaurus();
        
        // Одиночные находки
        RegisterStandaloneFossils();
    }

    private static void RegisterTriceratops()
    {
        string colId = "triceratops";
        Rarity rarity = Rarity.Uncommon;

        var skull = new FossilDefinition { Id = $"{colId}_skull", DisplayName = "Triceratops Skull", Description = "The massive skull with distinctive horns and frill.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 50, BaseMuseumIncome = 5, CollectionId = colId, PieceIndex = 0, TotalPieces = 3, CanExhibitAlone = false };
        var body = new FossilDefinition { Id = $"{colId}_body", DisplayName = "Triceratops Body", Description = "The sturdy torso with bony frill.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 40, BaseMuseumIncome = 4, CollectionId = colId, PieceIndex = 1, TotalPieces = 3, CanExhibitAlone = false };
        var tail = new FossilDefinition { Id = $"{colId}_tail", DisplayName = "Triceratops Tail", Description = "The long tail with bony spikes.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 30, BaseMuseumIncome = 3, CollectionId = colId, PieceIndex = 2, TotalPieces = 3, CanExhibitAlone = false };

        _resources[skull.Id] = skull;
        _resources[body.Id] = body;
        _resources[tail.Id] = tail;

        _collections[colId] = new CollectionDefinition
        {
            Id = colId,
            DisplayName = "Triceratops",
            Description = "A majestic horned dinosaur from the late Cretaceous of Canada.",
            LocationId = "canada",
            Rarity = rarity,
            CollectionBonus = 2.0f, // Uncommon bonus
            MinSizeX = 2, MinSizeY = 2,
            Pieces = new List<FossilDefinition> { skull, body, tail }
        };
    }

    private static void RegisterProtoceratops()
    {
        string colId = "protoceratops";
        Rarity rarity = Rarity.Rare;

        var skull = new FossilDefinition { Id = $"{colId}_skull", DisplayName = "Protoceratops Skull", Description = "The parrot-like beak skull.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 70, BaseMuseumIncome = 7, CollectionId = colId, PieceIndex = 0, TotalPieces = 3, CanExhibitAlone = false };
        var body = new FossilDefinition { Id = $"{colId}_body", DisplayName = "Protoceratops Body", Description = "The compact body with distinctive frill.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 55, BaseMuseumIncome = 5, CollectionId = colId, PieceIndex = 1, TotalPieces = 3, CanExhibitAlone = false };
        var tail = new FossilDefinition { Id = $"{colId}_tail", DisplayName = "Protoceratops Tail", Description = "The short tail of a young ceratopsian.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 45, BaseMuseumIncome = 4, CollectionId = colId, PieceIndex = 2, TotalPieces = 3, CanExhibitAlone = false };

        _resources[skull.Id] = skull; _resources[body.Id] = body; _resources[tail.Id] = tail;

        _collections[colId] = new CollectionDefinition { Id = colId, DisplayName = "Protoceratops", Description = "A sheep-sized ceratopsian from the sands of the Gobi Desert.", LocationId = "gobi", Rarity = rarity, CollectionBonus = 2.5f, MinSizeX = 1, MinSizeY = 1, Pieces = new List<FossilDefinition> { skull, body, tail } };
    }

    private static void RegisterVelociraptor()
    {
        string colId = "velociraptor";
        Rarity rarity = Rarity.Rare;

        var skull = new FossilDefinition { Id = $"{colId}_skull", DisplayName = "Velociraptor Skull", Description = "The skull of the famous turkey-sized predator.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 90, BaseMuseumIncome = 9, CollectionId = colId, PieceIndex = 0, TotalPieces = 3, CanExhibitAlone = false };
        var body = new FossilDefinition { Id = $"{colId}_body", DisplayName = "Velociraptor Body", Description = "The agile body with sickle claws.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 75, BaseMuseumIncome = 7, CollectionId = colId, PieceIndex = 1, TotalPieces = 3, CanExhibitAlone = false };
        var tail = new FossilDefinition { Id = $"{colId}_tail", DisplayName = "Velociraptor Tail", Description = "The stiff tail used for balance.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 60, BaseMuseumIncome = 6, CollectionId = colId, PieceIndex = 2, TotalPieces = 3, CanExhibitAlone = false };

        _resources[skull.Id] = skull; _resources[body.Id] = body; _resources[tail.Id] = tail;
        _collections[colId] = new CollectionDefinition { Id = colId, DisplayName = "Velociraptor", Description = "A swift, feathered predator made famous by movies.", LocationId = "gobi", Rarity = rarity, CollectionBonus = 2.5f, MinSizeX = 1, MinSizeY = 1, Pieces = new List<FossilDefinition> { skull, body, tail } };
    }

    private static void RegisterTherizinosaurus()
    {
        string colId = "therizinosaurus";
        Rarity rarity = Rarity.Rare;

        var skull = new FossilDefinition { Id = $"{colId}_skull", DisplayName = "Therizinosaurus Skull", Description = "The small skull of this bizarre herbivore.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 100, BaseMuseumIncome = 10, CollectionId = colId, PieceIndex = 0, TotalPieces = 3, CanExhibitAlone = false };
        var body = new FossilDefinition { Id = $"{colId}_body", DisplayName = "Therizinosaurus Body", Description = "The massive body with huge claws.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 85, BaseMuseumIncome = 8, CollectionId = colId, PieceIndex = 1, TotalPieces = 3, CanExhibitAlone = false };
        var tail = new FossilDefinition { Id = $"{colId}_tail", DisplayName = "Therizinosaurus Tail", Description = "The short tail of a giant sloth-like dinosaur.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 70, BaseMuseumIncome = 7, CollectionId = colId, PieceIndex = 2, TotalPieces = 3, CanExhibitAlone = false };

        _resources[skull.Id] = skull; _resources[body.Id] = body; _resources[tail.Id] = tail;
        _collections[colId] = new CollectionDefinition { Id = colId, DisplayName = "Therizinosaurus", Description = "A bizarre giant herbivore with meter-long claws.", LocationId = "gobi", Rarity = rarity, CollectionBonus = 2.5f, MinSizeX = 1, MinSizeY = 1, Pieces = new List<FossilDefinition> { skull, body, tail } };
    }

    private static void RegisterIchthyosaurus()
    {
        string colId = "ichthyosaurus";
        Rarity rarity = Rarity.Rare;

        var skull = new FossilDefinition { Id = $"{colId}_skull", DisplayName = "Ichthyosaurus Skull", Description = "The dolphin-like skull of an ichthyosaur.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 80, BaseMuseumIncome = 8, CollectionId = colId, PieceIndex = 0, TotalPieces = 3, CanExhibitAlone = false };
        var body = new FossilDefinition { Id = $"{colId}_body", DisplayName = "Ichthyosaurus Body", Description = "The streamlined body built for swimming.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 65, BaseMuseumIncome = 6, CollectionId = colId, PieceIndex = 1, TotalPieces = 3, CanExhibitAlone = false };
        var tail = new FossilDefinition { Id = $"{colId}_tail", DisplayName = "Ichthyosaurus Tail", Description = "The tail fluke of a marine reptile.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 50, BaseMuseumIncome = 5, CollectionId = colId, PieceIndex = 2, TotalPieces = 3, CanExhibitAlone = false };

        _resources[skull.Id] = skull; _resources[body.Id] = body; _resources[tail.Id] = tail;
        _collections[colId] = new CollectionDefinition { Id = colId, DisplayName = "Ichthyosaurus", Description = "A dolphin-like marine reptile from the Jurassic seas of Undoria.", LocationId = "undoria", Rarity = rarity, CollectionBonus = 2.5f, MinSizeX = 1, MinSizeY = 1, Pieces = new List<FossilDefinition> { skull, body, tail } };
    }

    private static void RegisterPlesiosaurus()
    {
        string colId = "plesiosaurus";
        Rarity rarity = Rarity.Rare;

        var skull = new FossilDefinition { Id = $"{colId}_skull", DisplayName = "Plesiosaurus Skull", Description = "The small skull on a long neck.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 95, BaseMuseumIncome = 9, CollectionId = colId, PieceIndex = 0, TotalPieces = 3, CanExhibitAlone = false };
        var body = new FossilDefinition { Id = $"{colId}_body", DisplayName = "Plesiosaurus Body", Description = "The broad body with four flippers.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 80, BaseMuseumIncome = 7, CollectionId = colId, PieceIndex = 1, TotalPieces = 3, CanExhibitAlone = false };
        var tail = new FossilDefinition { Id = $"{colId}_tail", DisplayName = "Plesiosaurus Tail", Description = "The short tail of a plesiosaur.", Type = ResourceType.Bone, Rarity = rarity, BaseSellPrice = 65, BaseMuseumIncome = 6, CollectionId = colId, PieceIndex = 2, TotalPieces = 3, CanExhibitAlone = false };

        _resources[skull.Id] = skull; _resources[body.Id] = body; _resources[tail.Id] = tail;
        _collections[colId] = new CollectionDefinition { Id = colId, DisplayName = "Plesiosaurus", Description = "An elegant long-necked predator of the ancient Volga sea.", LocationId = "undoria", Rarity = rarity, CollectionBonus = 2.5f, MinSizeX = 2, MinSizeY = 2, Pieces = new List<FossilDefinition> { skull, body, tail } };
    }

    public static void RegisterStandaloneFossils()
    {
        _resources["dino_tooth"] = new FossilDefinition { Id = "dino_tooth", DisplayName = "Dinosaur Tooth", Description = "A sharp tooth from an unknown dinosaur.", Type = ResourceType.Tooth, Rarity = Rarity.Common, BaseSellPrice = 15, BaseMuseumIncome = 2, CollectionId = "", PieceIndex = -1, TotalPieces = 1, CanExhibitAlone = true };
        _resources["dino_egg"] = new FossilDefinition { Id = "dino_egg", DisplayName = "Fossilized Dinosaur Egg", Description = "A perfectly preserved dinosaur egg from the Gobi Desert.", Type = ResourceType.Bone, Rarity = Rarity.Uncommon, BaseSellPrice = 40, BaseMuseumIncome = 4, CollectionId = "", PieceIndex = -1, TotalPieces = 1, CanExhibitAlone = true };
        _resources["ammonite"] = new FossilDefinition { Id = "ammonite", DisplayName = "Ammonite Fossil", Description = "A beautifully preserved spiral shell from the Jurassic sea.", Type = ResourceType.Bone, Rarity = Rarity.Uncommon, BaseSellPrice = 35, BaseMuseumIncome = 3, CollectionId = "", PieceIndex = -1, TotalPieces = 1, CanExhibitAlone = true };
    }

    // ===== РЕГИСТРАЦИЯ МИНЕРАЛОВ =====
    private static void RegisterMinerals()
    {
        _resources["gold_nugget"] = new MineralDefinition { Id = "gold_nugget", DisplayName = "Gold Nugget", Description = "A shiny piece of gold.", Type = ResourceType.Gold, Rarity = Rarity.Common, BaseSellPrice = 25, BaseMuseumIncome = 0, MinDropAmount = 1, MaxDropAmount = 3 };
        _resources["precious_gem"] = new MineralDefinition { Id = "precious_gem", DisplayName = "Precious Gem", Description = "A sparkling gemstone.", Type = ResourceType.Gem, Rarity = Rarity.Uncommon, BaseSellPrice = 60, BaseMuseumIncome = 0, MinDropAmount = 1, MaxDropAmount = 1 };
    }

    // ===== РЕГИСТРАЦИЯ ЛОКАЦИЙ =====
    private static void RegisterLocations()
    {
        _locations["canada"] = new LocationDefinition
        {
            Id = "canada", DisplayName = "Canada (Alberta)", Description = "Dinosaur Provincial Park. Land of horned giants.",
            UnlockCost = 0, RequiredPlayerLevel = 1, GridWidth = 8, GridHeight = 12, BaseTileHp = 3, TileHpGrowthPerRow = 1.15f,
            LootTable = new List<LootEntry>()
            {
                CreateLoot("triceratops_skull", 0.03f), CreateLoot("triceratops_body", 0.03f), CreateLoot("triceratops_tail", 0.03f),
                CreateLoot("dino_tooth", 0.08f), CreateLoot("gold_nugget", 0.12f), CreateLoot("precious_gem", 0.04f)
            }
        };

        _locations["gobi"] = new LocationDefinition
        {
            Id = "gobi", DisplayName = "Gobi Desert", Description = "Mongolia/China. The graveyard of dinosaurs.",
            UnlockCost = 1500, RequiredPlayerLevel = 3, GridWidth = 10, GridHeight = 14, BaseTileHp = 5, TileHpGrowthPerRow = 1.20f,
            LootTable = new List<LootEntry>()
            {
                CreateLoot("protoceratops_skull", 0.025f), CreateLoot("protoceratops_body", 0.025f), CreateLoot("protoceratops_tail", 0.025f),
                CreateLoot("velociraptor_skull", 0.02f), CreateLoot("velociraptor_body", 0.02f), CreateLoot("velociraptor_tail", 0.02f),
                CreateLoot("therizinosaurus_skull", 0.015f), CreateLoot("therizinosaurus_body", 0.015f), CreateLoot("therizinosaurus_tail", 0.015f),
                CreateLoot("dino_egg", 0.06f), CreateLoot("gold_nugget", 0.12f), CreateLoot("precious_gem", 0.05f)
            }
        };

        _locations["undoria"] = new LocationDefinition
        {
            Id = "undoria", DisplayName = "Undoria Geopark", Description = "Ulyanovsk, Russia. Jurassic sea fossils on the Volga.",
            UnlockCost = 1500, RequiredPlayerLevel = 3, GridWidth = 10, GridHeight = 14, BaseTileHp = 5, TileHpGrowthPerRow = 1.20f,
            LootTable = new List<LootEntry>()
            {
                CreateLoot("ichthyosaurus_skull", 0.025f), CreateLoot("ichthyosaurus_body", 0.025f), CreateLoot("ichthyosaurus_tail", 0.025f),
                CreateLoot("plesiosaurus_skull", 0.02f), CreateLoot("plesiosaurus_body", 0.02f), CreateLoot("plesiosaurus_tail", 0.02f),
                CreateLoot("ammonite", 0.08f), CreateLoot("gold_nugget", 0.12f), CreateLoot("precious_gem", 0.05f)
            }
        };
    }

    // ===== РЕГИСТРАЦИЯ ИНСТРУМЕНТОВ =====
    private static void RegisterTools()
    {
        _tools[ToolType.Shovel] = new ToolDefinition { Type = ToolType.Shovel, DisplayName = "Shovel", Description = "Fast digging, but can damage fossils.", Damage = 2, CanDamageFossil = true, UseDelay = 0.25f, DamageChance = 0.5f };
        _tools[ToolType.Pickaxe] = new ToolDefinition { Type = ToolType.Pickaxe, DisplayName = "Pickaxe", Description = "Slower but safe for fossils.", Damage = 1, CanDamageFossil = false, UseDelay = 0.5f, DamageChance = 0.0f };
    }

    // ===== УТИЛИТЫ =====
    private static LootEntry CreateLoot(string resourceId, float chance)
    {
        return new LootEntry { Resource = _resources[resourceId], DropChance = chance };
    }
}