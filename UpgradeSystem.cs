using Godot;
using System;

public partial class UpgradeSystem : Node
{
    public static UpgradeSystem Instance { get; private set; }
    
    private int _pickaxeLevel = 1;
    private int _shovelLevel = 1;
    
    private const int PickaxeBaseCost = 50;
    private const int ShovelBaseCost = 40;
    private const float CostGrowth = 1.5f;
    private const int MaxLevel = 20;
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    public int GetPickaxeLevel() => _pickaxeLevel;
    public int GetShovelLevel() => _shovelLevel;
    
    public int GetToolDamage(ToolType tool)
    {
        var def = GameData.GetTool(tool);
        if (def == null) return 1;
        
        int level = tool == ToolType.Pickaxe ? _pickaxeLevel : _shovelLevel;
        return def.Damage + (level - 1);
    }
    
    public float GetToolDelay(ToolType tool)
    {
        var def = GameData.GetTool(tool);
        if (def == null) return 0.5f;
        
        int level = tool == ToolType.Pickaxe ? _pickaxeLevel : _shovelLevel;
        return def.UseDelay * Mathf.Pow(0.95f, level - 1);
    }
    
    public float GetToolDamageChance(ToolType tool)
    {
        var def = GameData.GetTool(tool);
        if (def == null) return 0f;
        
        int level = tool == ToolType.Pickaxe ? _pickaxeLevel : _shovelLevel;
        return def.DamageChance * Mathf.Pow(0.90f, level - 1);
    }
    
    public int GetPickaxeCost() => (int)(PickaxeBaseCost * Mathf.Pow(CostGrowth, _pickaxeLevel - 1));
    public int GetShovelCost() => (int)(ShovelBaseCost * Mathf.Pow(CostGrowth, _shovelLevel - 1));
    
    public bool TryBuyPickaxe()
    {
        if (_pickaxeLevel >= MaxLevel) return false;
        int cost = GetPickaxeCost();
        if (Wallet.Instance.SpendCoins(cost))
        {
            _pickaxeLevel++;
            SaveSystem.Instance?.MarkDirty();
            GD.Print($"[Upgrade] Pickaxe -> Lv.{_pickaxeLevel}");
            return true;
        }
        return false;
    }
    
    public bool TryBuyShovel()
    {
        if (_shovelLevel >= MaxLevel) return false;
        int cost = GetShovelCost();
        if (Wallet.Instance.SpendCoins(cost))
        {
            _shovelLevel++;
            SaveSystem.Instance?.MarkDirty();
            GD.Print($"[Upgrade] Shovel -> Lv.{_shovelLevel}");
            return true;
        }
        return false;
    }

    public void LoadFromSaveData(SaveData data)
    {
        _pickaxeLevel = data.PickaxeLevel;
        _shovelLevel = data.ShovelLevel;
        GD.Print($"[UpgradeSystem] Loaded: Pickaxe Lv.{_pickaxeLevel}, Shovel Lv.{_shovelLevel}");
    }
}