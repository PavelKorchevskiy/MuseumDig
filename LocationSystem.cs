using Godot;
using System.Collections.Generic;

public partial class LocationSystem : Node
{
	public static LocationSystem Instance { get; private set; }
	
	// Текущая локация
	private string _currentLocationId = "canada";
	
	// Открытые локации (ID -> true/false)
	private HashSet<string> _unlockedLocations = new();
	
	public override void _Ready()
	{
		Instance = this;
		// Канада всегда открыта
		_unlockedLocations.Add("canada");
		GD.Print($"[LocationSystem] Starting in {_currentLocationId}");
	}
	
	// ===== ГЕТТЕРЫ =====
	
	public string GetCurrentLocationId() => _currentLocationId;
	
	public LocationDefinition GetCurrentLocation()
	{
		return GameData.GetLocation(_currentLocationId);
	}
	
	public bool IsLocationUnlocked(string locationId)
	{
		return _unlockedLocations.Contains(locationId);
	}
	
	public List<LocationDefinition> GetAllLocations()
	{
		return GameData.GetAllLocations();
	}
	
	// ===== ПЕРЕКЛЮЧЕНИЕ ЛОКАЦИИ =====
	
	public bool TrySetCurrentLocation(string locationId)
	{
		if (!IsLocationUnlocked(locationId))
		{
			GD.PrintErr($"[LocationSystem] Location '{locationId}' is locked!");
			return false;
		}
		
		if (_currentLocationId == locationId) return true;
		
		_currentLocationId = locationId;
		var location = GetCurrentLocation();
		GD.Print($"[LocationSystem] Switched to {location.DisplayName}");
		SaveSystem.Instance?.MarkDirty();
		
		// Сигнализируем, что нужно перегенерировать сетку
		EmitSignal(SignalName.LocationChanged);
		
		return true;
	}
	
	// ===== ОТКРЫТИЕ ЛОКАЦИИ =====
	
	public bool TryUnlockLocation(string locationId)
	{
		if (IsLocationUnlocked(locationId))
		{
			GD.Print($"[LocationSystem] Location '{locationId}' already unlocked");
			return false;
		}
		
		var location = GameData.GetLocation(locationId);
		if (location == null) return false;
		
		if (!Wallet.Instance.SpendCoins(location.UnlockCost))
		{
			GD.PrintErr($"[LocationSystem] Not enough coins to unlock {location.DisplayName}");
			return false;
		}
		
		_unlockedLocations.Add(locationId);
		GD.Print($"[LocationSystem] Unlocked {location.DisplayName} for {location.UnlockCost} coins");
		SaveSystem.Instance?.MarkDirty();
		
		return true;
	}
	
	// ===== СИГНАЛЫ =====
	
	[Signal]
	public delegate void LocationChangedEventHandler();
	
	// ===== ДЛЯ СОХРАНЕНИЯ =====
	
	public LocationSaveData GetSaveData()
{
	return new LocationSaveData
	{
		CurrentLocationId = _currentLocationId,
		UnlockedLocations = new List<string>(_unlockedLocations)
	};
}

public void LoadFromSaveData(LocationSaveData data)
{
	if (data == null) return;
	
	_currentLocationId = data.CurrentLocationId;
	
	_unlockedLocations.Clear();
	if (data.UnlockedLocations != null)
	{
		foreach (var locationId in data.UnlockedLocations)
		{
			_unlockedLocations.Add(locationId);
		}
	}
	
	// Канада всегда открыта
	_unlockedLocations.Add("canada");
	
	GD.Print($"[LocationSystem] Loaded: current={_currentLocationId}, unlocked={_unlockedLocations.Count}");
}
}
