using Godot;
using System.Collections.Generic;

public static class GameData
{
    // ===== КАТАЛОГИ РЕСУРСОВ =====
    private static Dictionary<string, ResourceDefinition> _resources;
    private static Dictionary<string, CollectionDefinition> _collections;
    private static Dictionary<string, LocationDefinition> _locations;

        private static Dictionary<string, TicketDefinition> _tickets = new();

    public static void Initialize()
    {
        if (_resources != null) return; // Уже инициализировано

        _resources = new Dictionary<string, ResourceDefinition>();
        _collections = new Dictionary<string, CollectionDefinition>();
        _locations = new Dictionary<string, LocationDefinition>();

        RegisterFossils();
        RegisterMinerals();
        RegisterLocations();
        RegisterTickets();
        RegisterTicketsAsResources();

        GD.Print($"[GameData] Loaded: {_resources.Count} resources, {_collections.Count} collections, {_locations.Count} locations");
    }

    // ===== ГЕТТЕРЫ =====
    public static ResourceDefinition GetResource(string id) => _resources.TryGetValue(id, out var def) ? def : null;
    public static CollectionDefinition GetCollection(string id) => _collections.TryGetValue(id, out var def) ? def : null;
    public static LocationDefinition GetLocation(string id) => _locations.TryGetValue(id, out var def) ? def : null;
    
    public static List<LocationDefinition> GetAllLocations() => new List<LocationDefinition>(_locations.Values);
    public static List<CollectionDefinition> GetAllCollections() => new List<CollectionDefinition>(_collections.Values);

        public static TicketDefinition GetTicket(string id)
    {
        return _tickets.TryGetValue(id, out var ticket) ? ticket : null;
    }

    public static bool IsTicket(string id)
    {
        return _tickets.ContainsKey(id);
    }

    public static List<TicketDefinition> GetAllTickets()
    {
        return new List<TicketDefinition>(_tickets.Values);
    }

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
        RegisterDilophosaurus();
        RegisterDimetrodon();
        RegisterStegosaurus();
        
        // Одиночные находки
        RegisterStandaloneFossils();
        RegisterConcretions();
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
            locationId: "morrison",
            rarity: Rarity.Uncommon,
            size: new Vector2I(5, 5)
        );
    }

     private static void RegisterDilophosaurus()
    {
        RegisterDinosaurCollection(
            colId: "dilophosaurus",
            displayName: "Dilophosaurus",
            description: "Dilophosaurus is a genus of theropod dinosaurs that lived in what is now North America during the Early Jurassic, about 186 million years ago.",
            locationId: "canada",
            rarity: Rarity.Uncommon,
            size: new Vector2I(2, 2)
        );
    }

     private static void RegisterDimetrodon()
    {
        RegisterDinosaurCollection(
            colId: "dimetrodon",
            displayName: "Dimetrodon",
            description: "Dimetrodon is an extinct genus of sphenacodontid synapsid that lived during the Cisuralian to early Guadalupian epoch of the Permian period, around 294.1–271 million years ago.",
            locationId: "texas",
            rarity: Rarity.Uncommon,
            size: new Vector2I(2, 2)
        );
    }

     private static void RegisterStegosaurus()
    {
        RegisterDinosaurCollection(
            colId: "stegosaurus",
            displayName: "Stegosaurus",
            description: "Stegosaurus is a genus of extinct herbivorous four-legged armored dinosaurs from the Late Jurassic.",
            locationId: "morrison",
            rarity: Rarity.Uncommon,
            size: new Vector2I(5, 5)
        );
    }

    public static void RegisterStandaloneFossils()
{
    // === СУЩЕСТВУЮЩИЕ (обновлённые) ===
    _resources["dino_tooth"] = new FossilDefinition 
    { 
        Id = "dino_tooth", DisplayName = "Dinosaur Tooth", 
        Description = "A sharp tooth from an unknown dinosaur.", 
        Type = ResourceType.Tooth, Rarity = Rarity.Common, 
        BaseSellPrice = 15, BaseMuseumIncome = 2, 
        VariantCount = 2, CanExhibitAlone = true 
    };
    
    _resources["dino_egg"] = new FossilDefinition 
    { 
        Id = "dino_egg", DisplayName = "Fossilized Dinosaur Egg", 
        Description = "A perfectly preserved dinosaur egg from the Gobi Desert.", 
        Type = ResourceType.Egg, Rarity = Rarity.Uncommon, 
        BaseSellPrice = 40, BaseMuseumIncome = 4, 
        VariantCount = 2, CanExhibitAlone = true 
    };
    
    // === НОВЫЕ ===
    _resources["ammonite"] = new FossilDefinition 
    { 
        Id = "ammonite", DisplayName = "Ammonite Fossil", 
        Description = "A beautifully preserved spiral shell from the Jurassic sea.", 
        Type = ResourceType.Bone, Rarity = Rarity.Uncommon, 
        BaseSellPrice = 35, BaseMuseumIncome = 3, 
        VariantCount = 2, CanExhibitAlone = true 
    };
    
    _resources["trilobite"] = new FossilDefinition 
    { 
        Id = "trilobite", DisplayName = "Trilobite Fossil", 
        Description = "An ancient marine arthropod from the Paleozoic era.", 
        Type = ResourceType.Bone, Rarity = Rarity.Rare, 
        BaseSellPrice = 50, BaseMuseumIncome = 5, 
        VariantCount = 2, CanExhibitAlone = true 
    };
    
    _resources["amber"] = new FossilDefinition 
    { 
        Id = "amber", DisplayName = "Amber with Inclusion", 
        Description = "Fossilized tree resin with a trapped ancient insect.", 
        Type = ResourceType.Amber, Rarity = Rarity.Rare, 
        BaseSellPrice = 80, BaseMuseumIncome = 6, 
        VariantCount = 2, CanExhibitAlone = true 
    };
}

    // ===== РЕГИСТРАЦИЯ МИНЕРАЛОВ =====
    private static void RegisterMinerals()
    {
        _resources["gold_nugget"] = new MineralDefinition { Id = "gold_nugget", DisplayName = "Gold Nugget", Description = "A shiny piece of gold.", Type = ResourceType.Gold, Rarity = Rarity.Common, BaseSellPrice = 25, BaseMuseumIncome = 0, MinDropAmount = 1, MaxDropAmount = 3 };
        _resources["precious_gem"] = new MineralDefinition { Id = "precious_gem", DisplayName = "Precious Gem", Description = "A sparkling gemstone.", Type = ResourceType.Gem, Rarity = Rarity.Uncommon, BaseSellPrice = 60, BaseMuseumIncome = 0, MinDropAmount = 1, MaxDropAmount = 1 };
    }

    private static void RegisterConcretions()
{
    // Конкреция с обычными находками
    _resources["common_concretion"] = new ConcretionDefinition
    {
        Id = "common_concretion", 
        DisplayName = "Stone Concretion", 
        Description = "A round stone that might contain something interesting.",
        Type = ResourceType.Concretion, 
        Rarity = Rarity.Common, 
        BaseSellPrice = 10, 
        BaseMuseumIncome = 0,
        HitsToOpen = 5,
        PossibleContents = new List<LootEntry>()
        {
            new LootEntry { Resource = GetResource("ammonite"), DropChance = 0.5f },
            new LootEntry { Resource = GetResource("dino_tooth"), DropChance = 0.2f }
        }
    };
    
    // Конкреция с редкими находками
    _resources["rare_concretion"] = new ConcretionDefinition
    {
        Id = "rare_concretion", 
        DisplayName = "Precious Concretion", 
        Description = "A strange stone that feels unusually heavy.",
        Type = ResourceType.Concretion, 
        Rarity = Rarity.Rare, 
        BaseSellPrice = 30, 
        BaseMuseumIncome = 0,
        HitsToOpen = 8,
        PossibleContents = new List<LootEntry>()
        {
            new LootEntry { Resource = GetResource("trilobite"), DropChance = 0.4f }
        }
    };
}

    // ===== РЕГИСТРАЦИЯ ЛОКАЦИЙ =====
    private static void RegisterLocations()
    {
        _locations["canada"] = new LocationDefinition
        {
            Id = "canada", DisplayName = "Canada (Alberta)", Description = "Dinosaur Provincial Park. Land of horned giants.",
            UnlockCost = 0, RequiredPlayerLevel = 1, GridWidth = 3, GridHeight = 4, BaseTileHp = 3, TileHpGrowthPerRow = 1.15f,
            LootTable = new List<LootEntry>()
            {
                CreateLoot("triceratops_skull", 0.3f), CreateLoot("triceratops_body", 0.3f), CreateLoot("triceratops_tail", 0.3f),
                CreateLoot("dilophosaurus_skull", 0.3f), CreateLoot("dilophosaurus_body", 0.3f), CreateLoot("dilophosaurus_tail", 0.3f),
                 CreateLoot("gold_nugget", 0.12f), CreateLoot("precious_gem", 0.04f),
                // Конкреции
            CreateLoot("common_concretion", 0.15f),
            CreateLoot("rare_concretion", 0.05f),
            
            // Янтарь
            CreateLoot("amber", 0.03f)
            }
        };

        _locations["gobi"] = new LocationDefinition
        {
            Id = "gobi", DisplayName = "Gobi Desert", Description = "Mongolia/China. The graveyard of dinosaurs.",
            UnlockCost = 1500, RequiredPlayerLevel = 3, GridWidth = 4, GridHeight = 4, BaseTileHp = 5, TileHpGrowthPerRow = 1.20f,
            LootTable = new List<LootEntry>()
            {
                CreateLoot("protoceratops_skull", 0.025f), CreateLoot("protoceratops_body", 0.025f), CreateLoot("protoceratops_tail", 0.025f),
                CreateLoot("velociraptor_skull", 0.02f), CreateLoot("velociraptor_body", 0.02f), CreateLoot("velociraptor_tail", 0.02f),
                CreateLoot("therizinosaurus_skull", 0.015f), CreateLoot("therizinosaurus_body", 0.015f), CreateLoot("therizinosaurus_tail", 0.015f),
                CreateLoot("dino_egg", 0.06f), CreateLoot("gold_nugget", 0.12f), CreateLoot("precious_gem", 0.05f),
                // Конкреции
            CreateLoot("common_concretion", 0.95f),
            CreateLoot("rare_concretion", 0.95f),
            
            // Янтарь
            CreateLoot("amber", 0.03f)
            }
        };

        _locations["undoria"] = new LocationDefinition
        {
            Id = "undoria", DisplayName = "Undoria Geopark", Description = "Ulyanovsk, Russia. Jurassic sea fossils on the Volga.",
            UnlockCost = 1500, RequiredPlayerLevel = 3, GridWidth = 4, GridHeight = 4, BaseTileHp = 5, TileHpGrowthPerRow = 1.20f,
            LootTable = new List<LootEntry>()
            {
                CreateLoot("ichthyosaurus_skull", 0.025f), CreateLoot("ichthyosaurus_body", 0.025f), CreateLoot("ichthyosaurus_tail", 0.025f),
                CreateLoot("plesiosaurus_skull", 0.02f), CreateLoot("plesiosaurus_body", 0.02f), CreateLoot("plesiosaurus_tail", 0.02f),
                CreateLoot("gold_nugget", 0.12f), CreateLoot("precious_gem", 0.05f),
                // Конкреции
            CreateLoot("common_concretion", 0.15f),
            CreateLoot("rare_concretion", 0.05f),
            
            // Янтарь
            CreateLoot("amber", 0.03f)
            }
        };

        _locations["texas"] = new LocationDefinition
{
    Id = "texas", 
    DisplayName = "Texas Red Beds", 
    Description = "Seymour, Texas. Permian period fossils in the ancient red clay deposits.",
    UnlockCost = 2500, 
    RequiredPlayerLevel = 5, 
    GridWidth = 6, 
    GridHeight = 6, 
    BaseTileHp = 8, 
    TileHpGrowthPerRow = 1.35f,
    Rarity = Rarity.Legendary,
    LootTable = new List<LootEntry>()
    {
        // Dimetrodon collection (rare)
        CreateLoot("dimetrodon_skull", 0.03f), 
        CreateLoot("dimetrodon_body", 0.03f), 
        CreateLoot("dimetrodon_tail", 0.03f),
        
        // Resources
        CreateLoot("gold_nugget", 0.15f), 
        CreateLoot("precious_gem", 0.06f),
        // Конкреции
            CreateLoot("common_concretion", 0.15f),
            CreateLoot("rare_concretion", 0.05f),
            
            // Янтарь
            CreateLoot("amber", 0.03f)
    }
};

    _locations["morrison"] = new LocationDefinition
{
    Id = "morrison", 
    DisplayName = "Morrison Formation", 
    Description = "Colorado & Utah, USA. Late Jurassic period. One of the richest dinosaur fossil sites in the world.",
    UnlockCost = 5000, 
    RequiredPlayerLevel = 8, 
    GridWidth = 5, 
    GridHeight = 5, 
    BaseTileHp = 4, 
    TileHpGrowthPerRow = 1.50f,
    LootTable = new List<LootEntry>()
    {
        // Stegosaurus collection (rare)
        CreateLoot("stegosaurus_skull", 0.02f), 
        CreateLoot("stegosaurus_body", 0.02f), 
        CreateLoot("stegosaurus_tail", 0.02f),
        
        // Allosaurus collection (rare) - уже есть у вас
        CreateLoot("allosaurus_skull", 0.025f), 
        CreateLoot("allosaurus_body", 0.025f), 
        CreateLoot("allosaurus_tail", 0.025f),
        
        
        
        // Premium resources
        CreateLoot("gold_nugget", 0.15f), 
        CreateLoot("precious_gem", 0.08f),
        CreateLoot("dino_egg", 0.05f)
    }
};
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

        private static void RegisterTickets()
    {
        _tickets["ticket_canada"] = new TicketDefinition
        {
            Id = "ticket_canada",
            DisplayName = "Билет: Canada (Alberta)",
            Description = "Стандартный доступ к Dinosaur Provincial Park.",
            TargetLocationId = "canada",
            ShopPrice = 500,
            IsSellable = false
        };

        _tickets["ticket_morrison"] = new TicketDefinition
        {
            Id = "ticket_morrison",
            DisplayName = "Билет: Morrison Formation",
            Description = "Доступ к богатым залежам поздней Юры.",
            TargetLocationId = "morrison",
            ShopPrice = 1000,
            IsSellable = false
        };

        _tickets["ticket_gobi"] = new TicketDefinition
        {
            Id = "ticket_gobi",
            DisplayName = "Билет: Gobi Desert",
            Description = "Экспедиция на кладбище динозавров.",
            TargetLocationId = "gobi",
            ShopPrice = 2000,
            IsSellable = false
        };

        _tickets["ticket_undoria"] = new TicketDefinition
        {
            Id = "ticket_undoria",
            DisplayName = "Билет: Undoria Geopark",
            Description = "Исследование юрских морских окаменелостей.",
            TargetLocationId = "undoria",
            ShopPrice = 5000,
            IsSellable = false
        };

        _tickets["ticket_texas"] = new TicketDefinition
        {
            Id = "ticket_texas",
            DisplayName = "Билет: Texas Red Beds",
            Description = "ЭКСКЛЮЗИВ: Доступ к редким находкам Перми. Не продается.",
            TargetLocationId = "texas",
            ShopPrice = 0, // Не продается
            IsSellable = false
        };
    }

        /// <summary>
    /// Регистрирует билеты как ResourceDefinition, чтобы инвентарь мог с ними работать.
    /// Это дублирование данных, но необходимо для совместимости с InventorySystem.
    /// </summary>
    private static void RegisterTicketsAsResources()
    {
        foreach (var ticket in _tickets.Values)
        {
            // Создаем ResourceDefinition на основе TicketDefinition
            var resource = new ResourceDefinition
            {
                Id = ticket.Id,
                DisplayName = ticket.DisplayName,
                Description = ticket.Description,
                BaseSellPrice = 0, // Билеты не продаются
                Rarity = Rarity.Common, // Или можно сделать Rarity.Legendary для Texas
            };
            
            _resources[ticket.Id] = resource;
        }
        
        GD.Print($"[GameData] ✅ Зарегистрировано {_tickets.Count} билетов как ресурсов для инвентаря");
    }
}