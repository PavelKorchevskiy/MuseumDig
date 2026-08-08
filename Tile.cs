using Godot;
using System.Collections.Generic;

public partial class Tile : ColorRect
{
	// ===== СОСТОЯНИЕ ТАЙЛА =====
	private TileState _state = TileState.Solid;
	
	// HP блока (земли)
	private int _blockHp;
	private int _blockMaxHp;
	
	// Что внутри (null = ничего)
	private ResourceDefinition _hiddenResource;
	private int _hiddenAmount = 1;
	
	// Для отладки — глубина ряда (влияет на HP)
	[Export] public int RowIndex = 0;
	
	// ===== ИНИЦИАЛИЗАЦИЯ =====
	
	public void Initialize(int row, float baseHp, float hpGrowth)
	{
		RowIndex = row;
		_blockMaxHp = (int)(baseHp * Mathf.Pow(hpGrowth, row));
		_blockHp = _blockMaxHp;
		
		// Решаем, что внутри, по таблице лута
		_hiddenResource = RollLoot();
		
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
		int toolDamage = ToolSystem.Instance.GetDamage();
		int upgradeBonus = UpgradeSystem.Instance.GetPickaxeDamage() - 1;
		int damage = toolDamage + upgradeBonus;
		
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
		// Монеты за разрушение блока
		Wallet.Instance.AddCoins(UpgradeSystem.Instance.GetCoinReward());
		
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
		switch (_state)
		{
			case TileState.Solid:
				Color = new Color(0.6f, 0.4f, 0.2f); // Коричневый
				break;
				
			case TileState.Cracked:
				// Чем меньше HP, тем темнее
				float ratio = (float)_blockHp / _blockMaxHp;
				Color = new Color(0.4f * ratio + 0.2f, 0.3f * ratio + 0.1f, 0.1f);
				break;
				
			case TileState.Exposed:
				// Цвет зависит от редкости находки
				Color = GetRarityColor(_hiddenResource.Rarity);
				break;
				
			case TileState.Extracted:
				Color = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Полупрозрачный серый
				break;
		}
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

	// 2. Всплывающий текст над плиткой
	private void SpawnFloatingText(string text, Color textColor)
{
    var label = new Label();
    label.Text = text;
    label.AddThemeFontSizeOverride("font_size", 16);
    label.AddThemeColorOverride("font_color", textColor);
    label.HorizontalAlignment = HorizontalAlignment.Center;
    
    // КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: Label не перехватывает клики
    label.MouseFilter = Control.MouseFilterEnum.Ignore;
    
    // Добавляем в CanvasLayer (UI), а не в родителя плитки, 
    // чтобы текст был поверх всех 2D-объектов и не мешал кнопкам
    var fossilUI = GetTree().Root.GetNodeOrNull<CanvasLayer>("DigSite/FossilUI");
    if (fossilUI != null)
    {
        fossilUI.AddChild(label);
        label.GlobalPosition = GlobalPosition + new Vector2(0f, -20f);
    }
    else
    {
        // Fallback: если не нашли FossilUI, добавляем в родителя
        GetParent().AddChild(label);
        label.GlobalPosition = GlobalPosition + new Vector2(0f, -20f);
    }
    
    var tween = CreateTween();
    tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.8f);
    tween.Parallel().TweenProperty(label, "position:y", (float)(label.Position.Y - 30f), 0.8f);
    
    tween.TweenCallback(Callable.From(() => label.QueueFree()));
}
}
