using Godot;
using System;
using System.Collections.Generic;

public class LocationSystem // ← Убрали ": Node"
{
    private static LocationSystem _instance;
    
    public static LocationSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LocationSystem();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    private string _activeDiggingLocationId = "";
    
    // Приватный конструктор
    private LocationSystem() { }

    private void Initialize()
    {
        GD.Print("[LocationSystem] ✅ Инициализирован как чистый C# синглтон.");
    }
    
    // ===== ГЕТТЕРЫ =====
    
    public string GetActiveDiggingLocationId() => _activeDiggingLocationId;
    
    public bool HasActiveDiggingSession() => !string.IsNullOrEmpty(_activeDiggingLocationId);
    
    public LocationDefinition GetActiveLocation()
    {
        if (HasActiveDiggingSession())
        {
            return GameData.GetLocation(_activeDiggingLocationId);
        }
        return null;
    }

    public List<LocationDefinition> GetAllLocations()
    {
        return GameData.GetAllLocations();
    }
    
    // ===== ЛОГИКА СЕССИЙ РАСКОПОК =====
    
    public void StartDiggingSession(string locationId)
    {
        var location = GameData.GetLocation(locationId);
        if (location == null)
        {
            GD.PrintErr($"[LocationSystem] ❌ Локация '{locationId}' не найдена в GameData!");
            return;
        }

        _activeDiggingLocationId = locationId;
        
        GD.Print($"[LocationSystem] 🚀 Начата новая сессия раскопок: {location.DisplayName}.");
        GD.Print($"[LocationSystem] ⚠️ Предыдущий прогресс раскопок был сброшен/перезаписан.");
        
        SaveSystem.Instance?.MarkDirty();
        
        // Уведомляем через C# событие
        DiggingSessionStarted?.Invoke(locationId);
    }

    public void EndDiggingSession()
    {
        if (HasActiveDiggingSession())
        {
            GD.Print($"[LocationSystem] 🏁 Сессия раскопок в {_activeDiggingLocationId} завершена вручную.");
            _activeDiggingLocationId = "";
            SaveSystem.Instance?.MarkDirty();
            DiggingSessionEnded?.Invoke();
        }
    }

    // ===== ЛОГИКА ДРОПА БИЛЕТОВ =====
    
    public string SelectRandomTicketLocation()
    {
        var allLocations = GameData.GetAllLocations();
        if (allLocations == null || allLocations.Count == 0) return null;

        var weightedList = new List<(string Id, float Weight)>();
        
        foreach (var location in allLocations)
        {
            float weight = location.Rarity switch
            {
                Rarity.Common => 10f,
                Rarity.Uncommon => 7f,
                Rarity.Rare => 4f,
                Rarity.Epic => 2f,
                Rarity.Legendary => 1f,  // В ~10 раз реже обычных
                _ => 5f
            };
            
            weightedList.Add((location.Id, weight));
        }

        float totalWeight = 0f;
        foreach (var item in weightedList)
        {
            totalWeight += item.Weight;
        }

        float roll = GD.Randf() * totalWeight;
        float cumulative = 0f;
        
        foreach (var item in weightedList)
        {
            cumulative += item.Weight;
            if (roll <= cumulative)
            {
                return item.Id;
            }
        }

        return weightedList[weightedList.Count - 1].Id;
    }

    // ===== C# СОБЫТИЯ (вместо Godot [Signal]) =====
    
    public event Action<string> DiggingSessionStarted;
    public event Action DiggingSessionEnded;

    // ===== ДЛЯ СОХРАНЕНИЯ =====
    
    public LocationSaveData GetSaveData()
    {
        return new LocationSaveData
        {
            ActiveDiggingLocationId = _activeDiggingLocationId
        };
    }

    public void LoadFromSaveData(LocationSaveData data)
    {
        if (data == null) 
        {
            _activeDiggingLocationId = "";
            return;
        }
        
        _activeDiggingLocationId = data.ActiveDiggingLocationId ?? "";
        
        if (HasActiveDiggingSession())
        {
            GD.Print($"[LocationSystem] 📂 Загружена активная сессия раскопок: {_activeDiggingLocationId}");
        }
    }
}