using Godot;
using System.Collections.Generic;

public partial class UpgradeSystem : Node
{
public static UpgradeSystem Instance { get; private set; }

// Текущие уровни улучшений (глобальные апгрейды)
private int _pickaxeLevel = 1;
private int _coinBonusLevel = 0;
private int _fossilChanceLevel = 0;

// Уровни инструментов (лопата и кирка)
private int _shovelLevel = 1;
private int _pickaxeToolLevel = 1;

// Базовые значения
private const int BasePickaxeDamage = 1;
private const int BaseCoinReward = 5;
private const float BaseFossilChance = 0.8f;

// Стоимость улучшений
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

public int GetPickaxeDamage()
{
return BasePickaxeDamage + _pickaxeLevel - 1;
}

public int GetCoinReward()
{
return BaseCoinReward + _coinBonusLevel * 2;
}

public float GetFossilChance()
{
return BaseFossilChance + _fossilChanceLevel * 0.05f;
}

// ===== ГЕТТЕРЫ УРОВНЕЙ (ГЛОБАЛЬНЫЕ АПГРЕЙДЫ) =====

public int GetPickaxeLevel() => _pickaxeLevel;
public int GetCoinBonusLevel() => _coinBonusLevel;
public int GetFossilChanceLevel() => _fossilChanceLevel;

// ===== ГЕТТЕРЫ УРОВНЕЙ ИНСТРУМЕНТОВ =====

public int GetShovelLevel() => _shovelLevel;
public int GetPickaxeToolLevel() => _pickaxeToolLevel;

// ===== ХАРАКТЕРИСТИКИ ИНСТРУМЕНТОВ С УЧЁТОМ УРОВНЯ =====

public int GetToolDamage(ToolType toolType)
{
var def = GameData.GetTool(toolType);
if (def == null) return 1;

int level = toolType == ToolType.Shovel ? _shovelLevel : _pickaxeToolLevel;
return def.GetDamageAtLevel(level);
}

public float GetToolUseDelay(ToolType toolType)
{
var def = GameData.GetTool(toolType);
if (def == null) return 0.3f;

int level = toolType == ToolType.Shovel ? _shovelLevel : _pickaxeToolLevel;
return def.GetUseDelayAtLevel(level);
}

public float GetToolFossilDamageChance(ToolType toolType)
{
var def = GameData.GetTool(toolType);
if (def == null) return 0f;

int level = toolType == ToolType.Shovel ? _shovelLevel : _pickaxeToolLevel;
return def.GetFossilDamageChanceAtLevel(level);
}

// ===== СТОИМОСТЬ УЛУЧШЕНИЯ ИНСТРУМЕНТОВ =====

public int GetShovelUpgradeCost()
{
var def = GameData.GetTool(ToolType.Shovel);
if (def == null || _shovelLevel >= def.MaxUpgradeLevel) return -1;
return def.GetUpgradeCostForLevel(_shovelLevel);
}

public int GetPickaxeUpgradeCost()
{
var def = GameData.GetTool(ToolType.Pickaxe);
if (def == null || _pickaxeToolLevel >= def.MaxUpgradeLevel) return -1;
return def.GetUpgradeCostForLevel(_pickaxeToolLevel);
}

public bool CanUpgradeShovel()
{
var def = GameData.GetTool(ToolType.Shovel);
return def != null && _shovelLevel < def.MaxUpgradeLevel;
}

public bool CanUpgradePickaxe()
{
var def = GameData.GetTool(ToolType.Pickaxe);
return def != null && _pickaxeToolLevel < def.MaxUpgradeLevel;
}

// ===== ПОКУПКА УЛУЧШЕНИЙ (ГЛОБАЛЬНЫЕ АПГРЕЙДЫ) =====

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
GD.Print($"Shovel upgraded to level {_shovelLevel}! Damage: {def.GetDamageAtLevel(_shovelLevel)}, Delay: {def.GetUseDelayAtLevel(_shovelLevel):F2}s");
return true;
}
return false;
}

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
GD.Print($"Pickaxe upgraded to level {_pickaxeToolLevel}! Damage: {def.GetDamageAtLevel(_pickaxeToolLevel)}, Delay: {def.GetUseDelayAtLevel(_pickaxeToolLevel):F2}s");
return true;
}
return false;
}

// ===== ЗАГРУЗКА ИЗ СОХРАНЕНИЯ =====

public void LoadFromSaveData(SaveData data)
{
_pickaxeLevel = data.PickaxeLevel;
_coinBonusLevel = data.CoinBonusLevel;
_fossilChanceLevel = data.FossilChanceLevel;

// Загрузка уровней инструментов
_shovelLevel = data.ShovelLevel;
_pickaxeToolLevel = data.PickaxeToolLevel;

GD.Print($"Loaded upgrades: Pickaxe Lv.{_pickaxeLevel}, Coin Lv.{_coinBonusLevel}, Fossil Lv.{_fossilChanceLevel}");
GD.Print($"Loaded tool levels: Shovel Lv.{_shovelLevel}, Pickaxe Lv.{_pickaxeToolLevel}");
}
}
