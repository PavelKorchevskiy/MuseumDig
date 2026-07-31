using Godot;

public partial class EnergySystem : Node
{
	public static EnergySystem Instance { get; private set; }
	
	// Текущие значения
	private int _currentEnergy;
	private int _maxEnergy = 20;
	private int _energyLevel = 1;
	
	// Восстановление
	private double _regenTimer = 0;
	private const double RegenInterval = 5.0; // 1 энергия каждые 5 секунд
	
	// Стоимость улучшений
	private const int MaxEnergyBaseCost = 100;
	private const int RegenBaseCost = 150;
	private const float CostGrowth = 1.6f;
	private const int MaxUpgradeLevel = 20;
	
	private int _regenLevel = 0;
	
	public override void _Ready()
	{
		Instance = this;
		// При первом запуске — полная энергия
		if (_currentEnergy == 0 && _maxEnergy > 0)
		{
			_currentEnergy = _maxEnergy;
		}
	}
	
	public override void _Process(double delta)
	{
		// Восстановление энергии, если не полная
		if (_currentEnergy < _maxEnergy)
		{
			_regenTimer += delta;
			double interval = GetRegenInterval();
			
			if (_regenTimer >= interval)
			{
				_regenTimer = 0;
				_currentEnergy++;
				SaveSystem.Instance?.MarkDirty();
			}
		}
		else
		{
			_regenTimer = 0; // Сбрасываем таймер при полной энергии
		}
	}
	
	// ===== РАСХОД ЭНЕРГИИ =====
	
	public bool TrySpendEnergy(int amount)
	{
		if (_currentEnergy >= amount)
		{
			_currentEnergy -= amount;
			SaveSystem.Instance?.MarkDirty();
			return true;
		}
		return false;
	}
	
	public void AddEnergy(int amount)
	{
		_currentEnergy = Mathf.Min(_currentEnergy + amount, _maxEnergy);
		SaveSystem.Instance?.MarkDirty();
	}
	
	// ===== ГЕТТЕРЫ =====
	
	public int GetCurrentEnergy() => _currentEnergy;
	public int GetMaxEnergy() => _maxEnergy;
	public int GetEnergyLevel() => _energyLevel;
	public int GetRegenLevel() => _regenLevel;
	
	public float GetEnergyRatio()
	{
		return _maxEnergy > 0 ? (float)_currentEnergy / _maxEnergy : 0f;
	}
	
	// ===== ПАРАМЕТРЫ ВОССТАНОВЛЕНИЯ =====
	
	public double GetRegenInterval()
	{
		// Базовый интервал 5 секунд, уменьшается с уровнем
		return 5.0 / (1.0 + _regenLevel * 0.2);
	}
	
	public int GetRegenPerMinute()
	{
		double interval = GetRegenInterval();
		return (int)(60.0 / interval);
	}
	
	// ===== СТОИМОСТЬ УЛУЧШЕНИЙ =====
	
	public int GetMaxEnergyCost()
	{
		return (int)(MaxEnergyBaseCost * Mathf.Pow(CostGrowth, _energyLevel - 1));
	}
	
	public int GetRegenCost()
	{
		return (int)(RegenBaseCost * Mathf.Pow(CostGrowth, _regenLevel));
	}
	
	// ===== ПОКУПКА УЛУЧШЕНИЙ =====
	
	public bool TryBuyMaxEnergy()
	{
		if (_energyLevel >= MaxUpgradeLevel) return false;
		
		int cost = GetMaxEnergyCost();
		if (Wallet.Instance.SpendCoins(cost))
		{
			_energyLevel++;
			_maxEnergy += 5; // +5 к максимуму за уровень
			GD.Print($"Max energy upgraded to Lv.{_energyLevel}! Max: {_maxEnergy}");
			SaveSystem.Instance?.MarkDirty();
			return true;
		}
		return false;
	}
	
	public bool TryBuyRegen()
	{
		if (_regenLevel >= MaxUpgradeLevel) return false;
		
		int cost = GetRegenCost();
		if (Wallet.Instance.SpendCoins(cost))
		{
			_regenLevel++;
			GD.Print($"Regen upgraded to Lv.{_regenLevel}! Interval: {GetRegenInterval():F2}s");
			SaveSystem.Instance?.MarkDirty();
			return true;
		}
		return false;
	}
	
	// ===== ДЛЯ СОХРАНЕНИЯ =====
	
	public void SetEnergy(int current, int max, int energyLevel, int regenLevel)
	{
		_currentEnergy = current;
		_maxEnergy = max;
		_energyLevel = energyLevel;
		_regenLevel = regenLevel;
	}
}
