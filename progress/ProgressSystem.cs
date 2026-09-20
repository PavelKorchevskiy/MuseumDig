using Godot;
using System.Collections.Generic;
using System.Linq;

public class ProgressSystem
{
    private static ProgressSystem _instance;
    
    public static ProgressSystem Instance
    {
        get
        {
            if (_instance == null) _instance = new ProgressSystem();
            return _instance;
        }
    }

    // Храним ID предметов, за которые игрок УЖЕ получил очки навыков
    public HashSet<string> UnlockedCollections { get; private set; } = new();
    public HashSet<string> UnlockedResources { get; private set; } = new();

    private ProgressSystem() { }

    /// <summary>
    /// Регистрирует первое получение предмета. Если предмет новый, начисляет очки навыков.
    /// </summary>
    public void RegisterDiscovery(string resourceId)
    {
        var collection = GameData.GetCollection(resourceId);
        
        if (collection != null)
        {
            // Это собранный скелет. HashSet.Add вернет true только если элемента там еще не было
            if (UnlockedCollections.Add(resourceId))
            {
                int points = collection.Rarity switch
                {
                    Rarity.Common => 4,
                    Rarity.Uncommon => 6,
                    Rarity.Rare => 8,
                    Rarity.Epic => 10,
                    Rarity.Legendary => 15,
                    _ => 4
                };
                
                SkillSystem.Instance.AddSkillPoints(points);
                GD.Print($"[Progress] 🎉 Открыт новый скелет: {collection.DisplayName} (+{points} очков)");
                SaveSystem.Instance?.MarkDirty();
            }
        }
        else
        {
            // Это обычная находка или часть коллекции
            if (UnlockedResources.Add(resourceId))
            {
                SkillSystem.Instance.AddSkillPoints(1);
                GD.Print($"[Progress] 🔍 Открыта новая находка: {resourceId} (+1 очко)");
                SaveSystem.Instance?.MarkDirty();
            }
        }
    }

    // ===== МЕТОДЫ ДЛЯ UI ВКЛАДОК "ПРОГРЕСС" =====

        public List<CollectionDefinition> GetUnlockedCollectionsList()
    {
        GD.Print($"[Progress] 🔍 GetUnlockedCollectionsList вызван. HashSet содержит {UnlockedCollections.Count} элементов");
        
        var result = new List<CollectionDefinition>();
        foreach (var id in UnlockedCollections)
        {
            var col = GameData.GetCollection(id);
            if (col != null)
            {
                result.Add(col);
                GD.Print($"[Progress] ✅ Добавлена коллекция: {col.DisplayName}");
            }
            else
            {
                GD.PrintErr($"[Progress] ❌ Коллекция '{id}' не найдена в GameData!");
            }
        }
        return result.OrderBy(c => c.DisplayName).ToList();
    }

    public List<ResourceDefinition> GetUnlockedResourcesList()
    {
        GD.Print($"[Progress] 🔍 GetUnlockedResourcesList вызван. HashSet содержит {UnlockedResources.Count} элементов");
        
        var result = new List<ResourceDefinition>();
        foreach (var id in UnlockedResources)
        {
            var res = GameData.GetResource(id);
            if (res != null && !(res is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId)))
            {
                result.Add(res);
                GD.Print($"[Progress] ✅ Добавлен ресурс: {res.DisplayName}");
            }
            else if (res == null)
            {
                GD.PrintErr($"[Progress] ❌ Ресурс '{id}' не найден в GameData!");
            }
        }
        return result.OrderBy(r => r.DisplayName).ToList();
    }

    // ===== СОХРАНЕНИЕ / ЗАГРУЗКА =====

    public Dictionary<string, object> GetSaveData()
    {
        return new Dictionary<string, object>
        {
            { "UnlockedCollections", UnlockedCollections.ToList() },
            { "UnlockedResources", UnlockedResources.ToList() }
        };
    }

        public void LoadFromSaveData(HashSet<string> collections, HashSet<string> resources)
    {
        GD.Print($"[Progress] 📥 Загрузка открытых предметов...");
        
        UnlockedCollections.Clear();
        UnlockedResources.Clear();

        if (collections != null)
        {
            foreach (var id in collections)
            {
                UnlockedCollections.Add(id);
            }
            GD.Print($"[Progress] ✅ Загружено {UnlockedCollections.Count} коллекций: {string.Join(", ", UnlockedCollections)}");
        }
        else
        {
            GD.PrintErr("[Progress] ❌ collections = null!");
        }

        if (resources != null)
        {
            foreach (var id in resources)
            {
                UnlockedResources.Add(id);
            }
            GD.Print($"[Progress] ✅ Загружено {UnlockedResources.Count} ресурсов: {string.Join(", ", UnlockedResources)}");
        }
        else
        {
            GD.PrintErr("[Progress] ❌ resources = null!");
        }
    }
}