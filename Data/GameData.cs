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
        RegisterAllosaurus();
        
        // Одиночные находки
        RegisterStandaloneFossils();
    }

    private static void RegisterTriceratops()
    {
        RegisterDinosaurCollection(
            colId: "triceratops",
            displayName: "Triceratops",
            description: "Triceratops is a genus of ceratopsian dinosaur that lived during the late Maastrichtian age of the Late Cretaceous period, about 69 to 66 million years ago on the island continent of Laramidia, now forming western North America.",
            locationId: "canada",
            rarity: Rarity.Uncommon,
            size: new Vector2I(3, 3)
        );
    }

    private static void RegisterProtoceratops()
    {
        RegisterDinosaurCollection(
            colId: "protoceratops",
            displayName: "Protoceratops",
            description: "Protoceratops is a genus of small protoceratopsid dinosaurs that lived in Asia during the Late Cretaceous, around 75 to 71 million years ago.",
            locationId: "gobi",
            rarity: Rarity.Rare,
            size: new Vector2I(2, 2)
        );
    }

    private static void RegisterVelociraptor()
    {
        RegisterDinosaurCollection(
            colId: "velociraptor",
            displayName: "Velociraptor",
            description: "Velociraptor is a genus of small dromaeosaurid dinosaurs that lived in Asia during the Late Cretaceous epoch, about 75 million to 71 million years ago.",
            locationId: "gobi",
            rarity: Rarity.Rare,
            size: new Vector2I(2, 2)
        );
    }

    private static void RegisterTherizinosaurus()
    {
        RegisterDinosaurCollection(
            colId: "therizinosaurus",
            displayName: "Therizinosaurus",
            description: "Therizinosaurus is a genus of very large therizinosaurid dinosaurs that lived during the Late Cretaceous period in what is now Asia.",
            locationId: "undoria",
            rarity: Rarity.Rare,
            size: new Vector2I(3, 3)
        );
    }

    private static void RegisterIchthyosaurus()
    {
        RegisterDinosaurCollection(
            colId: "ichthyosaurus",
            displayName: "Ichthyosaurus",
            description: "Ichthyosaurus is an extinct genus of ichthyosaurs from the Early Jurassic of Europe.",
            locationId: "undoria",
            rarity: Rarity.Rare,
            size: new Vector2I(2, 2)
        );
    }

    private static void RegisterPlesiosaurus()
    {
        RegisterDinosaurCollection(
            colId: "plesiosaurus",
            displayName: "Plesiosaurus",
            description: "Plesiosaurus is a genus of extinct, large marine sauropterygian reptile that lived during the Early Jurassic.",
            locationId: "undoria",
            rarity: Rarity.Rare,
            size: new Vector2I(3, 3)
        );
    }

     private static void RegisterAllosaurus()
    {
        RegisterDinosaurCollection(
            colId: "allosaurus",
            displayName: "Allosaurus",
            description: "Allosaurus is a genus of theropod dinosaur that lived 155 to 143 million years ago during the late Jurassic period.",
            locationId: "canada",
            rarity: Rarity.Uncommon,
            size: new Vector2I(3, 3)
        );
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
                CreateLoot("allosaurus_skull", 0.03f), CreateLoot("allosaurus_body", 0.03f), CreateLoot("allosaurus_tail", 0.03f),
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

        private static void RegisterDinosaurCollection(
        string colId, 
        string displayName, 
        string description, 
        string locationId, 
        Rarity rarity, 
        Vector2I size, 
        int baseMuseumIncome = 15,
        int pieceSellPrice = 50, 
        int collectionSellPrice = 500)
    {
        // 1. Создаем 3 части (Череп, Тело, Хвост)
        var skull = new FossilDefinition 
        { 
            Id = $"{colId}_skull", 
            DisplayName = $"{displayName} (Череп)", 
            Description = $"Окаменелый череп {displayName}.", 
            Type = ResourceType.Bone, 
            Rarity = rarity, 
            BaseSellPrice = pieceSellPrice, 
            BaseMuseumIncome = baseMuseumIncome / 3, 
            CollectionId = colId, 
            PieceIndex = 0, 
            TotalPieces = 3, 
            CanExhibitAlone = false 
        };

        var body = new FossilDefinition 
        { 
            Id = $"{colId}_body", 
            DisplayName = $"{displayName} (Тело)", 
            Description = $"Окаменелый туловище {displayName}.", 
            Type = ResourceType.Bone, 
            Rarity = rarity, 
            BaseSellPrice = pieceSellPrice, 
            BaseMuseumIncome = baseMuseumIncome / 3, 
            CollectionId = colId, 
            PieceIndex = 1, 
            TotalPieces = 3, 
            CanExhibitAlone = false 
        };

        var tail = new FossilDefinition 
        { 
            Id = $"{colId}_tail", 
            DisplayName = $"{displayName} (Хвост)", 
            Description = $"Окаменелый хвост {displayName}.", 
            Type = ResourceType.Bone, 
            Rarity = rarity, 
            BaseSellPrice = pieceSellPrice, 
            BaseMuseumIncome = baseMuseumIncome / 3, 
            CollectionId = colId, 
            PieceIndex = 2, 
            TotalPieces = 3, 
            CanExhibitAlone = false 
        };

        // Регистрируем части как ресурсы
        _resources[skull.Id] = skull;
        _resources[body.Id] = body;
        _resources[tail.Id] = tail;

        // 2. Создаем и регистрируем саму Коллекцию
        _collections[colId] = new CollectionDefinition
        {
            Id = colId,
            DisplayName = displayName,
            Description = description,
            LocationId = locationId,
            Rarity = rarity,
            CollectionBonus = rarity == Rarity.Uncommon ? 2.0f : 2.5f, // Умное значение по умолчанию
            Size = size,
            TexturePath = $"res://assets/museum/items/{colId}/full.png", // Автоматический путь!
            BaseMuseumIncome = baseMuseumIncome,
            Pieces = new List<FossilDefinition> { skull, body, tail } // ОБЯЗАТЕЛЬНО ОСТАВЛЯЕМ
        };

        // 3. Регистрируем собранную коллекцию как отдельный предмет инвентаря
        _resources[colId] = new FossilDefinition 
        { 
            Id = colId, 
            DisplayName = $"{displayName} (Собранная)", 
            Description = $"Полный скелет {displayName}, готовый к установке в зале.", 
            Type = ResourceType.Bone, 
            Rarity = rarity, 
            BaseSellPrice = collectionSellPrice, 
            BaseMuseumIncome = baseMuseumIncome, 
            CollectionId = "", 
            PieceIndex = -1, 
            TotalPieces = 1, 
            CanExhibitAlone = true,
            IsCollection = true
        };
    }
}