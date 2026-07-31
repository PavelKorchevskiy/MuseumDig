using Godot;

public partial class Wallet : Node
{
	public static Wallet Instance { get; private set; }
	
	private int _coins = 0;
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public void AddCoins(int amount)
	{
		_coins += amount;
		GD.Print($"Added {amount} coins. Total: {_coins}");
		SaveSystem.Instance?.MarkDirty(); // Помечаем, что нужно сохранить
	}
	
	public bool SpendCoins(int amount)
	{
		if (_coins >= amount)
		{
			_coins -= amount;
			GD.Print($"Spent {amount} coins. Remaining: {_coins}");
			SaveSystem.Instance?.MarkDirty();
			return true;
		}
		GD.Print($"Not enough coins! Need {amount}, have {_coins}");
		return false;
	}
	
	public int GetCoins() => _coins;
	
	// Для загрузки из сохранения
	public void SetCoins(int coins)
	{
		_coins = coins;
		GD.Print($"Loaded coins: {_coins}");
	}
}
