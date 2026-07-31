using Godot;
using System.Collections.Generic;

public partial class MuseumUI : CanvasLayer
{
	private VBoxContainer _container;
	private Label _incomeLabel;
	private VBoxContainer _exhibitsList;
	private Button _closeButton;
	
	private string _lastExhibitState = "";
	private bool _initialized = false;
	
	public override void _Ready()
	{
		// Находим все узлы с проверками
		_container = GetNodeOrNull<VBoxContainer>("Container");
		_incomeLabel = GetNodeOrNull<Label>("Container/IncomeLabel");
		_exhibitsList = GetNodeOrNull<VBoxContainer>("Container/ScrollContainer/ExhibitsList");
		_closeButton = GetNodeOrNull<Button>("Container/CloseButton");
		
		// Проверяем, что все узлы найдены
		if (_container == null || _incomeLabel == null || _exhibitsList == null || _closeButton == null)
		{
			GD.PrintErr("MuseumUI: Не все узлы найдены в сцене!");
			GD.PrintErr($"Container: {_container != null}");
			GD.PrintErr($"IncomeLabel: {_incomeLabel != null}");
			GD.PrintErr($"ExhibitsList: {_exhibitsList != null}");
			GD.PrintErr($"CloseButton: {_closeButton != null}");
			return;
		}
		
		// Настройка контейнера
		_container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_container.OffsetLeft = 50;
		_container.OffsetTop = 50;
		_container.OffsetRight = -50;
		_container.OffsetBottom = -50;
		_container.AddThemeConstantOverride("separation", 20);
		
		_closeButton.Pressed += OnClosePressed;
		
		_initialized = true;
		UpdateDisplay();
	}
	
	public override void _Process(double delta)
	{
		if (!_initialized) return;
		
		// Проверяем, что autoload'ы инициализированы
		if (MuseumSystem.Instance == null || FossilInventory.Instance == null)
		{
			return;
		}
		
		UpdateDisplay();
	}
	
	private void UpdateDisplay()
	{
		if (_incomeLabel == null || _exhibitsList == null) return;
		
		// Обновить доход
		int income = MuseumSystem.Instance.GetTotalIncomePerSecond();
		_incomeLabel.Text = $"Income: {income} coins/sec";
		
		// Проверяем, изменилось ли состояние
		string currentState = GetExhibitState();
		if (currentState == _lastExhibitState)
		{
			return;
		}
		_lastExhibitState = currentState;
		
		// Полностью очищаем список
		foreach (var child in _exhibitsList.GetChildren())
		{
			child.Free();
		}
		
		// Показать выставленные экспонаты
		var exhibited = MuseumSystem.Instance.GetExhibitedFossils();
		
		if (exhibited.Count == 0)
		{
			var emptyLabel = new Label();
			emptyLabel.Text = "No exhibits yet";
			emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_exhibitsList.AddChild(emptyLabel);
		}
		else
		{
			var exhibitedLabel = new Label();
			exhibitedLabel.Text = "Current exhibits:";
			exhibitedLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_exhibitsList.AddChild(exhibitedLabel);
			
			foreach (var kvp in exhibited)
			{
				var label = new Label();
				label.Text = $"- {kvp.Key}: +{kvp.Value} coins/sec";
				_exhibitsList.AddChild(label);
			}
		}
		
		// Добавить разделитель
		var separator = new HSeparator();
		_exhibitsList.AddChild(separator);
		
		// Показать доступные для выставления экспонаты
		var allPieces = FossilInventory.Instance.GetAllPieces();
		bool hasAvailable = false;
		
		foreach (var kvp in allPieces)
		{
			string fossilId = kvp.Key;
			if (MuseumSystem.Instance.CanExhibit(fossilId))
			{
				if (!hasAvailable)
				{
					var availableLabel = new Label();
					availableLabel.Text = "Available to exhibit:";
					availableLabel.HorizontalAlignment = HorizontalAlignment.Center;
					_exhibitsList.AddChild(availableLabel);
					hasAvailable = true;
				}
				
				var button = new Button();
				button.Text = $"Exhibit {fossilId}";
				button.Pressed += () => OnExhibitPressed(fossilId);
				_exhibitsList.AddChild(button);
			}
		}
		
		if (!hasAvailable)
		{
			var noAvailableLabel = new Label();
			noAvailableLabel.Text = "No fossils available to exhibit";
			noAvailableLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_exhibitsList.AddChild(noAvailableLabel);
		}
	}

	private string GetExhibitState()
	{
		var exhibited = MuseumSystem.Instance.GetExhibitedFossils();
		var pieces = FossilInventory.Instance.GetAllPieces();
		
		string state = "E:";
		foreach (var kvp in exhibited)
		{
			state += kvp.Key + ",";
		}
		
		state += "|A:";
		foreach (var kvp in pieces)
		{
			if (MuseumSystem.Instance.CanExhibit(kvp.Key))
			{
				state += kvp.Key + ",";
			}
		}
		
		return state;
	}
	private void OnExhibitPressed(string fossilId)
	{
		MuseumSystem.Instance.ExhibitFossil(fossilId);
		GD.Print($"Exhibited {fossilId}!");
		_lastExhibitState = "";
	}
	
	private void OnClosePressed()
	{
		Visible = false;
	}
}
