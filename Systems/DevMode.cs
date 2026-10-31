using Godot;

/// <summary>
/// Синглтон для управления режимом разработчика.
/// Загружает настройки из res://dev_mode.cfg
/// </summary>
public class DevMode
{
    private static DevMode _instance;
    
    public static DevMode Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new DevMode();
                _instance.LoadConfig();
            }
            return _instance;
        }
    }

    // === Флаги фич ===
    public bool Enabled { get; private set; }
    public bool FreeCamera { get; private set; }
    public bool HighDropChance { get; private set; }
    public bool InfiniteGold { get; private set; }
    public bool SpawnVisitorsFast { get; private set; }
        public bool ShowGridCoords { get; private set; }

    private DevMode() { }

    private void LoadConfig()
    {
        var config = new ConfigFile();
        string path = "res://dev_mode.cfg";

        var error = config.Load(path);
        if (error != Error.Ok)
        {
            GD.Print($"[DevMode] Конфиг не найден или ошибка чтения: {error}. Режим разработчика ВЫКЛЮЧЕН.");
            Enabled = false;
            return;
        }

        Enabled = (bool)config.GetValue("general", "enabled", false);
        
        if (!Enabled)
        {
            GD.Print("[DevMode] Режим разработчика ВЫКЛЮЧЕН в конфиге.");
            return;
        }

        FreeCamera = (bool)config.GetValue("features", "free_camera", false);
        HighDropChance = (bool)config.GetValue("features", "high_drop_chance", false);
        InfiniteGold = (bool)config.GetValue("features", "infinite_gold", false);
        SpawnVisitorsFast = (bool)config.GetValue("features", "spawn_visitors_fast", false);
                ShowGridCoords = (bool)config.GetValue("features", "show_grid_coords", false);

        GD.Print($"[DevMode] ✅ Режим разработчика ВКЛЮЧЕН:");
        GD.Print($"  • FreeCamera: {FreeCamera}");
        GD.Print($"  • HighDropChance: {HighDropChance}");
        GD.Print($"  • InfiniteGold: {InfiniteGold}");
        GD.Print($"  • SpawnVisitorsFast: {SpawnVisitorsFast}");
                GD.Print($"  • ShowGridCoords: {ShowGridCoords}");
    }

    /// <summary>
    /// Перезагружает конфиг (можно вызвать после редактирования файла).
    /// </summary>
    public void Reload()
    {
        LoadConfig();
    }
}