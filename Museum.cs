using Godot;
using System.Collections.Generic;

public partial class Museum : Node2D
{
	private Label _incomeLabel;
	private VBoxContainer _exhibitsList;
	private Button _worldMapButton;
	private Button _digButton;
	private CanvasLayer _worldMap;
	private CanvasLayer _offlineReward;
	
	private string _lastExhibitState = "";
	private bool _offlineRewardShown = false;
	
	public override void _Ready()
	{
		// Настройка MainContainer — растягивается на весь экран с отступами
		var mainContainer = GetNode<VBoxContainer>("UI/MainContainer");
		mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		mainContainer.OffsetLeft = 30;
		mainContainer.OffsetTop = 30;
		mainContainer.OffsetRight = -30;
		mainContainer.OffsetBottom = -30;
		mainContainer.AddThemeConstantOverride("separation", 20);
		
		// Поиск узлов
		_incomeLabel = GetNode<Label>("UI/MainContainer/IncomeLabel");
		_exhibitsList = GetNode<VBoxContainer>("UI/MainContainer/ExhibitsScroll/ExhibitsList");
		_worldMapButton = GetNode<Button>("UI/MainContainer/ButtonsRow/WorldMapButton");
		_digButton = GetNode<Button>("UI/MainContainer/ButtonsRow/DigButton");
		_worldMap = GetNode<CanvasLayer>("UI/WorldMap");
		_offlineReward = GetNodeOrNull<CanvasLayer>("UI/OfflineReward");
		
		// Скрываем карту мира и окно офлайн-наград при запуске сцены музея
		if (_worldMap != null)
		{
			_worldMap.Visible = false;
		}
		if (_offlineReward != null)
		{
			_offlineReward.Visible = false;
		}
		
		_worldMapButton.Pressed += OnWorldMapPressed;
		_digButton.Pressed += OnDigPressed;
		
		UpdateDisplay();
	}
	
	public override void _Process(double delta)
	{
		UpdateDisplay();
		CheckOfflineReward();
	}
	
	private void CheckOfflineReward()
	{
		if (_offlineReward == null || OfflineRewardSystem.Instance == null) return;
		
		bool hasReward = OfflineRewardSystem.Instance.HasReward();
		
		// Показываем окно один раз при запуске сцены музея
		if (hasReward && !_offlineReward.Visible && !_offlineRewardShown)
		{
			GD.Print("✅ Showing offline reward window in Museum!");
			_offlineReward.Visible = true;
			_offlineRewardShown = true;
		}
	}
	
	private void UpdateDisplay()
	{
		if (MuseumSystem.Instance == null || InventorySystem.Instance == null) return;
		
		// Обновляем доход
		int income = MuseumSystem.Instance.GetTotalIncomePerSecond();
		_incomeLabel.Text = $"Income: {income} coins/sec";
		
		// Проверяем, изменилось ли состояние
		string currentState = GetExhibitState();
		if (currentState == _lastExhibitState) return;
		_lastExhibitState = currentState;
		
		// Очищаем список
		foreach (var child in _exhibitsList.GetChildren())
		{
			child.Free();
		}
		
		// Показываем экспонаты
		var exhibited = MuseumSystem.Instance.GetExhibitedItems();
		
		if (exhibited.Count == 0)
		{
			var emptyLabel = new Label();
			emptyLabel.Text = "Your museum is empty.\nGo dig some fossils!";
			emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
			emptyLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
			_exhibitsList.AddChild(emptyLabel);
		}
		else
		{
			// Группируем по коллекциям
			var collections = new Dictionary<string, List<string>>();
			var standalone = new List<string>();
			
			foreach (var kvp in exhibited)
			{
				string resourceId = kvp.Value;
				var resource = GameData.GetResource(resourceId);
				if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
				{
					if (!collections.ContainsKey(fossil.CollectionId))
					{
						collections[fossil.CollectionId] = new List<string>();
					}
					collections[fossil.CollectionId].Add(kvp.Key);
				}
				else
				{
					standalone.Add(kvp.Key);
				}
			}
			
			// Показываем коллекции
			foreach (var col in collections)
			{
				var collection = GameData.GetCollection(col.Key);
				
				var colLabel = new Label();
				colLabel.Text = $"📦 {collection.DisplayName} ({col.Value.Count}/{collection.Pieces.Count})";
				colLabel.AddThemeFontSizeOverride("font_size", 20);
				colLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
				_exhibitsList.AddChild(colLabel);
				
				// Показываем части коллекции
				foreach (var key in col.Value)
				{
					string resourceId = exhibited[key];
					var resource = GameData.GetResource(resourceId);
					var parts = key.Split('_');
					Quality quality = (Quality)int.Parse(parts[parts.Length - 1]);
					
					var itemLabel = new Label();
					itemLabel.Text = $"  - {resource.DisplayName} ({quality})";
					_exhibitsList.AddChild(itemLabel);
				}
			}
			
			// Показываем одиночные экспонаты
			if (standalone.Count > 0)
			{
				var separator = new HSeparator();
				_exhibitsList.AddChild(separator);
				
				var standaloneLabel = new Label();
				standaloneLabel.Text = "🏺 Standalone Exhibits";
				standaloneLabel.AddThemeFontSizeOverride("font_size", 20);
				standaloneLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 1f));
				_exhibitsList.AddChild(standaloneLabel);
				
				foreach (var key in standalone)
				{
					string resourceId = exhibited[key];
					var resource = GameData.GetResource(resourceId);
					var parts = key.Split('_');
					Quality quality = (Quality)int.Parse(parts[parts.Length - 1]);
					
					var itemLabel = new Label();
					itemLabel.Text = $"  - {resource.DisplayName} ({quality})";
					_exhibitsList.AddChild(itemLabel);
				}
			}
		}
	}
	
	private string GetExhibitState()
	{
		var exhibited = MuseumSystem.Instance.GetExhibitedItems();
		string state = "E:";
		foreach (var kvp in exhibited)
		{
			state += kvp.Key + ",";
		}
		return state;
	}
	
	private void OnWorldMapPressed()
	{
		if (_worldMap != null)
		{
			_worldMap.Visible = true;
		}
	}
	
	private void OnDigPressed()
	{
		// Переходим к раскопкам
		GetTree().ChangeSceneToFile("res://DigSite.tscn");
	}
}
