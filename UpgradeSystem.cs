using Godot;
using System.Collections.Generic;

/// <summary>
/// Система улучшений для глобальных апгрейдов и инструментов.
/// </summary>
public partial class UpgradeSystem : Node
{
	public static UpgradeSystem Instance { get; private set; }
	
	// ===== ГЛОБАЛЬНЫЕ УЛУЧШЕНИЯ =====
	private int _pickaxeLevel = 1;
	private int _coinBonusLevel = 0;
	private int _fossilChanceLevel = 0;
	
	// ===== УРОВНИ ИНСТРУМЕНТОВ =====
	private int _shovelLevel = 1;
	private int _pickaxeToolLevel = 1;
	
	// ===== БАЗОВЫЕ ЗНАЧЕНИЯ (ГЛОБАЛЬНЫЕ АПГРЕЙДЫ) =====
	private const int BasePickaxeDamage = 1;
	private const int BaseCoinReward = 5;
	private const float BaseFossilChance = 0.8f;
	
	// ===== СТОИМОСТЬ И ЛИМИТЫ (ГЛОБАЛЬНЫЕ АПГРЕЙДЫ) =====
	private const int PickaxeBaseCost = 50;
	private const int CoinBonusBaseCost = 30;
	private const int FossilChanceBaseCost = 40;
	private const float CostGrowth = 1.5f;
	private const int MaxLevel = 20;
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	// ===== ГЕТТЕРЫ ТЕКУЩИХ ЗНАЧЕНИЙ (ГЛОБАЛЬНЫЕ АПГРЕЙДЫ) =====
	
	/// <summary>Получить урон кирки с учётом глобального улучшения.</summary>
	public int GetPickaxeDamage()
	{
		return BasePickaxeDamage + _pickaxeLevel - 1;
	}
	
	/// <summary>Получить награду за монеты с учётом улучшения бонуса.</summary>
	public int GetCoinReward()
	{
		return BaseCoinReward + _coinBonusLevel * 2;
	}
	
	/// <summary>Получить шанс нахождения окаменелости с учётом улучшения.</summary>
	public float GetFossilChance()
	{
		return BaseFossilChance + _fossilChanceLevel * 0.05f;
	}
	
	// ===== ГЕТТЕРЫ УРОВНЕЙ (ГЛОБАЛЬНЫЕ АПГРЕЙДЫ) =====
	
	public int GetPickaxeLevel() => _pickaxeLevel;
	public int GetCoinBonusLevel() => _coinBonusLevel;
	public int GetFossilChanceLevel() => _fossilChanceLevel;
	
	// ===== ГЕТТЕРЫ УРОВНЕЙ ИНСТРУМЕНТОВ =====
	
	/// <summary>Получить текущий уровень лопаты.</summary>
	public int GetShovelLevel() => _shovelLevel;
	
	/// <summary>Получить текущий уровень кирки как инструмента.</summary>
	public int GetPickaxeToolLevel() => _pickaxeToolLevel;
	
	// ===== ХАРАКТЕРИСТИКИ ИНСТРУМЕНТОВ С УЧЁТОМ УРОВНЯ =====
	
	/// <summary>Получить урон инструмента с учётом его уровня улучшения.</summary>
	public int GetToolDamage(ToolType toolType)
	{
		var def = GameData.GetTool(toolType);
		if (def == null) return 1;
		
		int level = toolType == ToolType.Shovel ? _shovelLevel : _pickaxeToolLevel;
		return def.GetDamageAtLevel(level);
	}
	
	/// <summary>Получить задержку между ударами инструмента с учётом его уровня.</summary>
	public float GetToolUseDelay(ToolType toolType)
	{
		var def = GameData.GetTool(toolType);
		if (def == null) return 0.3f;
		
		int level = toolType == ToolType.Shovel ? _shovelLevel : _pickaxeToolLevel;
		return def.GetUseDelayAtLevel(level);
	}
	
	/// <summary>Получить шанс повреждения окаменелости для инструмента с учётом уровня.</summary>
	public float GetToolFossilDamageChance(ToolType toolType)
	{
		var def = GameData.GetTool(toolType);
		if (def == null) return 0f;
		
		int level = toolType == ToolType.Shovel ? _shovelLevel : _pickaxeToolLevel;
		return def.GetFossilDamageChanceAtLevel(level);
	}
	
	// ===== СТОИМОСТЬ УЛУЧШЕНИЯ ИНСТРУМЕНТОВ =====
	
	/// <summary>Получить стоимость улучшения лопаты на следующий уровень.</summary>
	public int GetShovelUpgradeCost()
	{
		var def = GameData.GetTool(ToolType.Shovel);
		if (def == null || _shovelLevel >= def.MaxUpgradeLevel) return -1;
		return def.GetUpgradeCostForLevel(_shovelLevel);
	}
	
	/// <summary>Получить стоимость улучшения кирки на следующий уровень.</summary>
	public int GetPickaxeUpgradeCost()
	{
		var def = GameData.GetTool(ToolType.Pickaxe);
		if (def == null || _pickaxeToolLevel >= def.MaxUpgradeLevel) return -1;
		return def.GetUpgradeCostForLevel(_pickaxeToolLevel);
	}
	
	/// <summary>Проверить, можно ли улучшить лопату.</summary>
	public bool CanUpgradeShovel()
	{
		var def = GameData.GetTool(ToolType.Shovel);
		return def != null && _shovelLevel < def.MaxUpgradeLevel;
	}
	
	/// <summary>Проверить, можно ли улучшить кирку.</summary>
	public bool CanUpgradePickaxe()
	{
		var def = GameData.GetTool(ToolType.Pickaxe);
		return def != null && _pickaxeToolLevel < def.MaxUpgradeLevel;
	}
	
	// ===== ПОКУПКА ГЛОБАЛЬНЫХ УЛУЧШЕНИЙ =====
	
	public bool TryBuyPickaxe()
	{
		if (_pickaxeLevel >= MaxLevel) return false;
		
		int cost = GetPickaxeCost();
		if (Wallet.Instance.SpendCoins(cost))
		{
			_pickaxeLevel++;
			SaveSystem.Instance?.MarkDirty();
			GD.Print($"Pickaxe upgraded to level {_pickaxeLevel}! Damage: {GetPickaxeDamage()}");
			return true;
		}
		return false;
	}
	
	public bool TryBuyCoinBonus()
	{
		if (_coinBonusLevel >= MaxLevel) return false;
		
		int cost = GetCoinBonusCost();
		if (Wallet.Instance.SpendCoins(cost))
		{
			_coinBonusLevel++;
			SaveSystem.Instance?.MarkDirty();
			GD.Print($"Coin bonus upgraded to level {_coinBonusLevel}! Reward: {GetCoinReward()}");
			return true;
		}
		return false;
	}
	
	public bool TryBuyFossilChance()
	{
		if (_fossilChanceLevel >= MaxLevel) return false;
		
		int cost = GetFossilChanceCost();
		if (Wallet.Instance.SpendCoins(cost))
		{
			_fossilChanceLevel++;
			SaveSystem.Instance?.MarkDirty();
			GD.Print($"Fossil chance upgraded to level {_fossilChanceLevel}! Chance: {GetFossilChance():P1}");
			return true;
		}
		return false;
	}
	
	// ===== ПОКУПКА УЛУЧШЕНИЙ ИНСТРУМЕНТОВ =====
	
	/// <summary>Попытаться улучшить лопату. Возвращает true при успехе.</summary>
	public bool TryUpgradeShovel()
	{
		if (!CanUpgradeShovel()) return false;
		
		int cost = GetShovelUpgradeCost();
		if (cost < 0) return false;
		
		if (Wallet.Instance.SpendCoins(cost))
		{
			_shovelLevel++;
			SaveSystem.Instance?.MarkDirty();
			
			var def = GameData.GetTool(ToolType.Shovel);
			GD.Print($"[Upgrade] Shovel → Lv.{_shovelLevel} | DMG: {def.GetDamageAtLevel(_shovelLevel)}, Delay: {def.GetUseDelayAtLevel(_shovelLevel):F2}s, Dmg Chance: {def.GetFossilDamageChanceAtLevel(_shovelLevel):P0}");
			return true;
		}
		return false;
	}
	
	/// <summary>Попытаться улучшить кирку. Возвращает true при успехе.</summary>
	public bool TryUpgradePickaxe()
	{
		if (!CanUpgradePickaxe()) return false;
		
		int cost = GetPickaxeUpgradeCost();
		if (cost < 0) return false;
		
		if (Wallet.Instance.SpendCoins(cost))
		{
			_pickaxeToolLevel++;
			SaveSystem.Instance?.MarkDirty();
			
			var def = GameData.GetTool(ToolType.Pickaxe);
			GD.Print($"[Upgrade] Pickaxe → Lv.{_pickaxeToolLevel} | DMG: {def.GetDamageAtLevel(_pickaxeToolLevel)}, Delay: {def.GetUseDelayAtLevel(_pickaxeToolLevel):F2}s");
			return true;
		}
		return false;
	}
	
	// ===== ЗАГРУЗКА ИЗ СОХРАНЕНИЯ =====
	
	/// <summary>Загрузить уровни улучшений из сохранённых данных.</summary>
	public void LoadFromSaveData(SaveData data)
	{
		_pickaxeLevel = data.PickaxeLevel;
		_coinBonusLevel = data.CoinBonusLevel;
		_fossilChanceLevel = data.FossilChanceLevel;
		
		_shovelLevel = data.ShovelLevel;
		_pickaxeToolLevel = data.PickaxeToolLevel;
		
		GD.Print($"[UpgradeSystem] Loaded: Pickaxe Lv.{_pickaxeLevel}, Coin Lv.{_coinBonusLevel}, Fossil Lv.{_fossilChanceLevel}");
		GD.Print($"[UpgradeSystem] Tools: Shovel Lv.{_shovelLevel}, Pickaxe Lv.{_pickaxeToolLevel}");
	}
	
	// ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ГЛОБАЛЬНЫХ АПГРЕЙДОВ =====
	
	public int GetPickaxeCost()
	{
		return (int)(PickaxeBaseCost * Mathf.Pow(CostGrowth, _pickaxeLevel - 1));
	}
	
	public int GetCoinBonusCost()
	{
		return (int)(CoinBonusBaseCost * Mathf.Pow(CostGrowth, _coinBonusLevel));
	}
	
	public int GetFossilChanceCost()
	{
		return (int)(FossilChanceBaseCost * Mathf.Pow(CostGrowth, _fossilChanceLevel));
	}
}
