using Godot;

public partial class OfflineRewardUI : CanvasLayer
{
	private Label _timeValueLabel;
	private Label _coinsRewardLabel;
	private Label _energyRewardLabel;
	private Button _collectButton;
	
	public override void _Ready()
	{
		GD.Print("=== OfflineRewardUI._Ready() START ===");
		
		_timeValueLabel = GetNodeOrNull<Label>("RewardPanel/Content/TimeValueLabel");
		_coinsRewardLabel = GetNodeOrNull<Label>("RewardPanel/Content/CoinsRewardLabel");
		_energyRewardLabel = GetNodeOrNull<Label>("RewardPanel/Content/EnergyRewardLabel");
		_collectButton = GetNodeOrNull<Button>("RewardPanel/Content/CollectButton");
		
		if (_collectButton != null)
		{
			_collectButton.Pressed += OnCollectPressed;
		}
		
		GD.Print("=== OfflineRewardUI._Ready() END ===");
	}
	
	// ДОБАВЬТЕ ЭТОТ МЕТОД
	public override void _Process(double delta)
	{
		// Обновляем содержимое каждый кадр, если окно видимо
		if (Visible)
		{
			UpdateDisplay();
		}
	}
	
	private void UpdateDisplay()
	{
		if (OfflineRewardSystem.Instance == null) return;
		if (_timeValueLabel == null || _coinsRewardLabel == null || _energyRewardLabel == null) return;
		
		string timeText = OfflineRewardSystem.Instance.GetFormattedTime();
		int coins = OfflineRewardSystem.Instance.GetOfflineCoins();
		int energy = OfflineRewardSystem.Instance.GetOfflineEnergy();
		
		_timeValueLabel.Text = timeText;
		
		_coinsRewardLabel.Text = coins > 0 ? $"💰 {coins} coins" : "";
		_coinsRewardLabel.Visible = coins > 0;
		
		_energyRewardLabel.Text = energy > 0 ? $"⚡ {energy} energy" : "";
		_energyRewardLabel.Visible = energy > 0;
	}
	
	private void OnCollectPressed()
	{
		GD.Print("✅ CollectButton pressed!");
		
		if (OfflineRewardSystem.Instance != null)
		{
			OfflineRewardSystem.Instance.CollectReward();
		}
		
		Visible = false;
	}
}
