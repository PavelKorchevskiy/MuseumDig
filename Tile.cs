using Godot;
using System.Collections.Generic;

public partial class Tile : ColorRect
{
	// ===== СОСТОЯНИЕ ТАЙЛА =====
	private TileState _state = TileState.Solid;

	private TextureRect _sprite;
    private Label _hpLabel;
	
	// HP блока (земли)
	private int _blockHp;
	private int _blockMaxHp;
	
	// Что внутри (null = ничего)
	private ResourceDefinition _hiddenResource;
	private int _hiddenAmount = 1;
	
	// Для отладки — глубина ряда (влияет на HP)
	[Export] public int RowIndex = 0;

	public Vector2I GridPosition { get; set; }
	
	// ===== ИНИЦИАЛИЗАЦИЯ =====
	
	public void Initialize(int row, float baseHp, float hpGrowth)
{
    RowIndex = row;
    _blockMaxHp = (int)(baseHp * Mathf.Pow(hpGrowth, row));
    _blockHp = _blockMaxHp;
    
    // Решаем, что внутри, по таблице лута
    _hiddenResource = RollLoot();
    
    // Инициализация визуальных элементов (только один раз)
    if (_sprite == null)
    {
        _sprite = new TextureRect();
        // ЗАМЕНИТЕ ПУТЬ на реальный путь к вашему сгенерированному тайлу!
        _sprite.Texture = GD.Load<Texture2D>("res://assets/tiles/dirt.png"); 
        _sprite.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
        _sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        _sprite.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(_sprite);
        
        _hpLabel = new Label();
        _hpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hpLabel.VerticalAlignment = VerticalAlignment.Center;
        _hpLabel.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
        _hpLabel.AddThemeFontSizeOverride("font_size", 14);
        _hpLabel.AddThemeColorOverride("font_color", Colors.White);
        _hpLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_hpLabel);
    }
    
    UpdateVisual();
}
	
	// ===== РОЛЛ ЛУТА =====
	
	private ResourceDefinition RollLoot()
{
	// Берём текущую локацию из LocationSystem
	var location = LocationSystem.Instance?.GetCurrentLocation();
	if (location == null)
	{
		GD.PrintErr("[Tile] No current location!");
		return null;
	}
	
	// Проходим по таблице лута
	foreach (var entry in location.LootTable)
	{
		if (GD.Randf() < entry.DropChance)
		{
			// Для минералов определяем количество
			if (entry.Resource is MineralDefinition mineral)
			{
				_hiddenAmount = GD.RandRange(mineral.MinDropAmount, mineral.MaxDropAmount);
			}
			else
			{
				_hiddenAmount = 1;
			}
			return entry.Resource;
		}
	}
	return null; // Ничего не выпало
}
	
	// ===== ОБРАБОТКА КЛИКА =====
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent 
			&& mouseEvent.Pressed 
			&& mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (GetRect().HasPoint(mouseEvent.Position))
			{
				HandleClick();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	private void HandleClick()
	{
		// Расход энергии
		if (!EnergySystem.Instance.TrySpendEnergy(1))
		{
			GD.Print("Not enough energy!");
			return;
		}
		
		switch (_state)
		{
			case TileState.Solid:
				DamageBlock();
				break;
				
			case TileState.Cracked:
				// Блок треснул, но ещё не разрушен — продолжаем ломать
				DamageBlock();
				break;
				
			case TileState.Exposed:
				// Находка видна — извлекаем или повреждаем
				ExtractOrDamage();
				break;
				
			case TileState.Extracted:
				// Пустой тайл — ничего не делаем
				break;
		}
	}
	
	// ===== ЛОМАЕМ БЛОК =====
	
		private void DamageBlock()
	{
		int damage = ToolSystem.Instance.GetDamage();
		
		_blockHp -= damage;
		
		// === ДОБАВЛЯЕМ ВИЗУАЛЬНУЮ СОЧНОСТЬ ===
		Shake(); // Трясем плитку
		
		if (_blockHp <= 0)
		{
			SpawnFloatingText("Broken!", Colors.White);
			DestroyBlock();
		}
		else
		{
			// Показываем остаток HP или просто визуальный фидбек
			SpawnFloatingText($"-{damage}", new Color(1f, 0.8f, 0.2f)); // Желтоватый текст
		}
		
		// Переход в состояние Cracked при первом ударе
		if (_state == TileState.Solid)
		{
			_state = TileState.Cracked;
		}
		
		UpdateVisual();
	}
	
	private void DestroyBlock()
	{
		
		if (_hiddenResource != null)
		{
			// Находка обнаружена!
			_state = TileState.Exposed;
			GD.Print($"[Tile] Found: {_hiddenResource.DisplayName}!");
		}
		else
		{
			// Пустой блок
			_state = TileState.Extracted;
			QueueFree();
		}
	}
	
	// ===== ИЗВЛЕЧЕНИЕ НАХОДКИ =====
	
		private void ExtractOrDamage()
	{
		var tool = ToolSystem.Instance.GetCurrentTool();
		Quality finalQuality = Quality.Good;
		
		if (tool != null && tool.CanDamageFossil && _hiddenResource.HasQuality)
		{
			if (GD.Randf() < UpgradeSystem.Instance.GetToolDamageChance(ToolType.Shovel)) // Используем реальный шанс из UpgradeSystem!
			{
				finalQuality = Quality.Damaged;
				SpawnFloatingText("Damaged!", new Color(1f, 0.3f, 0.3f)); // Красный текст
			}
		}
		
		InventorySystem.Instance.AddItem(_hiddenResource.Id, finalQuality, _hiddenAmount);
		
		// Показываем, что нашли
		SpawnFloatingText($"+1 {_hiddenResource.DisplayName}", new Color(0.3f, 1f, 0.3f)); // Зеленый текст
		
		_state = TileState.Extracted;
		QueueFree();
	}
	
	// ===== ВИЗУАЛ =====
	
	private void UpdateVisual()
{
    // Обновляем отображение HP
    if (_hpLabel != null)
    {
        _hpLabel.Text = _blockHp > 0 ? $"{_blockHp}" : "";
    }
    
    // Обновляем позицию (изометрия)
    UpdatePosition();
}

// НОВЫЙ МЕТОД: Обновление позиции в изометрии
private void UpdatePosition()
{
    // Изометрическая позиция
    var isoPos = IsoUtils.GridToIso(GridPosition.X, GridPosition.Y);
    
    // Добавляем смещение для центрирования сетки на экране
    Position = isoPos + new Vector2(400, 100); // Настройте смещение под свой экран
    
    // Z-порядок для правильной отрисовки
    ZIndex = IsoUtils.GetZOrder(GridPosition.X, GridPosition.Y);
}
	
	private Color GetRarityColor(Rarity rarity)
	{
		return rarity switch
		{
			Rarity.Common => new Color(0.7f, 0.7f, 0.7f),     // Серый
			Rarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),   // Зелёный
			Rarity.Rare => new Color(0.3f, 0.5f, 0.9f),       // Синий
			Rarity.Epic => new Color(0.7f, 0.3f, 0.9f),       // Фиолетовый
			Rarity.Legendary => new Color(1.0f, 0.8f, 0.2f),  // Золотой
			_ => new Color(1f, 1f, 1f)
		};
	}

		// ===== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ (Future-proof) =====
	
	// 1. Тряска плитки при ударе
	private void Shake()
	{
		var tween = CreateTween();
		
		// Явное приведение к float, чтобы исключить ошибку CS1503
		float shakeX = (float)GD.RandRange(-4.0f, 4.0f);
		float shakeY = (float)GD.RandRange(-4.0f, 4.0f);
		Vector2 shakeOffset = new Vector2(shakeX, shakeY);
		
		tween.TweenProperty(this, "position", Position + shakeOffset, 0.05f);
		tween.TweenProperty(this, "position", Position, 0.05f);
	}

	private void SpawnFloatingText(string text, Color textColor)
{
    var label = new Label();
    label.Text = text;
    label.AddThemeFontSizeOverride("font_size", 16);
    label.AddThemeColorOverride("font_color", textColor);
    label.HorizontalAlignment = HorizontalAlignment.Center;
    label.MouseFilter = Control.MouseFilterEnum.Ignore;
    
    // Добавляем в CanvasLayer (UI), чтобы пережил уничтожение плитки
    var fossilUI = GetTree().Root.GetNodeOrNull<CanvasLayer>("DigSite/FossilUI");
    if (fossilUI != null)
    {
        fossilUI.AddChild(label);
        label.GlobalPosition = GlobalPosition + new Vector2(0f, -20f);
    }
    else
    {
        GetTree().Root.AddChild(label);
        label.GlobalPosition = GlobalPosition + new Vector2(0f, -20f);
    }
    
    // ИСПРАВЛЕНИЕ: Создаём tween на Label, а не на плитке
    var tween = label.CreateTween();
    tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.8f);
    tween.Parallel().TweenProperty(label, "position:y", (float)(label.Position.Y - 30f), 0.8f);
    
    tween.TweenCallback(Callable.From(() => label.QueueFree()));
}
}
