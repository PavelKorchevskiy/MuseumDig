using Godot;
using System.Collections.Generic;
using System.Linq;

public class SkillSystem // ← Убрали ": Node"
{
    private static SkillSystem _instance;
    
    public static SkillSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SkillSystem();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    private Dictionary<string, int> _skillLevels = new();
    private int _availablePoints = 0;
    private const int ResetCost = 1000;

    // Приватный конструктор, чтобы нельзя было создать экземпляр извне
    private SkillSystem() { }

    private void Initialize()
    {
        SkillData.Initialize();
        GD.Print("[SkillSystem] ✅ Инициализирован как чистый C# синглтон.");
    }

    // ===== ПОЛУЧЕНИЕ ИНФОРМАЦИИ =====
    public int GetAvailablePoints() => _availablePoints;
    public int GetSkillLevel(string skillId) => _skillLevels.TryGetValue(skillId, out int level) ? level : 0;
    public bool IsSkillUnlocked(string skillId) => GetSkillLevel(skillId) > 0;
    
    public bool IsSkillMaxed(string skillId)
    {
        var skill = SkillData.GetSkill(skillId);
        return skill != null && GetSkillLevel(skillId) >= skill.MaxLevel;
    }

    public float GetModifier(string effectType)
    {
        float total = 0;
        foreach (var skill in SkillData.GetAllSkills())
        {
            if (skill.EffectType == effectType && IsSkillUnlocked(skill.Id))
            {
                int level = GetSkillLevel(skill.Id);
                if (level > 0 && level <= skill.ValuePerLevel.Length)
                {
                    total += skill.ValuePerLevel[level - 1];
                }
            }
        }
        return total;
    }

    // ===== ПРОВЕРКА И УЛУЧШЕНИЕ =====
    public bool CanUpgrade(string skillId)
    {
        var skill = SkillData.GetSkill(skillId);
        if (skill == null) return false;

        int currentLevel = GetSkillLevel(skillId);
        if (currentLevel >= skill.MaxLevel) return false;

        foreach (var prereq in skill.Prerequisites)
        {
            if (!IsSkillUnlocked(prereq)) return false;
        }

        int cost = skill.CostPerLevel[currentLevel];
        return _availablePoints >= cost;
    }

    public bool UpgradeSkill(string skillId)
    {
        if (!CanUpgrade(skillId)) return false;

        var skill = SkillData.GetSkill(skillId);
        int currentLevel = GetSkillLevel(skillId);
        int cost = skill.CostPerLevel[currentLevel];

        _availablePoints -= cost;
        _skillLevels[skillId] = currentLevel + 1;

        GD.Print($"[SkillSystem] Улучшен навык {skillId} до уровня {currentLevel + 1}");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    // ===== ОЧКИ И СБРОС =====
    public void AddSkillPoints(int amount)
    {
        _availablePoints += amount;
        GD.Print($"[SkillSystem] +{amount} очков навыков. Всего: {_availablePoints}");
        SaveSystem.Instance?.MarkDirty();
    }

    public bool CanReset()
    {
        return Wallet.Instance.GetCoins() >= ResetCost && _skillLevels.Values.Any(l => l > 0);
    }

    public bool ResetSkills()
    {
        if (!CanReset()) return false;

        int totalPoints = 0;
        foreach (var kvp in _skillLevels)
        {
            var skill = SkillData.GetSkill(kvp.Key);
            if (skill != null)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    totalPoints += skill.CostPerLevel[i];
                }
            }
        }

        _availablePoints += totalPoints;
        _skillLevels.Clear();

        Wallet.Instance.SpendCoins(ResetCost); // Убедитесь, что в Wallet есть метод SpendCoins, или используйте AddCoins(-ResetCost)
        GD.Print($"[SkillSystem] Навыки сброшены. Возвращено {totalPoints} очков.");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    // ===== СОХРАНЕНИЕ/ЗАГРУЗКА =====
    public Dictionary<string, int> GetSaveData() => new Dictionary<string, int>(_skillLevels);
    
    public void LoadFromSaveData(Dictionary<string, int> skillLevels, int availablePoints)
    {
        _skillLevels = skillLevels != null ? new Dictionary<string, int>(skillLevels) : new();
        _availablePoints = availablePoints;
        GD.Print($"[SkillSystem] Загружено {_skillLevels.Count} навыков, {_availablePoints} очков");
    }
}