using Godot;

public partial class Tile : ColorRect
{
	public int Hp = 3;
	public int MaxHp = 3;
	
	public override void _Input(InputEvent @event)
{
	if (@event is InputEventMouseButton mouseEvent 
		&& mouseEvent.Pressed 
		&& mouseEvent.ButtonIndex == MouseButton.Left)
	{
		if (GetRect().HasPoint(mouseEvent.Position))
		{
			// Проверяем энергию
			if (EnergySystem.Instance.TrySpendEnergy(1))
			{
				TakeDamage(UpgradeSystem.Instance.GetPickaxeDamage());
				GetViewport().SetInputAsHandled();
			}
			else
			{
				GD.Print("Not enough energy!");
			}
		}
	}
}
	
	public void TakeDamage(int damage)
	{
		Hp -= damage;
		UpdateVisual();
		GD.Print($"Tile damaged! HP: {Hp}/{MaxHp}");
		
		if (Hp <= 0)
		{
			GD.Print("Tile destroyed!");
			Wallet.Instance.AddCoins(UpgradeSystem.Instance.GetCoinReward());
			TryDropFossil();
			QueueFree();
		}
	}
	
	private void TryDropFossil()
	{
		if (GD.Randf() < UpgradeSystem.Instance.GetFossilChance())
		{
			DropFossil();
		}
	}
	
	private void DropFossil()
{
	string fossilId = "dino_bone";
	int pieceIndex = GD.RandRange(0, 3);
	
	bool added = FossilInventory.Instance.AddPiece(fossilId, pieceIndex);
	
	if (added)
	{
		GD.Print($"✓ New fossil piece: {fossilId} part {pieceIndex}");
	}
	else
	{
		// Дубликат — даём бонусные монеты
		int bonusCoins = 10;
		Wallet.Instance.AddCoins(bonusCoins);
		GD.Print($"💰 Duplicate piece! Bonus: {bonusCoins} coins");
	}
}
	
	private void UpdateVisual()
	{
		float ratio = (float)Hp / MaxHp;
		Color = new Color(0.6f * ratio, 0.4f * ratio, 0.2f);
	}
}
