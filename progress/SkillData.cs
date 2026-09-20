using Godot;
using System.Collections.Generic;

public static class SkillData
{
    private static Dictionary<string, SkillDefinition> _skills = new();

    public static void Initialize()
    {
        RegisterMuseumSkills();
        RegisterDiggingSkills();
        GD.Print($"[SkillData] Зарегистрировано {_skills.Count} навыков");
    }

    public static SkillDefinition GetSkill(string id)
    {
        return _skills.TryGetValue(id, out var skill) ? skill : null;
    }

    public static List<SkillDefinition> GetAllSkills()
    {
        return new List<SkillDefinition>(_skills.Values);
    }

    public static List<SkillDefinition> GetSkillsByBranch(SkillBranch branch)
    {
        var result = new List<SkillDefinition>();
        foreach (var skill in _skills.Values)
        {
            if (skill.Branch == branch)
            {
                result.Add(skill);
            }
        }
        return result;
    }

    // ===== ВЕТКА МУЗЕЯ =====
    private static void RegisterMuseumSkills()
    {
        // 1. Больше денег за посетителя (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_ticket_price",
            NameKey = "skill.museum.ticket_price.name",
            DescriptionKey = "skill.museum.ticket_price.desc",
            IconPath = "res://assets/skills/museum/ticket_price.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(0, 0),
            Prerequisites = new List<string>(),
            MaxLevel = 5,
            CostPerLevel = new int[] { 1, 2, 3, 4, 5 },
            EffectType = "visitor_ticket_price",
            ValuePerLevel = new float[] { 50, 60, 75, 100, 150 },
            IsFlag = false
        });

        // 2. Больше посетителей за Common/Uncommon (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_visitors_common",
            NameKey = "skill.museum.visitors_common.name",
            DescriptionKey = "skill.museum.visitors_common.desc",
            IconPath = "res://assets/skills/museum/visitors_common.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(1, 1),
            Prerequisites = new List<string> { "museum_ticket_price" },
            MaxLevel = 3,
            CostPerLevel = new int[] { 2, 3, 4 },
            EffectType = "max_visitors_common_uncommon",
            ValuePerLevel = new float[] { 1, 2, 3 },
            IsFlag = false
        });

        // 3. Больше посетителей за Rare (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_visitors_rare",
            NameKey = "skill.museum.visitors_rare.name",
            DescriptionKey = "skill.museum.visitors_rare.desc",
            IconPath = "res://assets/skills/museum/visitors_rare.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(2, 2),
            Prerequisites = new List<string> { "museum_visitors_common" },
            MaxLevel = 3,
            CostPerLevel = new int[] { 3, 4, 5 },
            EffectType = "max_visitors_rare",
            ValuePerLevel = new float[] { 2, 4, 6 },
            IsFlag = false
        });

        // 4. Больше посетителей за Epic/Legendary (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_visitors_epic_legendary",
            NameKey = "skill.museum.visitors_epic_legendary.name",
            DescriptionKey = "skill.museum.visitors_epic_legendary.desc",
            IconPath = "res://assets/skills/museum/visitors_epic.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(3, 3),
            Prerequisites = new List<string> { "museum_visitors_rare" },
            MaxLevel = 3,
            CostPerLevel = new int[] { 4, 5, 6 },
            EffectType = "max_visitors_epic_legendary",
            ValuePerLevel = new float[] { 3, 6, 9 },
            IsFlag = false
        });

        // 5. Шанс получить билет от посетителя (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_ticket_chance",
            NameKey = "skill.museum.ticket_chance.name",
            DescriptionKey = "skill.museum.ticket_chance.desc",
            IconPath = "res://assets/skills/museum/ticket_chance.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(1, 3),
            Prerequisites = new List<string> { "museum_visitors_common" },
            MaxLevel = 5,
            CostPerLevel = new int[] { 2, 3, 4, 5, 6 },
            EffectType = "ticket_drop_chance",
            ValuePerLevel = new float[] { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f },
            IsFlag = false
        });

        // 6. Открывает новую комнату (флаг)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_new_room",
            NameKey = "skill.museum.new_room.name",
            DescriptionKey = "skill.museum.new_room.desc",
            IconPath = "res://assets/skills/museum/new_room.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(0, 2),
            Prerequisites = new List<string> { "museum_ticket_price" },
            MaxLevel = 1,
            CostPerLevel = new int[] { 10 },
            EffectType = "unlock_room",
            ValuePerLevel = new float[] { 1 },
            IsFlag = true
        });

        // 7. Возможность купить большую витрину (флаг)
        RegisterSkill(new SkillDefinition
        {
            Id = "museum_large_display",
            NameKey = "skill.museum.large_display.name",
            DescriptionKey = "skill.museum.large_display.desc",
            IconPath = "res://assets/skills/museum/large_display.png",
            Branch = SkillBranch.Museum,
            GridPosition = new Vector2I(2, 4),
            Prerequisites = new List<string> { "museum_visitors_rare" },
            MaxLevel = 1,
            CostPerLevel = new int[] { 8 },
            EffectType = "unlock_large_display",
            ValuePerLevel = new float[] { 1 },
            IsFlag = true
        });
    }

    // ===== ВЕТКА РАСКОПОК =====
    private static void RegisterDiggingSkills()
    {
        // 8. Количество землекопов (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "digging_diggers_count",
            NameKey = "skill.digging.diggers_count.name",
            DescriptionKey = "skill.digging.diggers_count.desc",
            IconPath = "res://assets/skills/digging/diggers_count.png",
            Branch = SkillBranch.Digging,
            GridPosition = new Vector2I(0, 0),
            Prerequisites = new List<string>(),
            MaxLevel = 5,
            CostPerLevel = new int[] { 1, 2, 3, 4, 5 },
            EffectType = "digger_count",
            ValuePerLevel = new float[] { 1, 2, 3, 4, 5 },
            IsFlag = false
        });

        // 9. Количество извлекателей (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "digging_extractors_count",
            NameKey = "skill.digging.extractors_count.name",
            DescriptionKey = "skill.digging.extractors_count.desc",
            IconPath = "res://assets/skills/digging/extractors_count.png",
            Branch = SkillBranch.Digging,
            GridPosition = new Vector2I(1, 1),
            Prerequisites = new List<string> { "digging_diggers_count" },
            MaxLevel = 5,
            CostPerLevel = new int[] { 2, 3, 4, 5, 6 },
            EffectType = "extractor_count",
            ValuePerLevel = new float[] { 1, 2, 3, 4, 5 },
            IsFlag = false
        });

        // 10. Скорость работы землекопов (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "digging_diggers_speed",
            NameKey = "skill.digging.diggers_speed.name",
            DescriptionKey = "skill.digging.diggers_speed.desc",
            IconPath = "res://assets/skills/digging/diggers_speed.png",
            Branch = SkillBranch.Digging,
            GridPosition = new Vector2I(0, 2),
            Prerequisites = new List<string> { "digging_diggers_count" },
            MaxLevel = 5,
            CostPerLevel = new int[] { 2, 3, 4, 5, 6 },
            EffectType = "digger_speed",
            ValuePerLevel = new float[] { 1.0f, 1.2f, 1.5f, 2.0f, 2.5f },
            IsFlag = false
        });

        // 11. Скорость работы извлекателей (числовой)
        RegisterSkill(new SkillDefinition
        {
            Id = "digging_extractors_speed",
            NameKey = "skill.digging.extractors_speed.name",
            DescriptionKey = "skill.digging.extractors_speed.desc",
            IconPath = "res://assets/skills/digging/extractors_speed.png",
            Branch = SkillBranch.Digging,
            GridPosition = new Vector2I(1, 3),
            Prerequisites = new List<string> { "digging_extractors_count" },
            MaxLevel = 5,
            CostPerLevel = new int[] { 2, 3, 4, 5, 6 },
            EffectType = "extractor_speed",
            ValuePerLevel = new float[] { 1.0f, 1.2f, 1.5f, 2.0f, 2.5f },
            IsFlag = false
        });

        // 12. Открывает экскаватор (флаг)
        RegisterSkill(new SkillDefinition
        {
            Id = "digging_excavator",
            NameKey = "skill.digging.excavator.name",
            DescriptionKey = "skill.digging.excavator.desc",
            IconPath = "res://assets/skills/digging/excavator.png",
            Branch = SkillBranch.Digging,
            GridPosition = new Vector2I(2, 2),
            Prerequisites = new List<string> { "digging_diggers_speed", "digging_extractors_speed" },
            MaxLevel = 1,
            CostPerLevel = new int[] { 15 },
            EffectType = "unlock_excavator",
            ValuePerLevel = new float[] { 1 },
            IsFlag = true
        });
    }

    private static void RegisterSkill(SkillDefinition skill)
    {
        _skills[skill.Id] = skill;
    }
}