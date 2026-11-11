using Godot;
using System;
using System.Collections.Generic;

public class DiggingManager
{
    private static DiggingManager _instance;
    public static DiggingManager Instance
    {
        get
        {
            if (_instance == null) _instance = new DiggingManager();
            return _instance;
        }
    }

    // Активные плитки (только когда игрок на локации раскопок)
    private Dictionary<string, Tile> _activeTiles = new();

    // === НОВОЕ: Сохранённые данные по всем локациям ===
    // Ключ: locationId, Значение: словарь "x,y" -> DiggingTileData
    private Dictionary<string, Dictionary<string, DiggingTileData>> _savedProgressByLocation = new();

    // Таймстамп выхода со сцены раскопок по локациям — используется для
    // догонки прогресса копателей/извлекателей, пока игрок был в музее
    // (та же идея, что офлайн-доход музея, но без деления пополам: игрок
    // просил "как будто бы я и на сцене раскопок"). Живёт только в памяти,
    // как и сам синглтон — не переживает перезапуск приложения.
    private Dictionary<string, long> _lastExitTimestampByLocation = new();

    private DiggingManager() { }

    // ===== РАБОТА С АКТИВНЫМИ ПЛИТКАМИ =====

 public int GetActiveTilesCount()
    {
        return _activeTiles.Count;
    }
    public void RegisterTile(Tile tile)
    {
        string key = $"{tile.GridPosition.X},{tile.GridPosition.Y}";
        _activeTiles[key] = tile;
                GD.Print($"[DiggingManager] ✅ Зарегистрирован тайл {key}. Всего активных: {_activeTiles.Count}");

    }

    public void UnregisterTile(Tile tile)
    {
        string key = $"{tile.GridPosition.X},{tile.GridPosition.Y}";
        _activeTiles.Remove(key);
    }

        public void ProcessDelta(float delta)
    {
        if (!LocationSystem.Instance.HasActiveDiggingSession()) return;

        // === ДИАГНОСТИКА ===
        if (_activeTiles.Count == 0)
        {
            GD.PrintErr("[DiggingManager] ⚠️ Нет активных тайлов!");
            return;
        }

        float diggerSpeedMod = 1f + (SkillSystem.Instance.GetModifier("digger_speed") / 100f);
        float extractorSpeedMod = 1f + (SkillSystem.Instance.GetModifier("extractor_speed") / 100f);

        float baseDigDamagePerSec = (10f / 60f) * diggerSpeedMod;
        float baseExtractProgressPerSec = (10f / 60f) * extractorSpeedMod;

        foreach (var tile in _activeTiles.Values)
        {
            if (tile.Action == TileAction.Digging)
            {
                tile.ApplyDiggingDamage(baseDigDamagePerSec * delta);
            }
            else if (tile.Action == TileAction.Extracting)
            {
                tile.ApplyExtractionProgress(baseExtractProgressPerSec * delta);
            }
        }
    }

    /// <summary>
    /// Запоминает момент выхода со сцены раскопок для локации (переход в
    /// музей и т.п.). Вызывается из DigSite._ExitTree().
    /// </summary>
    public void MarkLocationExited(string locationId)
    {
        _lastExitTimestampByLocation[locationId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Догоняет прогресс активных (уже зарегистрированных) тайлов локации за
    /// время, прошедшее с момента выхода со сцены раскопок. Вызывается из
    /// DigSite сразу после загрузки/генерации сетки для этой локации — т.е.
    /// когда все её тайлы уже есть в _activeTiles.
    ///
    /// Работает одним разовым применением урона/прогресса (как офлайн-доход
    /// музея), а не покадровым циклом: ApplyDiggingDamage/ApplyExtractionProgress
    /// сами переводят Action в None при завершении стадии/извлечения, так что
    /// тайл не "проскакивает" сразу несколько стадий — ровно как если бы
    /// игрок всё это время стоял на раскопках, не перезапуская работу вручную.
    /// </summary>
    public void ApplyCatchUpForLocation(string locationId)
    {
        if (!_lastExitTimestampByLocation.TryGetValue(locationId, out long lastExit))
            return; // игрок ещё не уходил с этой локации — догонять нечего

        _lastExitTimestampByLocation.Remove(locationId);

        const long maxCatchUpSeconds = 12 * 3600;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = Math.Min(Math.Max(now - lastExit, 0), maxCatchUpSeconds);
        if (elapsed <= 0) return;
        if (_activeTiles.Count == 0) return;

        float diggerSpeedMod = 1f + (SkillSystem.Instance.GetModifier("digger_speed") / 100f);
        float extractorSpeedMod = 1f + (SkillSystem.Instance.GetModifier("extractor_speed") / 100f);

        float digDamage = (10f / 60f) * diggerSpeedMod * elapsed;
        float extractProgress = (10f / 60f) * extractorSpeedMod * elapsed;

        int affected = 0;
        foreach (var tile in _activeTiles.Values)
        {
            if (tile.Action == TileAction.Digging)
            {
                tile.ApplyDiggingDamage(digDamage);
                affected++;
            }
            else if (tile.Action == TileAction.Extracting)
            {
                tile.ApplyExtractionProgress(extractProgress);
                affected++;
            }
        }

        if (affected > 0)
            GD.Print($"[DiggingManager] ⏱️ Догонка для '{locationId}': {elapsed} сек, затронуто тайлов: {affected}");
    }

    // ===== ЛИМИТЫ РАБОЧИХ =====

    public int GetActiveDiggersCount()
    {
        int count = 0;
        foreach (var tile in _activeTiles.Values)
        {
            if (tile.Action == TileAction.Digging) count++;
        }
        return count;
    }

    public int GetActiveExtractorsCount()
    {
        int count = 0;
        foreach (var tile in _activeTiles.Values)
        {
            if (tile.Action == TileAction.Extracting) count++;
        }
        return count;
    }

    public int GetMaxDiggers()
    {
        int skillLevel = SkillSystem.Instance.GetSkillLevel("digging_diggers_count");
        return 1 + skillLevel;
    }

    public int GetMaxExtractors()
    {
        int skillLevel = SkillSystem.Instance.GetSkillLevel("digging_extractors_count");
        return 1 + skillLevel;
    }

    public bool IsExcavatorUnlocked()
    {
        return SkillSystem.Instance.GetSkillLevel("digging_excavator") > 0;
    }

    // ===== СОХРАНЕНИЕ / ЗАГРУЗКА =====

    /// <summary>
    /// Возвращает сохраненные данные для конкретной локации.
    /// Вызывается из DigSite при генерации сетки.
    /// </summary>
    public Dictionary<string, DiggingTileData> GetSaveDataForLocation(string locationId)
    {
        if (_savedProgressByLocation.TryGetValue(locationId, out var data))
        {
            return data;
        }
        return new Dictionary<string, DiggingTileData>();
    }

    /// <summary>
    /// Сохраняет данные конкретной локации.
    /// Вызывается из DigSite при выходе со сцены.
    /// </summary>
    public void SetSaveDataForLocation(string locationId, Dictionary<string, DiggingTileData> data)
    {
        _savedProgressByLocation[locationId] = data;
    }

    /// <summary>
    /// Возвращает ВСЕ данные раскопок для сохранения в JSON.
    /// Вызывается из SaveSystem.SaveGame().
    /// </summary>
    public Dictionary<string, Dictionary<string, DiggingTileData>> GetAllSaveData()
    {
        SyncCurrentDigSite();
        return _savedProgressByLocation;
    }

    /// <summary>
    /// Загружает все данные из сохранения.
    /// Вызывается из SaveSystem.LoadGame().
    /// </summary>
    public void LoadAllSaveData(Dictionary<string, Dictionary<string, DiggingTileData>> data)
    {
        _savedProgressByLocation.Clear();
        if (data == null) return;

        foreach (var kvp in data)
        {
            _savedProgressByLocation[kvp.Key] = kvp.Value;
        }

        GD.Print($"[DiggingManager] 📂 Загружены данные раскопок для {_savedProgressByLocation.Count} локаций");
    }

        /// <summary>
    /// Синхронизирует данные текущей сцены раскопок с хранилищем.
    /// Вызывается автоматически перед сохранением.
    /// </summary>
    private void SyncCurrentDigSite()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        var digSite = sceneTree?.CurrentScene as DigSite;
        
        if (digSite != null)
        {
            var location = LocationSystem.Instance?.GetActiveLocation();
            if (location != null)
            {
                var data = digSite.CollectTileData();
                SetSaveDataForLocation(location.Id, data);
            }
        }
    }


    /// <summary>
    /// Сбрасывает прогресс конкретной локации (при использовании нового билета).
    /// Вызывается из InventoryUI.OnUseTicketPressed().
    /// </summary>
    public void ResetLocationProgress(string locationId)
    {
        if (_savedProgressByLocation.ContainsKey(locationId))
        {
            _savedProgressByLocation.Remove(locationId);
            GD.Print($"[DiggingManager] 🔄 Сброшен прогресс локации: {locationId}");
        }
    }
}