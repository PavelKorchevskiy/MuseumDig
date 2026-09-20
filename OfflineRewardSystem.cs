using Godot;
using System;

public partial class OfflineRewardSystem : Node
{
    public static OfflineRewardSystem Instance { get; private set; }
    
    // Лимит офлайн-дохода (4 часа = 14400 секунд)
    private const long MaxOfflineSeconds = 14400;
    
    // Данные текущей награды
    private int _offlineCoins = 0;
    private long _offlineSeconds = 0;
    private bool _hasReward = false;
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    // ===== РАСЧЁТ НАГРАДЫ =====
    
    public void CalculateOfflineReward(long lastSaveTimestamp)
    {
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsedSeconds = currentTimestamp - lastSaveTimestamp;
        
        if (elapsedSeconds <= 0)
        {
            GD.Print("[Offline] Нет времени в офлайне.");
            return;
        }
        
        // Ограничиваем максимумом (4 часа)
        _offlineSeconds = Math.Min(elapsedSeconds, MaxOfflineSeconds);
        
        // Расчёт монет на основе расчетного дохода музея
        int incomePerSecond = MuseumSystem.Instance.GetEstimatedIncomePerSecond();
        _offlineCoins = (int)(_offlineSeconds * incomePerSecond);
        
        _hasReward = _offlineCoins > 0;
        
        GD.Print($"[Offline] Награда рассчитана:");
        GD.Print($"  Время в офлайне: {FormatTime(_offlineSeconds)}");
        GD.Print($"  Монеты: {_offlineCoins}");
    }
    
    // ===== ПРИМЕНЕНИЕ НАГРАДЫ =====
    
    public void CollectReward()
    {
        if (!_hasReward) return;
        
        if (_offlineCoins > 0)
        {
            Wallet.Instance.AddCoins(_offlineCoins);
            GD.Print($"[Offline] Получено {_offlineCoins} монет!");
        }
        
        // Сброс
        _hasReward = false;
        _offlineCoins = 0;
        _offlineSeconds = 0;
    }
    
    // ===== ГЕТТЕРЫ ДЛЯ UI =====
    
    public bool HasReward() => _hasReward;
    public int GetOfflineCoins() => _offlineCoins;
    public long GetOfflineSeconds() => _offlineSeconds;
    
    public string GetFormattedTime()
    {
        return FormatTime(_offlineSeconds);
    }
    
    // ===== УТИЛИТЫ =====
    
    private string FormatTime(long seconds)
    {
        if (seconds < 60) return $"{seconds} сек.";
        if (seconds < 3600) return $"{seconds / 60} мин. {seconds % 60} сек.";
        long hours = seconds / 3600;
        long minutes = (seconds % 3600) / 60;
        return $"{hours} ч. {minutes} мин.";
    }
}