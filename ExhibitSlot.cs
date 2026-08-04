using Godot;
using System.Collections.Generic;

public partial class ExhibitSlot : Node2D
{
    private Sprite2D _sprite;
    private ColorRect _highlight;
    private Label _label;
    
    private string _exhibitedResourceId = null;
    private Quality _exhibitedQuality = Quality.Good;
    private bool _isOccupied = false;
    
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _highlight = GetNode<ColorRect>("Highlight");
        _label = GetNode<Label>("Label");
        
        UpdateVisuals();
    }
    
    public void Initialize(string resourceId, Quality quality)
    {
        _exhibitedResourceId = resourceId;
        _exhibitedQuality = quality;
        _isOccupied = true;
        UpdateVisuals();
    }
    
    public void Clear()
    {
        _exhibitedResourceId = null;
        _exhibitedQuality = Quality.Good;
        _isOccupied = false;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (_isOccupied && !string.IsNullOrEmpty(_exhibitedResourceId))
        {
            var resource = GameData.GetResource(_exhibitedResourceId);
            if (resource != null)
            {
                _label.Text = resource.DisplayName;
                _label.AddThemeFontSizeOverride("font_size", 14);
                _label.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
                
                // Меняем цвет спрайта в зависимости от качества
                _sprite.Modulate = GetQualityColor(_exhibitedQuality);
            }
        }
        else
        {
            _label.Text = "Empty";
            _label.AddThemeFontSizeOverride("font_size", 12);
            _label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            _sprite.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
    }
    
    private Color GetQualityColor(Quality quality)
    {
        return quality switch
        {
            Quality.Damaged => new Color(0.6f, 0.3f, 0.3f),  // Красноватый для поврежденного
            Quality.Good => new Color(0.8f, 0.8f, 0.8f),     // Серый для хорошего
            _ => new Color(0.8f, 0.8f, 0.8f)
        };
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            var globalMousePos = GetGlobalMousePosition();
            var localPos = globalMousePos - GlobalPosition;
            
            // Проверяем, попал ли клик в область слота (радиус 40 пикселей)
            if (localPos.Length() < 40f)
            {
                if (_isOccupied)
                {
                    // Если занято — можно убрать экспонат (опционально)
                    RemoveExhibit();
                }
                else
                {
                    // Если пусто — открываем выбор из инвентаря
                    OpenExhibitSelection();
                }
            }
        }
    }
    
    private void RemoveExhibit()
    {
        if (!_isOccupied || string.IsNullOrEmpty(_exhibitedResourceId)) return;
        
        // Возвращаем предмет в инвентарь
        InventorySystem.Instance.AddItem(_exhibitedResourceId, _exhibitedQuality, 1);
        
        // Убираем из музея
        MuseumSystem.Instance.RemoveExhibit(_exhibitedResourceId, _exhibitedQuality);
        
        GD.Print($"[ExhibitSlot] Removed {_exhibitedResourceId} ({_exhibitedQuality}) from museum");
        
        Clear();
    }
    
    private void OpenExhibitSelection()
    {
        // Получаем доступные для выставки предметы из инвентаря
        var availableItems = GetAvailableExhibits();
        
        if (availableItems.Count == 0)
        {
            GD.Print("[ExhibitSlot] No available items to exhibit");
            return;
        }
        
        // Показываем простой диалог выбора (можно заменить на полноценное UI окно)
        ShowExhibitSelectionDialog(availableItems);
    }
    
    private List<(string ResourceId, Quality Quality, int Amount)> GetAvailableExhibits()
    {
        var result = new List<(string, Quality, int)>();
        
        if (InventorySystem.Instance == null) return result;
        
        // Получаем все предметы из инвентаря
        var inventory = InventorySystem.Instance.GetAllItems();
        
        foreach (var item in inventory)
        {
            if (item.Amount <= 0) continue;
            
            // Проверяем, можно ли выставить этот предмет
            if (MuseumSystem.Instance.CanExhibit(item.ResourceId, item.Quality))
            {
                result.Add((item.ResourceId, item.Quality, item.Amount));
            }
        }
        
        return result;
    }
    
    private void ShowExhibitSelectionDialog(List<(string ResourceId, Quality Quality, int Amount)> items)
    {
        // Создаём простое всплывающее меню с доступными предметами
        var popup = new PopupPanel();
        
        var vbox = new VBoxContainer();
        vbox.OffsetLeft = 20;
        vbox.OffsetTop = 20;
        vbox.OffsetRight = -20;
        vbox.OffsetBottom = -20;
        vbox.AddThemeConstantOverride("separation", 10);
        
        var titleLabel = new Label();
        titleLabel.Text = "Select an exhibit:";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(titleLabel);
        
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(scroll);
        
        var itemsList = new VBoxContainer();
        itemsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        itemsList.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(itemsList);
        
        foreach (var item in items)
        {
            var resource = GameData.GetResource(item.ResourceId);
            if (resource == null) continue;
            
            var button = new Button();
            button.Text = $"{resource.DisplayName} ({item.Quality})";
            button.Pressed += () => OnItemSelected(popup, item.ResourceId, item.Quality);
            itemsList.AddChild(button);
        }
        
        var closeButton = new Button();
        closeButton.Text = "Cancel";
        closeButton.Pressed += () => popup.Hide();
        vbox.AddChild(closeButton);
        
        popup.AddChild(vbox);
        GetTree().Root.AddChild(popup);
        popup.PopupCentered(new Vector2(400, 300));
    }
    
    private void OnItemSelected(PopupPanel popup, string resourceId, Quality quality)
    {
        popup.Hide();
        popup.QueueFree();
        
        // Выставляем предмет
        MuseumSystem.Instance.ExhibitItem(resourceId, quality);
        Initialize(resourceId, quality);
        
        GD.Print($"[ExhibitSlot] Exhibited {resourceId} ({quality})");
    }
}
