using Godot;
using System.Collections.Generic;

public partial class InventoryUI : CanvasLayer
{
	private Label _totalValueLabel;
	private VBoxContainer _itemsList;
	private Button _sellAllButton;
	private Button _closeButton;
	
	// Кэш строк UI (чтобы не пересоздавать каждый кадр)
	private Dictionary<string, Control> _rowCache = new();
	
	public override void _Ready()
	{
		_totalValueLabel = GetNode<Label>("MainPanel/Content/TotalValueLabel");
		_itemsList = GetNode<VBoxContainer>("MainPanel/Content/ScrollContainer/ItemsList");
		_sellAllButton = GetNode<Button>("MainPanel/Content/ButtonsRow/SellAllButton");
		_closeButton = GetNode<Button>("MainPanel/Content/ButtonsRow/CloseButton");
		
		_sellAllButton.Pressed += OnSellAllPressed;
		_closeButton.Pressed += OnClosePressed;
	}
	
	public override void _Process(double delta)
	{
		if (!Visible) return;
		if (InventorySystem.Instance == null) return;
		
		UpdateDisplay();
	}
	
	private void UpdateDisplay()
	{
		var allItems = InventorySystem.Instance.GetAllItems();
		
		// Подсчёт общей стоимости
		int totalValue = 0;
		foreach (var item in allItems)
		{
			totalValue += CalculateItemValue(item) * item.Amount;
		}
		_totalValueLabel.Text = $"Total value: {totalValue} coins";
		
		// Определяем, какие строки нужны
		var neededKeys = new HashSet<string>();
		foreach (var item in allItems)
		{
			string key = $"{item.ResourceId}_{(int)item.Quality}";
			neededKeys.Add(key);
		}
		
		// Удаляем лишние строки
		var toRemove = new List<string>();
		foreach (var kvp in _rowCache)
		{
			if (!neededKeys.Contains(kvp.Key))
			{
				kvp.Value.QueueFree();
				toRemove.Add(kvp.Key);
			}
		}
		foreach (var key in toRemove)
		{
			_rowCache.Remove(key);
		}
		
		// Создаём/обновляем строки
		foreach (var item in allItems)
		{
			string key = $"{item.ResourceId}_{(int)item.Quality}";
			
			if (!_rowCache.ContainsKey(key))
			{
				var row = CreateItemRow(item);
				_itemsList.AddChild(row);
				_rowCache[key] = row;
			}
			
			UpdateItemRow(_rowCache[key], item);
		}
	}
	
	// ===== СОЗДАНИЕ СТРОКИ =====
	
	private Control CreateItemRow(FoundItem item)
	{
		var resource = GameData.GetResource(item.ResourceId);
		
		// Контейнер строки
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		
		// Иконка (пока текстовая)
		var iconLabel = new Label();
		iconLabel.CustomMinimumSize = new Vector2(40, 0);
		iconLabel.Text = GetResourceIcon(resource.Type);
		iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
		row.AddChild(iconLabel);
		
		// Название + качество
		var nameLabel = new Label();
		nameLabel.Name = "NameLabel";
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(nameLabel);
		
		// Количество
		var amountLabel = new Label();
		amountLabel.Name = "AmountLabel";
		amountLabel.CustomMinimumSize = new Vector2(60, 0);
		amountLabel.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(amountLabel);
		
		// Цена за штуку
		var priceLabel = new Label();
		priceLabel.Name = "PriceLabel";
		priceLabel.CustomMinimumSize = new Vector2(100, 0);
		priceLabel.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(priceLabel);
		
		// Кнопка "Продать 1"
		var sellOneButton = new Button();
		sellOneButton.Name = "SellOneButton";
		sellOneButton.Text = "Sell 1";
		sellOneButton.CustomMinimumSize = new Vector2(70, 0);
		sellOneButton.Pressed += () => OnSellOnePressed(item.ResourceId, item.Quality);
		row.AddChild(sellOneButton);
		
		// Кнопка "Продать всё"
		var sellAllOfItemButton = new Button();
		sellAllOfItemButton.Name = "SellAllOfItemButton";
		sellAllOfItemButton.Text = "Sell All";
		sellAllOfItemButton.CustomMinimumSize = new Vector2(80, 0);
		sellAllOfItemButton.Pressed += () => OnSellAllOfItemPressed(item.ResourceId, item.Quality);
		row.AddChild(sellAllOfItemButton);
		
		return row;
	}
	
	// ===== ОБНОВЛЕНИЕ СТРОКИ =====
	
	private void UpdateItemRow(Control row, FoundItem item)
	{
		var resource = GameData.GetResource(item.ResourceId);
		if (resource == null) return;
		
		var nameLabel = row.GetNode<Label>("NameLabel");
		var amountLabel = row.GetNode<Label>("AmountLabel");
		var priceLabel = row.GetNode<Label>("PriceLabel");
		
		// Название с цветом качества
		string qualityText = resource.HasQuality ? $" [{item.Quality}]" : "";
		nameLabel.Text = $"{resource.DisplayName}{qualityText}";
		nameLabel.Modulate = GetQualityColor(item.Quality, resource.HasQuality);
		
		// Количество
		amountLabel.Text = $"x{item.Amount}";
		
		// Цена за штуку
		int pricePerUnit = CalculateItemValue(item);
		priceLabel.Text = $"{pricePerUnit}💰";
	}
	
	// ===== РАСЧЁТ СТОИМОСТИ =====
	
	private int CalculateItemValue(FoundItem item)
	{
		var resource = GameData.GetResource(item.ResourceId);
		if (resource == null) return 0;
		
		float multiplier = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(item.Quality);
		return (int)(resource.BaseSellPrice * multiplier);
	}
	
	// ===== УТИЛИТЫ =====
	
	private string GetResourceIcon(ResourceType type)
	{
		return type switch
		{
			ResourceType.Bone => "🦴",
			ResourceType.Tooth => "🦷",
			ResourceType.Gold => "💰",
			ResourceType.Gem => "💎",
			_ => "❓"
		};
	}
	
	private Color GetQualityColor(Quality quality, bool hasQuality)
	{
		if (!hasQuality) return Colors.White;
		
		return quality switch
		{
			Quality.Damaged => new Color(1f, 0.4f, 0.4f),  // Красноватый
			Quality.Good => new Color(0.4f, 1f, 0.4f),     // Зеленоватый
			_ => Colors.White
		};
	}
	
	// ===== ОБРАБОТЧИКИ =====
	
	private void OnSellOnePressed(string resourceId, Quality quality)
	{
		int earned = InventorySystem.Instance.SellItem(resourceId, quality, 1);
		if (earned > 0)
		{
			GD.Print($"[InventoryUI] Sold 1 for {earned} coins");
		}
	}
	
	private void OnSellAllOfItemPressed(string resourceId, Quality quality)
	{
		var item = InventorySystem.Instance.GetItem(resourceId, quality);
		if (item != null && item.Amount > 0)
		{
			int earned = InventorySystem.Instance.SellItem(resourceId, quality, item.Amount);
			GD.Print($"[InventoryUI] Sold all for {earned} coins");
		}
	}
	
	private void OnSellAllPressed()
	{
		var allItems = InventorySystem.Instance.GetAllItems();
		int totalEarned = 0;
		
		// Копируем список, чтобы не модифицировать во время итерации
		var itemsCopy = new List<FoundItem>(allItems);
		
		foreach (var item in itemsCopy)
		{
			int earned = InventorySystem.Instance.SellItem(item.ResourceId, item.Quality, item.Amount);
			totalEarned += earned;
		}
		
		GD.Print($"[InventoryUI] Sold everything for {totalEarned} coins");
	}
	
	private void OnClosePressed()
	{
		Visible = false;
	}
}
