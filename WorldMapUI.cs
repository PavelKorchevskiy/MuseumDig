using Godot;
using System.Collections.Generic;

public partial class WorldMapUI : CanvasLayer
{
	private VBoxContainer _locationsList;
	private Button _closeButton;
	
	// Кэш строк UI
	private Dictionary<string, Control> _rowCache = new();
	
	public override void _Ready()
	{
		_locationsList = GetNode<VBoxContainer>("MainPanel/Content/ScrollContainer/LocationsList");
		_closeButton = GetNode<Button>("MainPanel/Content/CloseButton");
		
		_closeButton.Pressed += OnClosePressed;
	}
	
	public override void _Process(double delta)
	{
		if (!Visible) return;
		if (LocationSystem.Instance == null) return;
		
		UpdateDisplay();
	}
	
	private void UpdateDisplay()
	{
		var allLocations = LocationSystem.Instance.GetAllLocations();
		var currentId = LocationSystem.Instance.GetCurrentLocationId();
		
		// Определяем, какие строки нужны
		var neededIds = new HashSet<string>();
		foreach (var location in allLocations)
		{
			neededIds.Add(location.Id);
		}
		
		// Удаляем лишние строки
		var toRemove = new List<string>();
		foreach (var kvp in _rowCache)
		{
			if (!neededIds.Contains(kvp.Key))
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
		foreach (var location in allLocations)
		{
			if (!_rowCache.ContainsKey(location.Id))
			{
				var row = CreateLocationRow(location);
				_locationsList.AddChild(row);
				_rowCache[location.Id] = row;
			}
			
			UpdateLocationRow(_rowCache[location.Id], location, location.Id == currentId);
		}
	}
	
	private Control CreateLocationRow(LocationDefinition location)
	{
		// Контейнер строки
		var row = new PanelContainer();
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0.15f, 0.15f, 0.2f);
		styleBox.CornerRadiusTopLeft = 5;
		styleBox.CornerRadiusTopRight = 5;
		styleBox.CornerRadiusBottomLeft = 5;
		styleBox.CornerRadiusBottomRight = 5;
		styleBox.ContentMarginLeft = 10;
		styleBox.ContentMarginTop = 8;
		styleBox.ContentMarginRight = 10;
		styleBox.ContentMarginBottom = 8;
		row.AddThemeStyleboxOverride("panel", styleBox);
		
		var content = new HBoxContainer();
		content.Name = "Content";
		content.AddThemeConstantOverride("separation", 15);
		row.AddChild(content);
		
		// Название локации
		var nameLabel = new Label();
		nameLabel.Name = "NameLabel";
		nameLabel.CustomMinimumSize = new Vector2(150, 0);
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		content.AddChild(nameLabel);
		
		// Описание
		var descLabel = new Label();
		descLabel.Name = "DescLabel";
		descLabel.CustomMinimumSize = new Vector2(200, 0);
		descLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		descLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
		content.AddChild(descLabel);
		
		// Стоимость открытия
		var costLabel = new Label();
		costLabel.Name = "CostLabel";
		costLabel.CustomMinimumSize = new Vector2(100, 0);
		costLabel.HorizontalAlignment = HorizontalAlignment.Right;
		content.AddChild(costLabel);
		
		// Кнопка действия
		var actionButton = new Button();
		actionButton.Name = "ActionButton";
		actionButton.CustomMinimumSize = new Vector2(120, 35);
		content.AddChild(actionButton);
		
		return row;
	}
	
	private void UpdateLocationRow(Control row, LocationDefinition location, bool isCurrent)
	{
		var nameLabel = row.GetNode<Label>("Content/NameLabel");
		var descLabel = row.GetNode<Label>("Content/DescLabel");
		var costLabel = row.GetNode<Label>("Content/CostLabel");
		var actionButton = row.GetNode<Button>("Content/ActionButton");
		
		bool isUnlocked = LocationSystem.Instance.IsLocationUnlocked(location.Id);
		
		// Название
		nameLabel.Text = isUnlocked ? location.DisplayName : $"??? ({location.DisplayName})";
		nameLabel.Modulate = isUnlocked ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
		
		// Описание
		descLabel.Text = isUnlocked ? location.Description : "Locked";
		
		// Стоимость
		if (!isUnlocked)
		{
			costLabel.Text = $"{location.UnlockCost}💰";
		}
		else
		{
			costLabel.Text = "";
		}
		
		// Кнопка
		if (!isUnlocked)
		{
			actionButton.Text = "Unlock";
			actionButton.Disabled = Wallet.Instance.GetCoins() < location.UnlockCost;
			DisconnectButton(actionButton); // Очищаем старые подписки
			actionButton.Pressed += () => OnUnlockPressed(location.Id);
		}
		else if (isCurrent)
		{
			actionButton.Text = "✓ Current";
			actionButton.Disabled = true;
			DisconnectButton(actionButton);
		}
		else
		{
			actionButton.Text = "Travel";
			actionButton.Disabled = false;
			DisconnectButton(actionButton);
			actionButton.Pressed += () => OnTravelPressed(location.Id);
		}
		
		// Подсветка текущей локации
		var panel = row as PanelContainer;
		if (panel != null)
		{
			var styleBox = new StyleBoxFlat();
			styleBox.CornerRadiusTopLeft = 5;
			styleBox.CornerRadiusTopRight = 5;
			styleBox.CornerRadiusBottomLeft = 5;
			styleBox.CornerRadiusBottomRight = 5;
			styleBox.ContentMarginLeft = 10;
			styleBox.ContentMarginTop = 8;
			styleBox.ContentMarginRight = 10;
			styleBox.ContentMarginBottom = 8;
			
			if (isCurrent)
			{
				styleBox.BgColor = new Color(0.2f, 0.3f, 0.2f); // Зеленоватый
				styleBox.BorderColor = new Color(0.4f, 0.8f, 0.4f);
				styleBox.BorderWidthLeft = 2;
				styleBox.BorderWidthTop = 2;
				styleBox.BorderWidthRight = 2;
				styleBox.BorderWidthBottom = 2;
			}
			else
			{
				styleBox.BgColor = new Color(0.15f, 0.15f, 0.2f);
			}
			
			panel.AddThemeStyleboxOverride("panel", styleBox);
		}
	}
	
	private void OnUnlockPressed(string locationId)
	{
		if (LocationSystem.Instance.TryUnlockLocation(locationId))
		{
			GD.Print($"[WorldMapUI] Unlocked {locationId}!");
		}
	}
	
	private void OnTravelPressed(string locationId)
	{
		if (LocationSystem.Instance.TrySetCurrentLocation(locationId))
		{
			GD.Print($"[WorldMapUI] Traveling to {locationId}!");
			Visible = false; // Закрываем карту
			
			// Возвращаемся на сцену музея
			GetTree().ChangeSceneToFile("res://Museum.tscn");
		}
	}
	
	// Вспомогательный метод для отключения всех обработчиков кнопки
private void DisconnectButton(Button button)
{
	// Отключаем все сигналы "pressed" от этой кнопки
	var connections = button.GetSignalConnectionList("pressed");
	foreach (var conn in connections)
	{
		var callable = conn["callable"].AsCallable();
		button.Disconnect("pressed", callable);
	}
}
	
	private void OnClosePressed()
	{
		Visible = false;
	}
}
