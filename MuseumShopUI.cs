using Godot;
using System.Collections.Generic;

public partial class MuseumShopUI : CanvasLayer
{
    private VBoxContainer _container;
    private Button _closeButton;
    private VBoxContainer _furnitureSection;
    
    public override void _Ready()
    {
        Layer = 100;
        
        // Создаём контейнер программно
        _container = new VBoxContainer();
        _container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _container.OffsetLeft = 50;
        _container.OffsetTop = 50;
        _container.OffsetRight = -50;
        _container.OffsetBottom = -50;
        _container.AddThemeConstantOverride("separation", 20);
        
        // Оборачиваем в ScrollContainer
        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(scroll);
        scroll.AddChild(_container);
        
        _container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        
        // Заголовок
        var title = new Label();
        title.Text = "🏛️ МЕБЕЛЬ ДЛЯ МУЗЕЯ";
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _container.AddChild(title);
        
        // Секция мебели
        _furnitureSection = new VBoxContainer();
        _furnitureSection.AddThemeConstantOverride("separation", 10);
        _container.AddChild(_furnitureSection);
        
        // Заполняем списком мебели
        if (MuseumSystem.Instance != null)
        {
            var templates = MuseumSystem.Instance.GetAvailableFurnitureTemplates();
            foreach (var template in templates)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 15);
                
                var infoLabel = new Label();
                infoLabel.Text = $"{template.DisplayName} ({template.Size.X}x{template.Size.Y})\nЦена: {template.BuyPrice} монет";
                infoLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(infoLabel);
                
                var buyBtn = new Button();
                buyBtn.Text = "Купить";
                buyBtn.CustomMinimumSize = new Vector2(80, 0);
                
                string typeId = template.TypeId;
                buyBtn.Pressed += () => OnBuyFurniturePressed(typeId);
                
                row.AddChild(buyBtn);
                _furnitureSection.AddChild(row);
            }
        }
        
        // Кнопка закрытия
        _closeButton = new Button();
        _closeButton.Text = "Закрыть";
        _closeButton.CustomMinimumSize = new Vector2(0, 40);
        _closeButton.Pressed += OnClosePressed;
        _container.AddChild(_closeButton);
        
        Visible = false;
    }
    
    public override void _Process(double delta)
    {
        if (Visible) UpdateButtons();
    }
    
    private void UpdateButtons()
    {
        int coins = Wallet.Instance.GetCoins();
        
        if (_furnitureSection != null && MuseumSystem.Instance != null)
        {
            int i = 0;
            foreach (var template in MuseumSystem.Instance.GetAvailableFurnitureTemplates())
            {
                if (i < _furnitureSection.GetChildCount())
                {
                    var row = _furnitureSection.GetChild(i) as HBoxContainer;
                    if (row != null && row.GetChildCount() > 1)
                    {
                        var btn = row.GetChild(1) as Button;
                        if (btn != null)
                        {
                            btn.Disabled = coins < template.BuyPrice;
                        }
                    }
                }
                i++;
            }
        }
    }
    
    private void OnBuyFurniturePressed(string typeId)
    {
        if (MuseumSystem.Instance.TryBuyFurniture(typeId))
        {
            GD.Print($"[MuseumShop] Bought furniture: {typeId}");
            Visible = false;
            
            var pendingFurniture = MuseumSystem.Instance.GetPendingFurniture();
            if (pendingFurniture.Count > 0)
            {
                var furniture = pendingFurniture[pendingFurniture.Count - 1];
                MuseumSystem.Instance.StartPlacementMode(furniture);
                
                var placementUI = GetTree().Root.GetNodeOrNull<PlacementModeUI>("PlacementModeUI");
                if (placementUI == null)
                {
                    placementUI = new PlacementModeUI();
                    placementUI.Name = "PlacementModeUI";
                    GetTree().Root.AddChild(placementUI);
                }
                
                placementUI.StartPlacement(MuseumSystem.Instance.GetCurrentRoom(), furniture);
            }
        }
    }
    
    private void OnClosePressed() => Visible = false;
}