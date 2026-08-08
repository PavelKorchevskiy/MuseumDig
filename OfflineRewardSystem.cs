using Godot;
using System;

public partial class OfflineRewardSystem : Node
{
	public static OfflineRewardSystem Instance { get; private set; }
	
	// Лимит офлайн-дохода (4 часа = 14400 секунд)
	private const long MaxOfflineSeconds = 14400;
	
	// Данные текущей награды
	private int _offlineCoins = 0;
	private int _offlineEnergy = 0;
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
			GD.Print("No offline time");
			return;
		}
		
		// Ограничиваем максимумом
		_offlineSeconds = Math.Min(elapsedSeconds, MaxOfflineSeconds);
		
		// Расчёт монет от музея
		int incomePerSecond = MuseumSystem.Instance.GetTotalIncomePerSecond();
		_offlineCoins = (int)(_offlineSeconds * incomePerSecond);
		
		// Расчёт восстановления энергии
		int maxEnergy = EnergySystem.Instance.GetMaxEnergy();
		int currentEnergy = EnergySystem.Instance.GetCurrentEnergy();
		int missingEnergy = maxEnergy - currentEnergy;
		
		if (missingEnergy > 0)
		{
			double regenInterval = EnergySystem.Instance.GetRegenInterval();
			int regenAmount = (int)(_offlineSeconds / regenInterval);
			_offlineEnergy = Math.Min(regenAmount, missingEnergy);
		}
		else
		{
			_offlineEnergy = 0;
		}
		
		_hasReward = _offlineCoins > 0 || _offlineEnergy > 0;
		
		GD.Print($"Offline reward calculated:");
		GD.Print($"  Time away: {FormatTime(_offlineSeconds)}");
		GD.Print($"  Coins: {_offlineCoins}");
		GD.Print($"  Energy: {_offlineEnergy}");
	}
	
	// ===== ПРИМЕНЕНИЕ НАГРАДЫ =====
	
	public void CollectReward()
	{
		if (!_hasReward) return;
		
		if (_offlineCoins > 0)
		{
			Wallet.Instance.AddCoins(_offlineCoins);
		}
		
		if (_offlineEnergy > 0)
		{
			EnergySystem.Instance.AddEnergy(_offlineEnergy);
		}
		
		GD.Print($"Offline reward collected!");
		_hasReward = false;
		_offlineCoins = 0;
		_offlineEnergy = 0;
	}
	
	// ===== ГЕТТЕРЫ =====
	
	public bool HasReward() => _hasReward;
	public int GetOfflineCoins() => _offlineCoins;
	public int GetOfflineEnergy() => _offlineEnergy;
	public long GetOfflineSeconds() => _offlineSeconds;
	
	public string GetFormattedTime()
	{
		return FormatTime(_offlineSeconds);
	}
	
	// ===== УТИЛИТЫ =====
	
	private string FormatTime(long seconds)
	{
		if (seconds < 60) return $"{seconds}s";
		if (seconds < 3600) return $"{seconds / 60}m {seconds % 60}s";
		long hours = seconds / 3600;
		long minutes = (seconds % 3600) / 60;
		return $"{hours}h {minutes}m";
	}

	// ===== ПУБЛИЧНЫЕ СВОЙСТВА ДЛЯ UI =====
public int OfflineCoins => _offlineCoins;
public int OfflineEnergy => _offlineEnergy;
}
