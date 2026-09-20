using Godot;
using System;
using System.Text.Json;

public partial class SaveSystem : Node
{
    public static SaveSystem Instance { get; private set; }
    
    private const string SavePath = "user://save.json";
    
    private bool _dirty = false;
    private double _saveTimer = 0;
    private const double AutoSaveInterval = 5.0;
    private long _lastSessionTimestamp = 0;

        public override void _Ready()
    {
        Instance = this;
        
        // === ИНИЦИАЛИЗИРУЕМ СИНГЛТОНЫ ДО ЗАГРУЗКИ ===
        var _ = ProgressSystem.Instance; // Принудительно создаём экземпляр
        var __ = SkillSystem.Instance;   // Принудительно создаём экземпляр
        
        CallDeferred(nameof(LoadGame));
    }
    
    public override void _Process(double delta)
    {
        if (_dirty)
        {
            _saveTimer += delta;
            if (_saveTimer >= AutoSaveInterval)
            {
                _saveTimer = 0;
                SaveGame(isAutoSave: true);
                _dirty = false;
            }
        }
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            GD.Print("Window closing, forcing save...");
            ForceSaveAndQuit();
        }
    }
    
    public void MarkDirty() => _dirty = true;



    
    public void SaveGame(bool isAutoSave = false)
    {
        GD.Print(isAutoSave ? "=== AUTO SAVE START ===" : "=== MANUAL SAVE START ===");
        
        var data = new SaveData();
        
        if (Wallet.Instance != null) data.Coins = Wallet.Instance.GetCoins();
        if (UpgradeSystem.Instance != null)
        {
            data.PickaxeLevel = UpgradeSystem.Instance.GetPickaxeLevel();
            data.ShovelLevel = UpgradeSystem.Instance.GetShovelLevel();
        }
        if (FossilInventory.Instance != null) data.FossilPieces = FossilInventory.Instance.GetSaveData();
        if (MuseumSystem.Instance != null) data.MuseumData = MuseumSystem.Instance.GetSaveData();
        if (InventorySystem.Instance != null) data.Inventory = InventorySystem.Instance.GetSaveData();
        if (ToolSystem.Instance != null) data.CurrentTool = ToolSystem.Instance.GetSaveData();
        if (LocationSystem.Instance != null) data.LocationData = LocationSystem.Instance.GetSaveData();
        
        // === НОВОЕ: Сохранение открытых предметов и навыков ===
    
        if (SkillSystem.Instance != null)
        {
                    data.ProgressData = ProgressSystem.Instance.GetSaveData();
        data.SkillLevels = SkillSystem.Instance.GetSaveData();
        data.AvailableSkillPoints = SkillSystem.Instance.GetAvailablePoints();
        }

        if (isAutoSave)
        {
            data.LastSaveTimestamp = _lastSessionTimestamp;
        }
        else
        {
            data.LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _lastSessionTimestamp = data.LastSaveTimestamp;
        }
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print("Game saved successfully!");
        }
        else
        {
            GD.PrintErr("Failed to open save file for writing!");
        }
        
        GD.Print(isAutoSave ? "=== AUTO SAVE END ===" : "=== MANUAL SAVE END ===");
    }
    
    public void ForceSaveAndQuit()
    {
        SaveGame(isAutoSave: false);
        GetTree().Quit();
    }
    
    public void LoadGame()
    {
        GD.Print("=== LOAD GAME START ===");
        
        if (!FileAccess.FileExists(SavePath))
        {
            GD.Print("No save file found, starting new game");
            return;
        }
        
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Failed to open save file for reading!");
            return;
        }
        
        string json = file.GetAsText();
        
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data == null)
            {
                GD.PrintErr("Save data is null!");
                return;
            }
            
            if (Wallet.Instance != null) Wallet.Instance.SetCoins(data.Coins);
            if (UpgradeSystem.Instance != null) UpgradeSystem.Instance.LoadFromSaveData(data);
            if (FossilInventory.Instance != null) FossilInventory.Instance.LoadFromSaveData(data.FossilPieces);
            if (data.MuseumData != null) MuseumSystem.Instance.LoadFromSaveData(data.MuseumData);
            if (InventorySystem.Instance != null) InventorySystem.Instance.LoadFromSaveData(data.Inventory);
            if (ToolSystem.Instance != null) ToolSystem.Instance.LoadFromSaveData(data.CurrentTool);
            if (LocationSystem.Instance != null) LocationSystem.Instance.LoadFromSaveData(data.LocationData);
            
           
            if (SkillSystem.Instance != null)
            {
                                   // === Загрузка открытых предметов ===
            GD.Print($"[SaveSystem] 📊 data.UnlockedCollections count: {data.UnlockedCollections?.Count ?? 0}");
            GD.Print($"[SaveSystem] 📊 data.UnlockedResources count: {data.UnlockedResources?.Count ?? 0}");
            
            if (ProgressSystem.Instance != null)
            {
                ProgressSystem.Instance.LoadFromSaveData(data.UnlockedCollections, data.UnlockedResources);
            }
            else
            {
                GD.PrintErr("[SaveSystem] ❌ ProgressSystem.Instance = null при загрузке!");
            }
        SkillSystem.Instance.LoadFromSaveData(data.SkillLevels, data.AvailableSkillPoints);
            }
            
            _lastSessionTimestamp = data.LastSaveTimestamp;
            GD.Print($"Loaded last session timestamp: {_lastSessionTimestamp}");
            
            if (_lastSessionTimestamp > 0 && OfflineRewardSystem.Instance != null)
            {
                OfflineRewardSystem.Instance.CalculateOfflineReward(_lastSessionTimestamp);
            }
            
            GD.Print("=== LOAD GAME END ===");
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Failed to load save: {e.Message}");
        }
    }
    
    public void ResetGame()
    {
        if (FileAccess.FileExists(SavePath))
        {
            DirAccess.RemoveAbsolute(SavePath);
        }
        GD.Print("Save file deleted");
    }

    public long GetLastSaveTimestamp() => _lastSessionTimestamp;
}