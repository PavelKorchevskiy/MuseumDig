using Godot;
using System;

public partial class UpgradeSystem : Node
{
    public static UpgradeSystem Instance { get; private set; }
    
    // Текущие уровни улучшений
    private int _pickaxeLevel = 1;
    private int _shovelLevel = 1; // НОВОЕ
    private int _coinBonusLevel = 0;
    private int _fossilChanceLevel = 0;
    
    // Стоимость улучшений
    private const int PickaxeBaseCost = 50;
    private const int ShovelBaseCost = 40;  // НОВОЕ
    private const int CoinBonusBaseCost = 30;
    private const int FossilChanceBaseCost = 40;
    private const float CostGrowth = 1.5f;
    private const int MaxLevel = 20;
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    // ===== ГЕТТЕРЫ УРОВНЕЙ =====
    public int GetPickaxeLevel() => _pickaxeLevel;
    public int GetShovelLevel() => _shovelLevel; // НОВОЕ
    public int GetCoinBonusLevel() => _coinBonusLevel;
    public int GetFossilChanceLevel() => _fossilChanceLevel;
    
    // ===== РАСЧЁТ ДИНАМИЧЕСКИХ ХАРАКТЕРИСТИК ИНСТРУМЕНТА =====
    
    public int GetToolDamage(ToolType tool)
    {
        var def = GameData.GetTool(tool);
        if (def == null) return 1;
        
        int level = tool == ToolType.Pickaxe ? _pickaxeLevel : _shovelLevel;
        // Формула: Базовый урон + (Уровень - 1)
        return def.Damage + (level - 1);
    }
    
    public float GetToolDelay(ToolType tool)
    {
        var def = GameData.GetTool(tool);
        if (def == null) return 0.5f;
        
        int level = tool == ToolType.Pickaxe ? _pickaxeLevel : _shovelLevel;
        // Формула: Базовая задержка * (0.95 ^ (Уровень - 1))
        // Уровень 1 = 100%, Уровень 5 = ~81%, Уровень 10 = ~63%
        return def.UseDelay * Mathf.Pow(0.95f, level - 1);
    }
    
    public float GetToolDamageChance(ToolType tool)
    {
        var def = GameData.GetTool(tool);
        if (def == null) return 0f;
        
        int level = tool == ToolType.Pickaxe ? _pickaxeLevel : _shovelLevel;
        // Формула: Базовый шанс * (0.90 ^ (Уровень - 1))
        // Для кирки базовый шанс обычно 0, так что он и останется 0.
        return def.DamageChance * Mathf.Pow(0.90f, level - 1);
    }

	public int GetPickaxeDamage()
    {
        return GetToolDamage(ToolType.Pickaxe);
    }
    
    public int GetCoinReward()
    {
        // Базовая награда 5 + 2 за каждый уровень (как было в оригинале)
        return 5 + _coinBonusLevel * 2;
    }
    
    public float GetFossilChance()
    {
        // Базовый шанс 0.8f (80%) + 0.05f (5%) за каждый уровень
        return 0.8f + _fossilChanceLevel * 0.05f;
    }
    
    // ===== СТОИМОСТЬ УЛУЧШЕНИЙ =====
    
    public int GetPickaxeCost() => (int)(PickaxeBaseCost * Mathf.Pow(CostGrowth, _pickaxeLevel - 1));
    public int GetShovelCost() => (int)(ShovelBaseCost * Mathf.Pow(CostGrowth, _shovelLevel - 1)); // НОВОЕ
    public int GetCoinBonusCost() => (int)(CoinBonusBaseCost * Mathf.Pow(CostGrowth, _coinBonusLevel));
    public int GetFossilChanceCost() => (int)(FossilChanceBaseCost * Mathf.Pow(CostGrowth, _fossilChanceLevel));
    
    // ===== ПОКУПКА УЛУЧШЕНИЙ =====
    
    public bool TryBuyPickaxe()
    {
        if (_pickaxeLevel >= MaxLevel) return false;
        int cost = GetPickaxeCost();
        if (Wallet.Instance.SpendCoins(cost))
        {
            _pickaxeLevel++;
            SaveSystem.Instance?.MarkDirty();
            GD.Print($"[Upgrade] Pickaxe -> Lv.{_pickaxeLevel} (Dmg: {GetToolDamage(ToolType.Pickaxe)})");
            return true;
        }
        return false;
    }
    
    public bool TryBuyShovel() // НОВОЕ
    {
        if (_shovelLevel >= MaxLevel) return false;
        int cost = GetShovelCost();
        if (Wallet.Instance.SpendCoins(cost))
        {
            _shovelLevel++;
            SaveSystem.Instance?.MarkDirty();
            GD.Print($"[Upgrade] Shovel -> Lv.{_shovelLevel} (Delay: {GetToolDelay(ToolType.Shovel):F2}s, DmgChance: {GetToolDamageChance(ToolType.Shovel):P1})");
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
            return true;
        }
        return false;
    }

    // ===== ЗАГРУЗКА =====
    public void LoadFromSaveData(SaveData data)
    {
        _pickaxeLevel = data.PickaxeLevel;
        _shovelLevel = data.ShovelLevel; // НОВОЕ
        _coinBonusLevel = data.CoinBonusLevel;
        _fossilChanceLevel = data.FossilChanceLevel;
        GD.Print($"[UpgradeSystem] Loaded: Pickaxe Lv.{_pickaxeLevel}, Shovel Lv.{_shovelLevel}, Coin Lv.{_coinBonusLevel}, Fossil Lv.{_fossilChanceLevel}");
    }
}