using Godot;

public partial class ShopUI : CanvasLayer
{
	private VBoxContainer _container;
	private Button _pickaxeButton;
	private Button _coinButton;
	private Button _fossilButton;
	private Button _maxEnergyButton;
	private Button _regenButton;
	private Button _closeButton;
	
	public override void _Ready()
	{
		_container = GetNode<VBoxContainer>("Container");
		_pickaxeButton = GetNode<Button>("Container/PickaxeButton");
		_coinButton = GetNode<Button>("Container/CoinButton");
		_fossilButton = GetNode<Button>("Container/FossilButton");
		_maxEnergyButton = GetNode<Button>("Container/MaxEnergyButton");
		_regenButton = GetNode<Button>("Container/RegenButton");
		_closeButton = GetNode<Button>("Container/CloseButton");
		
		// Настройка контейнера
		_container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_container.OffsetLeft = 50;
		_container.OffsetTop = 50;
		_container.OffsetRight = -50;
		_container.OffsetBottom = -50;
		_container.AddThemeConstantOverride("separation", 20);
		
		// Подписка на события
		_pickaxeButton.Pressed += OnPickaxePressed;
		_coinButton.Pressed += OnCoinPressed;
		_fossilButton.Pressed += OnFossilPressed;
		_maxEnergyButton.Pressed += OnMaxEnergyPressed;
		_regenButton.Pressed += OnRegenPressed;
		_closeButton.Pressed += OnClosePressed;
		
		UpdateButtons();
	}
	
	public override void _Process(double delta)
	{
		UpdateButtons();
	}
	
	private void UpdateButtons()
	{
		var upgrades = UpgradeSystem.Instance;
		int coins = Wallet.Instance.GetCoins();
		
		// Pickaxe
		int pickaxeCost = upgrades.GetPickaxeCost();
		_pickaxeButton.Text = $"Pickaxe Damage Lv.{upgrades.GetPickaxeLevel()}\n" +
							  $"Damage: {upgrades.GetPickaxeDamage()}\n" +
							  $"Cost: {pickaxeCost} coins";
		_pickaxeButton.Disabled = coins < pickaxeCost || upgrades.GetPickaxeLevel() >= 20;
		
		// Coin Bonus
		int coinCost = upgrades.GetCoinBonusCost();
		_coinButton.Text = $"Coin Bonus Lv.{upgrades.GetCoinBonusLevel()}\n" +
						   $"Reward: {upgrades.GetCoinReward()} per tile\n" +
						   $"Cost: {coinCost} coins";
		_coinButton.Disabled = coins < coinCost || upgrades.GetCoinBonusLevel() >= 20;
		
		// Fossil Chance
		int fossilCost = upgrades.GetFossilChanceCost();
		_fossilButton.Text = $"Fossil Chance Lv.{upgrades.GetFossilChanceLevel()}\n" +
							 $"Chance: {upgrades.GetFossilChance():P1}\n" +
							 $"Cost: {fossilCost} coins";
		_fossilButton.Disabled = coins < fossilCost || upgrades.GetFossilChanceLevel() >= 20;
		
		// Max Energy
		int maxEnergyCost = EnergySystem.Instance.GetMaxEnergyCost();
		_maxEnergyButton.Text = $"Max Energy Lv.{EnergySystem.Instance.GetEnergyLevel()}\n" +
								$"Max: {EnergySystem.Instance.GetMaxEnergy()}\n" +
								$"Cost: {maxEnergyCost} coins";
		_maxEnergyButton.Disabled = coins < maxEnergyCost || EnergySystem.Instance.GetEnergyLevel() >= 20;
		
		// Regen Speed
		int regenCost = EnergySystem.Instance.GetRegenCost();
		_regenButton.Text = $"Regen Speed Lv.{EnergySystem.Instance.GetRegenLevel()}\n" +
							$"Rate: {EnergySystem.Instance.GetRegenPerMinute()}/min\n" +
							$"Cost: {regenCost} coins";
		_regenButton.Disabled = coins < regenCost || EnergySystem.Instance.GetRegenLevel() >= 20;
	}
	
	// ===== ОБРАБОТЧИКИ КНОПОК =====
	
	private void OnPickaxePressed()
	{
		UpgradeSystem.Instance.TryBuyPickaxe();
	}
	
	private void OnCoinPressed()
	{
		UpgradeSystem.Instance.TryBuyCoinBonus();
	}
	
	private void OnFossilPressed()
	{
		UpgradeSystem.Instance.TryBuyFossilChance();
	}
	
	private void OnMaxEnergyPressed()
	{
		EnergySystem.Instance.TryBuyMaxEnergy();
	}
	
	private void OnRegenPressed()
	{
		EnergySystem.Instance.TryBuyRegen();
	}
	
	private void OnClosePressed()
	{
		Visible = false;
	}
}
