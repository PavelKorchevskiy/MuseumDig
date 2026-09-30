using Godot;
using System;

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
    
    // Лут
    public string HiddenResourceId { get; set; } = "";
    public int HiddenAmount { get; set; } = 0;
    public bool IsFindingExtractable { get; set; } = false;

    // Визуальные элементы
    private Sprite2D _dirtSprite;
    private AnimatedSprite2D _diggerSprite;
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

        // Анимация археолога
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

        _findingSprite = new Sprite2D();
        _findingSprite.Position = new Vector2(64, 64);
        _findingSprite.ZIndex = 1;
        _findingSprite.Visible = false;
        AddChild(_findingSprite);

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

    

    public void Initialize(int x, int y, float baseHp, float hpGrowth, string locationId)
    {
        GridPosition = new Vector2I(x, y);
        MaxHP = (float)Math.Round(baseHp * Math.Pow(hpGrowth, y));
        CurrentHP = MaxHP;
        ExtractionProgress = 0;

        // Ролл лута сразу (Вариант А)
        RollLoot(locationId);
        
        UpdatePosition();
        UpdateVisuals();
    }

        private void RollLoot(string locationId)
    {
        
        var location = GameData.GetLocation(locationId);
        if (location == null) return;

        foreach (var entry in location.LootTable)
        {
        
            if (GD.Randf() < entry.DropChance)
            {
                HiddenResourceId = entry.Resource.Id;
                
                // Если это минерал, можно добавить логику Min/Max DropAmount
                if (entry.Resource is MineralDefinition mineral)
                {
                    HiddenAmount = GD.RandRange(mineral.MinDropAmount, mineral.MaxDropAmount);
                }
                else
                {
                    HiddenAmount = 1;
                }
                return;
            }
        }
    }

    // ===== ЛОГИКА СОСТОЯНИЙ =====

    public void ApplyDiggingDamage(float damage)
    {
        if (Action != TileAction.Digging) return;
        if (Stage == TileStage.FullyDug || Stage == TileStage.HasFinding_Dug1 || Stage == TileStage.HasFinding_Dug2) return;

        CurrentHP -= damage;
        
        float threshold1 = MaxHP * 0.66f;
        float threshold2 = MaxHP * 0.33f;

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Action = TileAction.None;
            
            if (string.IsNullOrEmpty(HiddenResourceId))
            {
                Stage = TileStage.FullyDug;
            }
            else
            {
                var resource = GameData.GetResource(HiddenResourceId);
                
                if (resource is ConcretionDefinition)
                {
                    // Конкреция сразу попадает в инвентарь
                    InventorySystem.Instance.AddItem(HiddenResourceId, HiddenAmount);
                    GD.Print($"[Tile] 📦 Конкреция найдена и добавлена в инвентарь: {HiddenResourceId}");
                    Stage = TileStage.FullyDug;
                    HiddenResourceId = "";
                    HiddenAmount = 0;
                }
                else if (resource is FossilDefinition fossil && fossil.Type == ResourceType.Bone)
                {
                    // Кость требует извлекателя
                    IsFindingExtractable = false;
                    Stage = (Stage == TileStage.Dug1) ? TileStage.HasFinding_Dug1 : TileStage.HasFinding_Dug2;
                    GD.Print($"[Tile] 🦴 Найдена кость (требует извлекателя): {HiddenResourceId}");
                }
                else
                {
                    // Не-кость можно забрать кликом
                    IsFindingExtractable = true;
                    Stage = (Stage == TileStage.Dug1) ? TileStage.HasFinding_Dug1 : TileStage.HasFinding_Dug2;
                    GD.Print($"[Tile] 💎 Найдена находка (можно забрать кликом): {HiddenResourceId}");
                }
            }
        }
        else if (CurrentHP <= threshold2 && Stage == TileStage.Intact)
        {
            Stage = TileStage.Dug2;
        }
        else if (CurrentHP <= threshold1 && Stage == TileStage.Intact)
        {
            Stage = TileStage.Dug1;
        }

        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
    }

    public void ApplyExtractionProgress(float progress)
    {
        if (Action != TileAction.Extracting) return;
        if (Stage != TileStage.HasFinding_Dug1 && Stage != TileStage.HasFinding_Dug2) return;

        ExtractionProgress += progress;

        if (ExtractionProgress >= MaxHP)
        {
            ExtractionProgress = MaxHP;
            Action = TileAction.None;
            
            // Выдача лута
            if (!string.IsNullOrEmpty(HiddenResourceId))
            {
                InventorySystem.Instance.AddItem(HiddenResourceId, HiddenAmount);
                GD.Print($"[Tile] Извлечено: {HiddenResourceId} x{HiddenAmount}");
            }
            
            Stage = TileStage.FullyDug;
            HiddenResourceId = "";
            HiddenAmount = 0;
        }

        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
    }

    public bool TryUseExcavator()
    {
        if (Stage != TileStage.Intact) return false;
        
        // Стоимость экскаватора (можно вынести в GameData)
        int cost = 100; 
        if (!Wallet.Instance.SpendCoins(cost))
        {
            GD.Print("[Tile] Недостаточно монет для экскаватора!");
            return false;
        }

        // Мгновенный переход в Dug1
        CurrentHP = MaxHP * 0.66f;
        Stage = TileStage.Dug1;
        
        // Если там был лут, он сразу становится виден
        if (!string.IsNullOrEmpty(HiddenResourceId))
        {
            Stage = TileStage.HasFinding_Dug1;
        }

        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

        /// <summary>
    /// Собирает находку, которая не требует извлекателя (зубы, яйца, янтарь и т.д.)
    /// </summary>
    public void CollectFinding()
    {
        if (!IsFindingExtractable) return;
        if (Stage != TileStage.HasFinding_Dug1 && Stage != TileStage.HasFinding_Dug2) return;
        if (string.IsNullOrEmpty(HiddenResourceId)) return;

        // Добавляем предмет в инвентарь
        InventorySystem.Instance.AddItem(HiddenResourceId, HiddenAmount);
        GD.Print($"[Tile] ✅ Собрана находка: {HiddenResourceId} x{HiddenAmount}");

        // Очищаем тайл
        HiddenResourceId = "";
        HiddenAmount = 0;
        IsFindingExtractable = false;
        Stage = TileStage.FullyDug;
        
        UpdateVisuals();
        SaveSystem.Instance?.MarkDirty();
    }

    // ===== ВИЗУАЛ =====

        public void UpdateVisuals()
    {
        // === Статичный спрайт земли ===
        string texturePath = GetTextureForStage();
        if (!string.IsNullOrEmpty(texturePath) && ResourceLoader.Exists(texturePath))
        {
            _dirtSprite.Texture = GD.Load<Texture2D>(texturePath);
        }

        // === Анимация археолога ===
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
                _progressBar.Value = ExtractionProgress * 100f;
            }
        }
        else
        {
            _progressBar.Visible = false;
        }

        // Находка
        if (Stage == TileStage.HasFinding_Dug1 || Stage == TileStage.HasFinding_Dug2)
        {
            if (!string.IsNullOrEmpty(HiddenResourceId))
            {
                string findingPath = GetResourceIconPath(HiddenResourceId);
                if (ResourceLoader.Exists(findingPath))
                {
                    _findingSprite.Texture = GD.Load<Texture2D>(findingPath);
                    _findingSprite.Visible = true;
                    
                    if (_findingSprite.Texture.GetWidth() > 64)
                    {
                        float scale = 64f / _findingSprite.Texture.GetWidth();
                        _findingSprite.Scale = new Vector2(scale, scale);
                    }
                }
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

    private void LoadDiggerAnimation()
    {
        var spriteFrames = _diggerSprite.SpriteFrames;
        
        if (spriteFrames.GetFrameCount("digging") >= 8)
        {
            return; // Кадры уже загружены
        }

        spriteFrames.Clear("digging");

        for (int i = 1; i <= 8; i++)
        {
            string framePath = $"res://assets/dig/digger/1_{i}.png";
            if (ResourceLoader.Exists(framePath))
            {
                var texture = GD.Load<Texture2D>(framePath);
                spriteFrames.AddFrame("digging", texture);
            }
        }
    }

    private string GetTextureForStage()
    {
        return Stage switch
        {
            TileStage.Intact => "res://assets/tiles/dirt_intact.png",
            TileStage.Dug1 => "res://assets/tiles/dirt_dug1.png",
            TileStage.Dug2 => "res://assets/tiles/dirt_dug2.png",
            TileStage.FullyDug => "res://assets/tiles/dirt_empty.png",
            TileStage.HasFinding_Dug1 => "res://assets/tiles/dirt_dug1.png",
            TileStage.HasFinding_Dug2 => "res://assets/tiles/dirt_dug2.png",
            _ => "res://assets/tiles/dirt_intact.png"
        };
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

private void UpdatePosition()
    {
        // Стандартная изометрическая формула
        float isoX = (GridPosition.X - GridPosition.Y) * (TileWidth / 2f);
        float isoY = (GridPosition.X + GridPosition.Y) * (TileHeight / 2f);

        // Так как файл 128x128, а ромб занимает только часть высоты,
        // корректируем позицию панели, чтобы ромб встал ровно в сетку
        float verticalOffset = (128f - TileHeight) / 2f;
        
        Position = new Vector2(isoX, isoY - verticalOffset);
        
        // Z-порядок для правильной отрисовки (нижние ряды поверх верхних)
        ZIndex = GridPosition.X + GridPosition.Y;
    }
        // ===== ПОДСВЕТКА (вызывается из DigInputController) =====

        public void Highlight()
    {
        if (CanApplyCurrentTool())
        {
            Modulate = new Color(0.7f, 1.0f, 0.7f, 1.0f); // Зелёная
        }
        else if (IsFindingExtractable && (Stage == TileStage.HasFinding_Dug1 || Stage == TileStage.HasFinding_Dug2))
        {
            // Специальная подсветка для извлекаемых находок (жёлтая)
            Modulate = new Color(1.0f, 1.0f, 0.5f, 1.0f);
        }
        else
        {
            Modulate = new Color(1.0f, 0.5f, 0.5f, 0.8f); // Красная
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
                return (Stage == TileStage.Intact || Stage == TileStage.Dug1 || Stage == TileStage.Dug2) 
                    && Action != TileAction.Digging;
                    
            case WorkerType.Extractor:
                // Извлекатель работает только с костями (не с извлекаемыми находками)
                return (Stage == TileStage.HasFinding_Dug1 || Stage == TileStage.HasFinding_Dug2) 
                    && !IsFindingExtractable
                    && Action != TileAction.Extracting;
                    
            case WorkerType.Excavator:
                return Stage == TileStage.Intact && Action == TileAction.None;
                
            default:
                return false;
        }
    }

    // ===== ПРИМЕНЕНИЕ ИНСТРУМЕНТА (вызывается из DigInputController) =====

    public void ApplySelectedTool()
    {
        var selectedTool = DiggingUI.Instance?.SelectedTool;
        if (selectedTool == null) return;

        GD.Print($"[Tile] Применяем инструмент {selectedTool} к плитке ({GridPosition.X},{GridPosition.Y}), стадия: {Stage}");

        if (selectedTool == WorkerType.Digger)
        {
            if (DiggingManager.Instance.GetActiveDiggersCount() >= DiggingManager.Instance.GetMaxDiggers())
            {
                GD.Print("[Tile] ⚠️ Достигнут лимит копателей!");
                return;
            }

            if (Stage == TileStage.Intact || Stage == TileStage.Dug1 || Stage == TileStage.Dug2)
            {
                Action = TileAction.Digging;
                UpdateVisuals();
                DiggingUI.Instance.ClearSelection();
            }
            else
            {
                GD.Print("[Tile] ⚠️ Эту плитку нельзя копать");
            }
        }
        else if (selectedTool == WorkerType.Extractor)
        {
            if (DiggingManager.Instance.GetActiveExtractorsCount() >= DiggingManager.Instance.GetMaxExtractors())
            {
                GD.Print("[Tile] ⚠️ Достигнут лимит извлекателей!");
                return;
            }

            if (Stage == TileStage.HasFinding_Dug1 || Stage == TileStage.HasFinding_Dug2)
            {
                Action = TileAction.Extracting;
                UpdateVisuals();
                DiggingUI.Instance.ClearSelection();
            }
            else
            {
                GD.Print("[Tile] ⚠️ Здесь нет находки для извлечения");
            }
        }
        else if (selectedTool == WorkerType.Excavator)
        {
            if (TryUseExcavator())
            {
                DiggingUI.Instance.ClearSelection();
            }
        }
    }

        /// <summary>
    /// Проверяет, находится ли точка внутри видимого ромба тайла.
    /// </summary>
    public bool IsPointInsideShape(Vector2 point)
    {
        // Конвертируем в локальные координаты тайла
        Vector2 localPoint = point - Position;
        
        // Центр ромба в локальных координатах (128x128)
        Vector2 center = new Vector2(64, 64);
        
        // Полуразмеры видимого ромба
        float halfWidth = 64f;
        float halfHeight = 32f; // Для 2:1 изометрии. Для 30° используйте 37f
        
        float dx = Mathf.Abs(localPoint.X - center.X);
        float dy = Mathf.Abs(localPoint.Y - center.Y);
        
        // Формула ромба
        return (dx / halfWidth) + (dy / halfHeight) <= 1.0f;
    }
}