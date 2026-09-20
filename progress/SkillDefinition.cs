using Godot;
using System.Collections.Generic;

public enum SkillBranch
{
    Museum,
    Digging
}

public class SkillDefinition
{
    public string Id;
    public string NameKey;           // Ключ локализации
    public string DescriptionKey;    // Ключ локализации
    public string IconPath;          // Путь к иконке
    public SkillBranch Branch;
    public Vector2I GridPosition;    // Позиция в графе
    public List<string> Prerequisites; // ID необходимых навыков
    public int MaxLevel;
    public int[] CostPerLevel;       // Стоимость каждого уровня
    public string EffectType;        // Ключ эффекта
    public float[] ValuePerLevel;    // Значение эффекта на каждом уровне
    public bool IsFlag;              // true = навык-флаг
}