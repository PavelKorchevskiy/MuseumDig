using Godot;
using System.Collections.Generic;

public partial class UpgradeSystem : Node
{
	public static UpgradeSystem Instance { get; private set; }
	
	// Текущие уровни улучшений
	private int _pickaxeLevel = 1;
	private int _coinBonusLevel = 0;
	private int _fossilChanceLevel = 0;
	
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
	
	// ===== ГЕТТЕРЫ ТЕКУЩИХ ЗНАЧЕНИЙ =====
	
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
	
	// ===== ГЕТТЕРЫ УРОВНЕЙ =====
	
	public int GetPickaxeLevel() => _pickaxeLevel;
	public int GetCoinBonusLevel() => _coinBonusLevel;
	public int GetFossilChanceLevel() => _fossilChanceLevel;
	
	// ===== СТОИМОСТЬ УЛУЧШЕНИЙ =====
	
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
	
	// ===== ПОКУПКА УЛУЧШЕНИЙ =====
	
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

	public void LoadFromSaveData(SaveData data)
{
	_pickaxeLevel = data.PickaxeLevel;
	_coinBonusLevel = data.CoinBonusLevel;
	_fossilChanceLevel = data.FossilChanceLevel;
	GD.Print($"Loaded upgrades: Pickaxe Lv.{_pickaxeLevel}, Coin Lv.{_coinBonusLevel}, Fossil Lv.{_fossilChanceLevel}");
}
}
