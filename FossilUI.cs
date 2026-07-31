using Godot;
using System.Collections.Generic;

public partial class FossilUI : CanvasLayer
{
	private Label _fossilLabel;
	private Label _coinsLabel;
	private Label _energyLabel;
	private ProgressBar _energyBar;
	private Button _shopButton;
	private Button _museumButton;
	private CanvasLayer _shop;
	private CanvasLayer _museum;
	private CanvasLayer _offlineReward;
	private Button _saveQuitButton;
	
	public override void _Ready()
{
	// ===== НАСТРОЙКА ФОНА =====
	var background = GetNodeOrNull<PanelContainer>("Background");
	if (background != null)
	{
		background.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		background.OffsetLeft = 10;
		background.OffsetTop = 10;
		background.CustomMinimumSize = new Vector2(260, 0);
		
		// Полупрозрачный фон
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0.6f);
		styleBox.CornerRadiusTopLeft = 8;
		styleBox.CornerRadiusTopRight = 8;
		styleBox.CornerRadiusBottomLeft = 8;
		styleBox.CornerRadiusBottomRight = 8;
		styleBox.ContentMarginLeft = 10;
		styleBox.ContentMarginTop = 10;
		styleBox.ContentMarginRight = 10;
		styleBox.ContentMarginBottom = 10;
		background.AddThemeStyleboxOverride("panel", styleBox);
	}
	
	// ===== НАСТРОЙКА КОНТЕЙНЕРА =====
	var mainContainer = GetNodeOrNull<VBoxContainer>("Background/MainContainer");
	if (mainContainer != null)
	{
		mainContainer.AddThemeConstantOverride("separation", 10);
	}
	
	// ===== ПОИСК УЗЛОВ (с Background/ в начале!) =====
	_fossilLabel = GetNodeOrNull<Label>("Background/MainContainer/TopSection/FossilLabel");
	_coinsLabel = GetNodeOrNull<Label>("Background/MainContainer/TopSection/CoinsLabel");
	_energyLabel = GetNodeOrNull<Label>("Background/MainContainer/EnergySection/EnergyLabel");
	_energyBar = GetNodeOrNull<ProgressBar>("Background/MainContainer/EnergySection/EnergyBar");
	_shopButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/ShopButton");
	_museumButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/MuseumButton");
	_shop = GetNodeOrNull<CanvasLayer>("Shop");
	_museum = GetNodeOrNull<CanvasLayer>("Museum");
	_offlineReward = GetNodeOrNull<CanvasLayer>("OfflineReward");
	
	if (_offlineReward == null)
	{
		GD.PrintErr("⚠️ OfflineReward not found! Trying alternative paths...");
		
		// Попробуем найти рекурсивно
		_offlineReward = FindChildRecursive<CanvasLayer>(this, "OfflineReward");
		
		if (_offlineReward != null)
		{
			GD.Print("✅ Found OfflineReward recursively!");
		}
		else
		{
			GD.PrintErr("❌ OfflineReward not found anywhere!");
		}
	}
	else
	{
		GD.Print("✅ OfflineReward found directly");
	}
	
	// ===== ПРОВЕРКА =====
	if (_fossilLabel == null) GD.PrintErr("⚠️ FossilLabel not found!");
	if (_coinsLabel == null) GD.PrintErr("⚠️ CoinsLabel not found!");
	if (_energyLabel == null) GD.PrintErr("⚠️ EnergyLabel not found!");
	if (_energyBar == null) GD.PrintErr("⚠️ EnergyBar not found!");
	if (_shopButton == null) GD.PrintErr("⚠️ ShopButton not found!");
	if (_museumButton == null) GD.PrintErr("⚠️ MuseumButton not found!");
	if (_shop == null) GD.PrintErr("⚠️ Shop not found!");
	if (_museum == null) GD.PrintErr("⚠️ Museum not found!");
	
	// ===== ПОДПИСКА =====
	if (_shopButton != null) _shopButton.Pressed += OnShopPressed;
	if (_museumButton != null) _museumButton.Pressed += OnMuseumPressed;

	_saveQuitButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/SaveQuitButton");
if (_saveQuitButton != null) 
{
	_saveQuitButton.Pressed += OnSaveQuitPressed;
}
}
	
	private void OnShopPressed()
	{
		if (_shop != null)
		{
			_shop.Visible = true;
		}
	}
	
	private void OnMuseumPressed()
	{
		if (_museum != null)
		{
			_museum.Visible = true;
		}
	}
	
	public override void _Process(double delta)
{
	UpdateFossilDisplay();
	UpdateCoinsDisplay();
	UpdateEnergyDisplay();
	CheckOfflineReward();
}

private bool _offlineRewardShown = false; // Флаг, чтобы показывать окно только один раз

private void CheckOfflineReward()
{
	if (_offlineReward == null || OfflineRewardSystem.Instance == null) return;
	
	bool hasReward = OfflineRewardSystem.Instance.HasReward();
	
	// Показываем окно один раз
	if (hasReward && !_offlineReward.Visible && !_offlineRewardShown)
	{
		GD.Print("✅ Showing offline reward window!");
		_offlineReward.Visible = true;
		_offlineRewardShown = true;
	}
}
	
	private void UpdateFossilDisplay()
	{
		if (_fossilLabel == null) return; // ← ЗАЩИТА
		
		var pieces = FossilInventory.Instance.GetAllPieces();
		
		if (pieces.Count == 0)
		{
			_fossilLabel.Text = "No fossils found yet";
			return;
		}
		
		string text = "Fossils:\n";
		foreach (var kvp in pieces)
		{
			string fossilId = kvp.Key;
			int count = kvp.Value.Count;
			string status = FossilInventory.Instance.IsFossilComplete(fossilId) ? "✓" : "";
			text += $"- {fossilId}: {count}/4 {status}\n";
		}
		
		_fossilLabel.Text = text;
	}
	
	private void UpdateCoinsDisplay()
	{
		if (_coinsLabel == null) return; // ← ЗАЩИТА
		
		int coins = Wallet.Instance.GetCoins();
		int income = MuseumSystem.Instance.GetTotalIncomePerSecond();
		_coinsLabel.Text = $"Coins: {coins}\nIncome: {income}/sec";
	}
	
	private void UpdateEnergyDisplay()
	{
		if (_energyLabel == null || _energyBar == null) return; // ← ЗАЩИТА
		if (EnergySystem.Instance == null) return;
		
		int current = EnergySystem.Instance.GetCurrentEnergy();
		int max = EnergySystem.Instance.GetMaxEnergy();
		float ratio = EnergySystem.Instance.GetEnergyRatio();
		
		_energyLabel.Text = $"Energy: {current}/{max}";
		_energyBar.Value = ratio * 100f;
		
		// Меняем цвет бара в зависимости от количества энергии
		if (ratio > 0.5f)
			_energyBar.Modulate = new Color(0.3f, 0.8f, 0.3f); // Зелёный
		else if (ratio > 0.2f)
			_energyBar.Modulate = new Color(0.9f, 0.8f, 0.2f); // Жёлтый
		else
			_energyBar.Modulate = new Color(0.9f, 0.3f, 0.3f); // Красный
	}

private void OnSaveQuitPressed()
{
	GD.Print("Manual save and quit triggered!");
	SaveSystem.Instance.ForceSaveAndQuit();
}

private T FindChildRecursive<T>(Node parent, string name) where T : Node
{
	foreach (var child in parent.GetChildren())
	{
		if (child.Name == name && child is T result)
			return result;
		
		var found = FindChildRecursive<T>(child, name);
		if (found != null)
			return found;
	}
	return null;
}
}
