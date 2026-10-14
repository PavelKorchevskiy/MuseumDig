using Godot;
using System;
using System.Threading.Tasks;

public partial class Tile : Panel
{
    public Vector2I GridPosition { get; set; }
    
    // Состояние
    public TileStage Stage { get; set; } = TileStage.Intact;
    public TileAction Action { get; set; } = TileAction.None;
    
    // HP и прогресс
    public float MaxHP { get; set; }
    public float CurrentHP { get; set; }
    public float ExtractionProgress { get; set; }
    
    public TileLoot _loot;

    private int _variantIntact = 1;
    private int _variantDug1 = 1;
    private int _variantDug2 = 1;
    private int _variantEmpty = 1;
    private const int IntactVariantCount = 8;
    private const int Dug1VariantCount = 2;
    private const int Dug2VariantCount = 2;

    private const int EmptyVariantCount = 2;

    // Визуальные элементы
    private Sprite2D _dirtSprite;
    private AnimatedSprite2D _diggerSprite;
    private AnimatedSprite2D _extractorSprite;

        private AnimatedSprite2D _bulldozerSprite;
    private Sprite2D _findingSprite;
    private ProgressBar _progressBar;
    private Label _statusLabel;

    private const float TileWidth = 128f;
    private const float TileHeight = 74f;

    public override void _Ready()
    {
        var emptyStyle = new StyleBoxEmpty();
        AddThemeStyleboxOverride("panel", emptyStyle);

        CustomMinimumSize = new Vector2(128, 128);
        Size = new Vector2(128, 128);
        MouseFilter = MouseFilterEnum.Ignore;
        
        _dirtSprite = new Sprite2D();
        _dirtSprite.Position = new Vector2(64, 64);
        AddChild(_dirtSprite);

        _diggerSprite = new AnimatedSprite2D();
        _diggerSprite.Position = new Vector2(64, 64);
        _diggerSprite.ZIndex = 2;
        
        var diggerFrames = new SpriteFrames();
        diggerFrames.ClearAll();
        diggerFrames.AddAnimation("digging");
        diggerFrames.SetAnimationSpeed("digging", 8);
        diggerFrames.SetAnimationLoop("digging", true);
        
        _diggerSprite.SpriteFrames = diggerFrames;
        AddChild(_diggerSprite);
        _diggerSprite.Visible = false;

        _extractorSprite = new AnimatedSprite2D();
        _extractorSprite.Position = new Vector2(64, 64);
        _extractorSprite.ZIndex = 3;
        
        var extractorFrames = new SpriteFrames();
        extractorFrames.ClearAll();
        extractorFrames.AddAnimation("extracting");
        extractorFrames.SetAnimationSpeed("extracting", 8);
        extractorFrames.SetAnimationLoop("extracting", true);
        
        _extractorSprite.SpriteFrames = extractorFrames;
        AddChild(_extractorSprite);
        _extractorSprite.Visible = false;

        _findingSprite = new Sprite2D();
        _findingSprite.Position = new Vector2(64, 64);
        _findingSprite.ZIndex = 1;
        _findingSprite.Visible = false;
        AddChild(_findingSprite);

         _bulldozerSprite = new AnimatedSprite2D();
        _bulldozerSprite.ZIndex = 4; // Поверх всех остальных
        _bulldozerSprite.Visible = false;
        
        var bulldozerFrames = new SpriteFrames();
        bulldozerFrames.ClearAll();
        bulldozerFrames.AddAnimation("moving");
        bulldozerFrames.SetAnimationSpeed("moving", 12); // 12 FPS
        bulldozerFrames.SetAnimationLoop("moving", false); // Один прогон, не в цикле
        
        _bulldozerSprite.SpriteFrames = bulldozerFrames;
        AddChild(_bulldozerSprite);

        _progressBar = new ProgressBar();
        _progressBar.Size = new Vector2(100, 12);
        _progressBar.Position = new Vector2(14, 100);
        _progressBar.MaxValue = 100;
        _progressBar.Visible = false;
        AddChild(_progressBar);

        _statusLabel = new Label();
        _statusLabel.Size = new Vector2(128, 20);
        _statusLabel.Position = new Vector2(0, 0);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.Visible = false;
        AddChild(_statusLabel);

        UpdateVisuals();
    }

    // ===== ИНИЦИАЛИЗАЦИЯ =====

    public void Initialize(int x, int y, float baseHp, float hpGrowth, string locationId, bool rollLoot = true)
    {
        GridPosition = new Vector2I(x, y);
        MaxHP = (float)Math.Round(baseHp * Math.Pow(hpGrowth, y));
        CurrentHP = MaxHP;
        ExtractionProgress = 0;

        DiggingManager.Instance.RegisterTile(this);

        GenerateVariants();
        if (rollLoot)
        {
            RollLoot(locationId);
        }
        
        UpdatePosition();
        UpdateVisuals();
    }

    public override void _ExitTree()
    {
        DiggingManager.Instance.UnregisterTile(this);
    }

      /// Кость, которую нужно извлечь. Видна на Dug1 и Dug2.
    public bool HasExtractableFinding =>
        _loot != null &&
        _loot.IsBone &&
        !_loot.Collected &&
        Stage >= _loot.RevealedAtStage &&
        Stage != TileStage.FullyDug;
    
    /// Находка, которую можно собрать кликом. Видна на Dug1, Dug2, FullyDug.
    public bool HasCollectableFinding =>
        _loot != null &&
        !_loot.IsBone &&
        !_loot.Collected &&
        Stage >= _loot.RevealedAtStage;

    private void RollLoot(string locationId)
    {
        var location = GameData.GetLocation(locationId);
        if (location == null) return;

        foreach (var entry in location.LootTable)
        {
            if (GD.Randf() < entry.DropChance)
            {
                string resourceId = entry.Resource.Id;
                int amount = 1;
                
                if (entry.Resource is MineralDefinition mineral)
                {
                    amount = GD.RandRange(mineral.MinDropAmount, mineral.MaxDropAmount);
                }
                
                // Определяем, является ли это костью
                bool isBone = entry.Resource is FossilDefinition fossil 
                              && fossil.Type == ResourceType.Bone;
                
                // Определяем стадию обнаружения
                TileStage revealedAt;
                if (isBone)
                {
                    // Кости видны только на Dug1 или Dug2
                    revealedAt = GD.Randf() < 0.5f ? TileStage.Dug1 : TileStage.Dug2;
                }
                else
                {
                    // Находки могут быть на любой стадии
                    float roll = GD.Randf();
                    if (roll < 0.33f)
                        revealedAt = TileStage.Dug1;
                    else if (roll < 0.66f)
                        revealedAt = TileStage.Dug2;
                    else
                        revealedAt = TileStage.FullyDug;
                }
                
                _loot = new TileLoot
                {
                    ResourceId = resourceId,
                    Amount = amount,
                    IsBone = isBone,
                    RevealedAtStage = revealedAt,
                    Collected = false
                };
                
                return;
            }
        }
    }

    // ===== ЛОГИКА КОПАНИЯ =====

    public void ApplyDiggingDamage(float damage)
    {
        if (Action != TileAction.Digging) return;
        if (Stage == TileStage.FullyDug) return;
        
        // Нельзя копать, если есть кость — нужно извлечь
        if (HasExtractableFinding) return;

        CurrentHP -= damage;

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Action = TileAction.None;
            AdvanceStage();
        }

        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
    }

    /// Переход на следующую стадию раскопок
    private void AdvanceStage()
    {
        switch (Stage)
        {
            case TileStage.Intact:
                Stage = TileStage.Dug1;
                break;
            case TileStage.Dug1:
                Stage = TileStage.Dug2;
                break;
            case TileStage.Dug2:
                Stage = TileStage.FullyDug;
                break;
        }
        
        // Сбрасываем HP для новой стадии
        CurrentHP = MaxHP;
        MaxHP *= 1.15f;
        
        // Логируем обнаружение лута
        if (HasExtractableFinding)
        {
            GD.Print($"[Tile] 🦴 Обнаружена кость: {_loot.ResourceId} (стадия {Stage})");
        }
        else if (HasCollectableFinding)
        {
            GD.Print($"[Tile] 💎 Обнаружена находка: {_loot.ResourceId} (стадия {Stage})");
        }
    }

    // ===== ЛОГИКА ИЗВЛЕЧЕНИЯ (кости) =====

    public void ApplyExtractionProgress(float progress)
    {
        if (Action != TileAction.Extracting) return;
        if (!HasExtractableFinding) return;

        ExtractionProgress += progress;

        if (ExtractionProgress >= MaxHP)
        {
            ExtractionProgress = 0;
            Action = TileAction.None;
            
            if (_loot != null)
            {
                InventorySystem.Instance.AddItem(_loot.ResourceId, _loot.Amount);
                GD.Print($"[Tile] 🦴 Извлечено: {_loot.ResourceId} x{_loot.Amount}");
                _loot.Collected = true;
            }
            
            // После извлечения кости → FullyDug
            Stage = TileStage.FullyDug;

            var digSite = GetParent() as DigSite;
            digSite?.RefreshLootSummary();
            
            UpdateVisuals();
            SaveSystem.Instance?.MarkDirty();
        }
    }

    // ===== СБОР НАХОДОК (клик) =====

    public void CollectFinding()
    {
        if (!HasCollectableFinding) return;
        if (_loot == null) return;

        InventorySystem.Instance.AddItem(_loot.ResourceId, _loot.Amount);
        GD.Print($"[Tile] ✅ Собрана находка: {_loot.ResourceId} x{_loot.Amount}");

        _loot.Collected = true;
        Action = TileAction.None;
        
        // Тайл остаётся в текущей стадии
        
        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
    }

    // ===== ЭКСКАВАТОР =====

        // === ИЗМЕНЕНО: async void -> async Task<bool> ===
    public async Task<bool> TryUseExcavator()
    {
        if (Stage != TileStage.Intact) return false;
        if (Action != TileAction.None) return false;

        int cost = 100;
        if (!Wallet.Instance.SpendCoins(cost))
        {
            GD.Print("[Tile] Недостаточно монет для экскаватора!");
            return false; // Возвращаем false при неудаче
        }

        // === Запускаем анимацию экскаватора ===
        LoadBulldozerAnimation();
        
        float startX = 108f;  // 128 - 20 (отступ справа)
        float endX = 20f;     // 0 + 20 (отступ слева)
        float centerY = 34f;  // 64 - 30 (поднято выше)
                
        _bulldozerSprite.Position = new Vector2(startX, centerY);
        _bulldozerSprite.Visible = true;
        _bulldozerSprite.Play("moving");
        
        var tween = CreateTween();
        tween.TweenProperty(_bulldozerSprite, "position:x", endX, 1.0f); 
        
        await ToSignal(tween, "finished");
        
        _bulldozerSprite.Visible = false;
        _bulldozerSprite.Stop();
        
        // === Меняем стадию ===
        Stage = TileStage.Dug1;
        CurrentHP = MaxHP;
        MaxHP *= 1.15f;
        
        if (HasExtractableFinding)
        {
            GD.Print($"[Tile] 🚜 Экскаватор обнаружил кость: {_loot.ResourceId}");
        }
        else if (HasCollectableFinding)
        {
            GD.Print($"[Tile] 🚜 Экскаватор обнаружил находку: {_loot.ResourceId}");
        }
        else
        {
            GD.Print("[Tile] 🚜 Экскаватор снял верхний слой. Ничего не найдено.");
        }

        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
        
        return true; // Возвращаем true при успехе
    }

    // ===== ВИЗУАЛ =====

    public void UpdateVisuals()
    {
        // Статичный спрайт земли
        string texturePath = GetTextureForStage();
        if (!string.IsNullOrEmpty(texturePath) && ResourceLoader.Exists(texturePath))
        {
            _dirtSprite.Texture = GD.Load<Texture2D>(texturePath);
        } else
    {
        // === НОВОЕ: Если текстура не найдена, логируем и ставим дефолт ===
        GD.PrintErr($"[Tile] ⚠️ Текстура не найдена: '{texturePath}' для Stage={Stage}. Ставлю дефолт.");
        string fallback = Stage == TileStage.FullyDug 
            ? "res://assets/tiles/empty_1.png" 
            : "res://assets/tiles/intact_1.png";
        if (ResourceLoader.Exists(fallback))
        {
            _dirtSprite.Texture = GD.Load<Texture2D>(fallback);
        }
    }

        // Анимация копателя
        if (Action == TileAction.Digging)
        {
            LoadDiggerAnimation();
            _diggerSprite.Play("digging");
            _diggerSprite.Visible = true;
            
            float diggerY = Stage switch
            {
                TileStage.Intact => 20f,
                TileStage.Dug1 => 30f,
                TileStage.Dug2 => 45f,
                _ => 30f
            };
            _diggerSprite.Position = new Vector2(64, diggerY);
        }
        else
        {
            _diggerSprite.Stop();
            _diggerSprite.Visible = false;
        }

        // Анимация извлекателя
        if (Action == TileAction.Extracting)
        {
            LoadExtractorAnimation();
            _extractorSprite.Play("extracting");
            _extractorSprite.Visible = true;
            
            float extractorY = Stage switch
            {
                TileStage.Dug1 => 30f,
                TileStage.Dug2 => 45f,
                _ => 35f
            };
            _extractorSprite.Position = new Vector2(64, extractorY);
        }
        else
        {
            _extractorSprite.Stop();
            _extractorSprite.Visible = false;
        }

        // Прогресс-бар
        if (Action == TileAction.Digging || Action == TileAction.Extracting)
        {
            _progressBar.Visible = true;
            if (Action == TileAction.Digging)
            {
                _progressBar.Value = (1f - CurrentHP / MaxHP) * 100f;
            }
            else if (Action == TileAction.Extracting)
            {
                _progressBar.Value = (ExtractionProgress / MaxHP) * 100f;
            }
        }
        else
        {
            _progressBar.Visible = false;
        }

        // Находка (кость или собираемый предмет)
        if ((HasExtractableFinding || HasCollectableFinding) && _loot != null)
        {
            string findingPath = GetDigTextureForFinding(_loot.ResourceId);
            if (ResourceLoader.Exists(findingPath))
            {
                _findingSprite.Texture = GD.Load<Texture2D>(findingPath);
                _findingSprite.Visible = true;
                
                float targetSize = 64f / 1.5f;
                float raiseOffset = 10f;
                
                if (_findingSprite.Texture.GetWidth() > targetSize)
                {
                    float scale = targetSize / _findingSprite.Texture.GetWidth();
                    _findingSprite.Scale = new Vector2(scale, scale);
                }
                else
                {
                    _findingSprite.Scale = new Vector2(1f / 1.5f, 1f / 1.5f);
                }
                
                _findingSprite.Position = new Vector2(64, 64 - raiseOffset);
            }
        }
        else
        {
            _findingSprite.Visible = false;
        }

        // Статус
        if (Action == TileAction.Digging)
        {
            _statusLabel.Text = "⛏️";
            _statusLabel.Visible = true;
        }
        else if (Action == TileAction.Extracting)
        {
            _statusLabel.Text = "🔨";
            _statusLabel.Visible = true;
        }
        else
        {
            _statusLabel.Visible = false;
        }
    }

    private string GetTextureForStage()
    {
        string path = Stage switch
        {
            TileStage.Intact => $"res://assets/tiles/intact_{_variantIntact}.png",
            TileStage.Dug1 => $"res://assets/tiles/dug1_{_variantDug1}.png",
            TileStage.Dug2 => $"res://assets/tiles/dug2_{_variantDug2}.png",
            TileStage.FullyDug => "res://assets/tiles/empty.png",
            _ => $"res://assets/tiles/intact_{_variantIntact}.png"
        };
        
        // === ДИАГНОСТИКА ===
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"[Tile] ❌ Текстура не найдена: {path} (Stage={Stage}, variant={_variantIntact})");
        }
        
        return path;
    }

    private string GetDigTextureForFinding(string resourceId)
    {
        var resource = GameData.GetResource(resourceId);
        
        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            string partName = fossil.Id.Replace($"{fossil.CollectionId}_", "");
            
            string specificPath = $"res://assets/museum/items/{fossil.CollectionId}/dig/{partName}.png";
            if (ResourceLoader.Exists(specificPath)) return specificPath;
            
            string commonPath = $"res://assets/tiles/common/{partName}.png";
            if (ResourceLoader.Exists(commonPath)) return commonPath;
        }
        else
        {
            string commonPath = $"res://assets/tiles/common/{resourceId}.png";
            if (ResourceLoader.Exists(commonPath)) return commonPath;
        }
        
        return GetResourceIconPath(resourceId);
    }

    private string GetResourceIconPath(string resourceId)
    {
        var resource = GameData.GetResource(resourceId);
        
        if (resource is ConcretionDefinition)
        {
            string shortName = resourceId.Replace("_concretion", "");
            return $"res://assets/museum/items/common/concretion/{shortName}_1.png";
        }

        var collection = GameData.GetCollection(resourceId);
        if (collection != null) return collection.TexturePath;

        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            string partName = fossil.Id.Replace($"{fossil.CollectionId}_", "");
            return $"res://assets/museum/items/{fossil.CollectionId}/{partName}.png";
        }

        if (resource is FossilDefinition standaloneFossil && standaloneFossil.VariantCount > 1)
        {
            string variantPath = $"res://assets/museum/items/common/{resourceId}/1.png";
            if (ResourceLoader.Exists(variantPath)) return variantPath;
        }

        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath)) return commonPath;

        return "res://icon.svg";
    }

    private void LoadDiggerAnimation()
    {
        var spriteFrames = _diggerSprite.SpriteFrames;
        if (spriteFrames.GetFrameCount("digging") >= 8) return;

        spriteFrames.Clear("digging");
        for (int i = 1; i <= 8; i++)
        {
            string framePath = $"res://assets/dig/digger/1_{i}.png";
            if (ResourceLoader.Exists(framePath))
            {
                spriteFrames.AddFrame("digging", GD.Load<Texture2D>(framePath));
            }
        }
    }

    private void LoadExtractorAnimation()
    {
        var spriteFrames = _extractorSprite.SpriteFrames;
        if (spriteFrames.GetFrameCount("extracting") >= 8) return;

        spriteFrames.Clear("extracting");
        for (int i = 1; i <= 8; i++)
        {
            string framePath = $"res://assets/dig/extracter/1_{i}.png";
            if (ResourceLoader.Exists(framePath))
            {
                spriteFrames.AddFrame("extracting", GD.Load<Texture2D>(framePath));
            }
        }
    }

        private void LoadBulldozerAnimation()
    {
        var spriteFrames = _bulldozerSprite.SpriteFrames;
        
        if (spriteFrames.GetFrameCount("moving") >= 8)
        {
            return;
        }

        spriteFrames.Clear("moving");

        for (int i = 1; i <= 8; i++)
        {
            string framePath = $"res://assets/dig/bulldozer/1_{i}.png";
            if (ResourceLoader.Exists(framePath))
            {
                var texture = GD.Load<Texture2D>(framePath);
                spriteFrames.AddFrame("moving", texture);
            }
        }
    }

       private void UpdatePosition()
    {
        float isoX = (GridPosition.X - GridPosition.Y) * (TileWidth / 2f);
        float isoY = (GridPosition.X + GridPosition.Y) * (TileHeight / 2f);
        float verticalOffset = (128f - TileHeight) / 2f;
        
        Position = new Vector2(isoX, isoY - verticalOffset);
        ZIndex = GridPosition.X + GridPosition.Y;
        
        // === ОТЛАДКА: выводим позицию для сравнения ===
        if (GridPosition.X == 0 && GridPosition.Y == 0) // Первый тайл поля
        {
            GD.Print($"[Tile] Тайл ({GridPosition.X},{GridPosition.Y}): позиция = {Position}");
        }
    }

    // ===== ПОДСВЕТКА =====

    public void Highlight()
    {
        if (CanApplyCurrentTool())
        {
            Modulate = new Color(0.7f, 1.0f, 0.7f, 1.0f);
        }
        else if (HasCollectableFinding)
        {
            Modulate = new Color(1.0f, 1.0f, 0.5f, 1.0f);
        }
        else
        {
            Modulate = new Color(1.0f, 0.5f, 0.5f, 0.8f);
        }
    }

    public void Unhighlight()
    {
        Modulate = Colors.White;
    }

    private bool CanApplyCurrentTool()
    {
        var tool = DiggingUI.Instance?.SelectedTool;
        if (tool == null) return false;

        switch (tool)
        {
            case WorkerType.Digger:
                // Можно копать, если нет кости (кость блокирует копание)
                return (Stage == TileStage.Intact || Stage == TileStage.Dug1 || Stage == TileStage.Dug2) 
                    && Action != TileAction.Digging
                    && !HasExtractableFinding;
                    
            case WorkerType.Extractor:
                // Извлекатель работает только с костями
                return HasExtractableFinding 
                    && Action != TileAction.Extracting;
                    
            case WorkerType.Excavator:
                return Stage == TileStage.Intact && Action == TileAction.None;
                
            default:
                return false;
        }
    }

    // ===== ПРИМЕНЕНИЕ ИНСТРУМЕНТА =====

    public void ApplySelectedTool()
    {
        var selectedTool = DiggingUI.Instance?.SelectedTool;
        if (selectedTool == null) return;

        if (selectedTool == WorkerType.Digger)
        {
            if (DiggingManager.Instance.GetActiveDiggersCount() >= DiggingManager.Instance.GetMaxDiggers())
            {
                GD.Print("[Tile] ⚠️ Достигнут лимит копателей!");
                return;
            }

            if (Stage == TileStage.Intact || Stage == TileStage.Dug1 || Stage == TileStage.Dug2)
            {
                if (HasExtractableFinding)
                {
                    GD.Print("[Tile] ⚠️ Сначала извлеките кость!");
                    return;
                }
                
                Action = TileAction.Digging;
                UpdateVisuals();
                DiggingUI.Instance.ClearSelection();
            }
        }
        else if (selectedTool == WorkerType.Extractor)
        {
            if (DiggingManager.Instance.GetActiveExtractorsCount() >= DiggingManager.Instance.GetMaxExtractors())
            {
                GD.Print("[Tile] ⚠️ Достигнут лимит извлекателей!");
                return;
            }

            if (HasExtractableFinding)
            {
                Action = TileAction.Extracting;
                UpdateVisuals();
                DiggingUI.Instance.ClearSelection();
            }
            else
            {
                GD.Print("[Tile] ⚠️ Здесь нет кости для извлечения");
            }
        }
        else if (selectedTool == WorkerType.Excavator)
        {
            UseExcavatorAsync();
        }
    }

        private async void UseExcavatorAsync()
    {
        bool success = await TryUseExcavator();
        if (success)
        {
            DiggingUI.Instance?.ClearSelection();
        }
    }

    // ===== ГЕОМЕТРИЯ =====

    public bool IsPointInsideShape(Vector2 point)
    {
        Vector2 localPoint = point - Position;
        Vector2 center = new Vector2(64, 64);
        
        float halfWidth = 64f;
        float halfHeight = 32f;
        
        float dx = Mathf.Abs(localPoint.X - center.X);
        float dy = Mathf.Abs(localPoint.Y - center.Y);
        
        return (dx / halfWidth) + (dy / halfHeight) <= 1.0f;
    }

        /// <summary>
    /// Детерминированный хеш координат. Всегда возвращает одно и то же значение
    /// для одних и тех же (x, y, salt), что гарантирует стабильность вариантов
    /// при перезагрузке сохранения.
    /// </summary>
    private static int HashCoordinates(int x, int y, int salt)
    {
        // Хеш на основе простых чисел (классический приём из процедурной генерации)
        long hash = x * 73856093L ^ y * 19349663L ^ salt * 83492791L;
        
        // Берём младшие биты и делаем положительными
        return Mathf.Abs((int)(hash & 0x7FFFFFFF));
    }

    /// <summary>
    /// Вычисляет варианты текстур на основе позиции тайла.
    /// Вызывается один раз при инициализации или загрузке.
    /// </summary>
    private void GenerateVariants()
    {
        // === ДИАГНОСТИКА ===
        GD.Print($"[Tile] GenerateVariants: GridPosition = ({GridPosition.X}, {GridPosition.Y})");
        
        _variantIntact = (HashCoordinates(GridPosition.X, GridPosition.Y, 0) % IntactVariantCount) + 1;
        _variantDug1 = (HashCoordinates(GridPosition.X, GridPosition.Y, 1) % Dug1VariantCount) + 1;
        _variantDug2 = (HashCoordinates(GridPosition.X, GridPosition.Y, 2) % Dug2VariantCount) + 1;
        
        GD.Print($"[Tile] Варианты: intact={_variantIntact}, dug1={_variantDug1}, dug2={_variantDug2}");
    }

    public void RegenerateVariants()
    {
        GenerateVariants();
    }
}