using Godot;
using System;

public partial class SellItemDialog : ConfirmationDialog
{
    private string _resourceId;
    private int _maxAmount;
    private int _pricePerUnit;
    
    private Label _titleLabel;
    private Label _amountLabel;
    private Label _totalPriceLabel;
    private HSlider _amountSlider;
    
    public event Action<string, int> OnSellConfirmed;

    public override void _Ready()
    {
        Title = LocalizationManager.Tr("ui.inventory.sell_dialog_title");
        DialogText = "";
        
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        
        _titleLabel = new Label();
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_titleLabel);
        
        var separator = new HSeparator();
        vbox.AddChild(separator);
        
        _amountLabel = new Label();
        _amountLabel.AddThemeFontSizeOverride("font_size", 14);
        _amountLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_amountLabel);
        
        _amountSlider = new HSlider();
        _amountSlider.MinValue = 1;
        _amountSlider.MaxValue = 1;
        _amountSlider.Step = 1;
        _amountSlider.Value = 1;
        _amountSlider.ValueChanged += OnSliderValueChanged;
        vbox.AddChild(_amountSlider);
        
        _totalPriceLabel = new Label();
        _totalPriceLabel.AddThemeFontSizeOverride("font_size", 16);
        _totalPriceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _totalPriceLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_totalPriceLabel);
        
        AddChild(vbox);
        
        Confirmed += OnConfirmed;
        Canceled += OnCanceled;
    }

    public void Open(string resourceId, int maxAmount, int pricePerUnit)
    {
        _resourceId = resourceId;
        _maxAmount = maxAmount;
        _pricePerUnit = pricePerUnit;
        
        var resource = GameData.GetResource(resourceId);
        if (resource != null)
        {
            _titleLabel.Text = resource.DisplayName;
        }
        
        _amountSlider.MaxValue = maxAmount;
        _amountSlider.Value = 1;
        
        UpdateLabels();
        PopupCentered();
    }

    private void OnSliderValueChanged(double value)
    {
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        int amount = (int)_amountSlider.Value;
        _amountLabel.Text = LocalizationManager.TrFormat("ui.inventory.amount", amount);
        
        int totalPrice = _pricePerUnit * amount;
        _totalPriceLabel.Text = LocalizationManager.TrFormat("ui.inventory.sell_for", amount, totalPrice);
    }

    private void OnConfirmed()
    {
        int amount = (int)_amountSlider.Value;
        OnSellConfirmed?.Invoke(_resourceId, amount);
    }

    private void OnCanceled()
    {
        // Ничего не делаем
    }
}