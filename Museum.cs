using Godot;
using System.Collections.Generic;

public partial class Museum : Node2D
{
    private Label _incomeLabel;
    private VBoxContainer _exhibitsList;
    private Button _worldMapButton;
    private Button _digButton;
    private CanvasLayer _worldMap;
    
    private string _lastExhibitState = "";
    
    public override void _Ready()
    {
        // Настройка MainContainer
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
        
        _worldMapButton.Pressed += OnWorldMapPressed;
        _digButton.Pressed += OnDigPressed;
        
        UpdateDisplay();
    }
    
    public override void _Process(double delta)
    {
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (MuseumSystem.Instance == null || InventorySystem.Instance == null) return;
        
        _incomeLabel.Text = $"Income: {MuseumSystem.Instance.GetTotalIncomePerSecond()} coins/sec";
        
        string currentState = GetExhibitState();
        if (currentState == _lastExhibitState) return;
        _lastExhibitState = currentState;
        
        // Очищаем старые элементы списка
        foreach (var child in _exhibitsList.GetChildren())
        {
            child.Free();
        }
        
        var exhibited = MuseumSystem.Instance.GetExhibitedItems();
        
        // ===== ЧАСТЬ 1: Отображение уже выставленных экспонатов =====
        if (exhibited.Count == 0)
        {
            AddCenteredLabel("Your museum is empty.\nGo dig some fossils!", new Color(0.7f, 0.7f, 0.7f));
        }
        else
        {
            var collections = new Dictionary<string, List<string>>();
            var standalone = new List<string>();
            
            foreach (var kvp in exhibited)
            {
                var resource = GameData.GetResource(kvp.Value);
                if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
                {
                    if (!collections.ContainsKey(fossil.CollectionId)) 
                        collections[fossil.CollectionId] = new List<string>();
                    collections[fossil.CollectionId].Add(kvp.Key);
                }
                else
                {
                    standalone.Add(kvp.Key);
                }
            }
            
            foreach (var col in collections)
            {
                var collection = GameData.GetCollection(col.Key);
                AddCenteredLabel($"📦 {collection.DisplayName} ({col.Value.Count}/{collection.Pieces.Count}) - COMPLETE!", new Color(1f, 0.9f, 0.5f), 22);
                
                foreach (var key in col.Value)
                {
                    CreateExhibitCard(exhibited[key], key);
                }
            }
            
            if (standalone.Count > 0)
            {
                _exhibitsList.AddChild(new HSeparator());
                AddCenteredLabel("🏺 Standalone Exhibits", new Color(0.8f, 0.8f, 1f), 22);
                
                foreach (var key in standalone)
                {
                    CreateExhibitCard(exhibited[key], key);
                }
            }
        }
        
        // ===== ЧАСТЬ 2: Секция "Доступно для выставки" =====
        _exhibitsList.AddChild(new HSeparator());
        AddCenteredLabel("Available to exhibit:", Colors.White, 20);
        
        bool hasAvailable = false;
        foreach (var item in InventorySystem.Instance.GetAllItems())
        {
            if (MuseumSystem.Instance.CanExhibit(item.ResourceId, item.Quality))
            {
                hasAvailable = true;
                var resource = GameData.GetResource(item.ResourceId);
                
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 10);
                
                var nameLabel = new Label();
                string qualityText = resource.HasQuality ? $" ({item.Quality})" : "";
                nameLabel.Text = $"{resource.DisplayName}{qualityText} x{item.Amount}";
                nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(nameLabel);
                
                var exhibitButton = new Button();
                exhibitButton.Text = "Exhibit";
                exhibitButton.CustomMinimumSize = new Vector2(100, 0);
                // Используем замыкание для передачи параметров
                string resId = item.ResourceId;
                Quality q = item.Quality;
                exhibitButton.Pressed += () => OnExhibitPressed(resId, q);
                row.AddChild(exhibitButton);
                
                _exhibitsList.AddChild(row);
            }
        }
        
        if (!hasAvailable)
        {
            AddCenteredLabel("No items available", new Color(0.7f, 0.7f, 0.7f));
        }
    }
    
    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====
    
    private void CreateExhibitCard(string resourceId, string key)
    {
        var resource = GameData.GetResource(resourceId);
        Quality quality = MuseumSystem.Instance.GetQualityFromKey(key);
        int income = MuseumSystem.Instance.CalculateItemIncome(resourceId, key);
        
        var card = new PanelContainer();
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.15f, 0.15f, 0.2f);
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 15;
        style.ContentMarginTop = 10;
        style.ContentMarginRight = 15;
        style.ContentMarginBottom = 10;
        card.AddThemeStyleboxOverride("panel", style);
        
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 15);
        card.AddChild(hbox);
        
        var icon = new Label();
        icon.Text = GetIcon(resource.Type);
        icon.AddThemeFontSizeOverride("font_size", 24);
        icon.CustomMinimumSize = new Vector2(40, 0);
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        hbox.AddChild(icon);
        
        var vbox = new VBoxContainer();
        hbox.AddChild(vbox);
        
        var nameLabel = new Label();
        nameLabel.Text = resource.DisplayName;
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(nameLabel);
        
        var detailLabel = new Label();
        string qText = resource.HasQuality ? $"Quality: {quality}" : "Type: Mineral/Resource";
        detailLabel.Text = $"{qText}  |  +{income} coins/sec";
        detailLabel.Modulate = new Color(0.7f, 1f, 0.7f);
        vbox.AddChild(detailLabel);
        
        _exhibitsList.AddChild(card);
    }
    
    private void AddCenteredLabel(string text, Color color, int fontSize = 16)
    {
        var label = new Label();
        label.Text = text;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Modulate = color;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        _exhibitsList.AddChild(label);
    }
    
    private string GetIcon(ResourceType type)
    {
        return type switch
        {
            ResourceType.Bone => "🦴",
            ResourceType.Tooth => "🦷",
            ResourceType.Gold => "💰",
            ResourceType.Gem => "💎",
            _ => "📦"
        };
    }
    
    private string GetExhibitState()
    {
        string state = "E:";
        foreach (var kvp in MuseumSystem.Instance.GetExhibitedItems()) 
            state += kvp.Key + ",";
        
        state += "|A:";
        foreach (var item in InventorySystem.Instance.GetAllItems())
        {
            if (MuseumSystem.Instance.CanExhibit(item.ResourceId, item.Quality))
                state += $"{item.ResourceId}_{(int)item.Quality}_{item.Amount},";
        }
        return state;
    }
    
    private void OnExhibitPressed(string resourceId, Quality quality)
    {
        MuseumSystem.Instance.ExhibitItem(resourceId, quality);
        _lastExhibitState = ""; // Сброс состояния для принудительного обновления UI
    }
    
    private void OnWorldMapPressed() 
    {
        if (_worldMap != null) _worldMap.Visible = true;
    }
    
    private void OnDigPressed() 
    {
        GetTree().ChangeSceneToFile("res://DigSite.tscn");
    }
}