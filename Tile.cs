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
	// Урон от инструмента + бонус от улучшения кирки
	int toolDamage = ToolSystem.Instance.GetDamage();
	int upgradeBonus = UpgradeSystem.Instance.GetPickaxeDamage() - 1; // -1, т.к. базовый урон уже в инструменте
	int damage = toolDamage + upgradeBonus;
	
	_blockHp -= damage;
	
	GD.Print($"[Tile] Hit with {ToolSystem.Instance.GetToolDisplayName()}: -{damage} HP (remaining: {_blockHp}/{_blockMaxHp})");
	
	// Переход в состояние Cracked при первом ударе
	if (_state == TileState.Solid)
	{
		_state = TileState.Cracked;
	}
	
	if (_blockHp <= 0)
	{
		DestroyBlock();
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
	// Получаем текущий инструмент из ToolSystem
	var tool = ToolSystem.Instance.GetCurrentTool();
	
	Quality finalQuality = Quality.Good;
	
	// Если инструмент может повредить — 50% шанс повреждения
	if (tool != null && tool.CanDamageFossil && _hiddenResource.HasQuality)
	{
		if (GD.Randf() < 0.5f)
		{
			finalQuality = Quality.Damaged;
			GD.Print($"[Tile] ⚠️ Fossil damaged by {tool.DisplayName}!");
		}
	}
	
	// Добавляем в инвентарь
	InventorySystem.Instance.AddItem(_hiddenResource.Id, finalQuality, _hiddenAmount);
	
	// Тайл становится пустым
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
}
