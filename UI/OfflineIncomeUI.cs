using Godot;

/// <summary>
/// Попап "С возвращением" с суммой офлайн-дохода и одной зелёной кнопкой-галочкой.
/// Монеты уже начислены в Wallet системой сохранения ДО показа этого окна -
/// кнопка только закрывает попап, деньги при этом никак не пересчитываются.
/// Обычный C#-класс (не нод), как ConfirmOverlay в MobileUI.cs.
/// </summary>
public class OfflineIncomeUI
{
    private readonly ColorRect _root;
    private readonly Label _amountLabel;

    public OfflineIncomeUI(CanvasLayer parent)
    {
        _root = new ColorRect { Color = new Color(0, 0, 0, 0.65f), Visible = false };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Stop;
        parent.AddChild(_root);

        var panel = new PanelContainer();
        panel.AnchorLeft = 0.1f;
        panel.AnchorRight = 0.9f;
        panel.AnchorTop = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.GrowVertical = Control.GrowDirection.Both;

        var style = MobileUI.MakeStyle(MobileUI.PanelColor, 24);
        style.ContentMarginLeft = MobileUI.PanelPadding + 10;
        style.ContentMarginRight = MobileUI.PanelPadding + 10;
        style.ContentMarginTop = MobileUI.PanelPadding + 10;
        style.ContentMarginBottom = MobileUI.PanelPadding + 10;
        panel.AddThemeStyleboxOverride("panel", style);
        _root.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 18);
        panel.AddChild(vbox);

        var title = MobileUI.CreateLabel(
            MobileUI.Tr("ui.offline_income.title", "С возвращением!"),
            MobileUI.TitleFontSize, MobileUI.TitleColor, center: true);
        vbox.AddChild(title);

        var subtitle = MobileUI.CreateLabel(
            MobileUI.Tr("ui.offline_income.subtitle", "Пока вас не было, музей заработал:"),
            MobileUI.BodyFontSize, Colors.White, center: true, wrap: true);
        vbox.AddChild(subtitle);

        _amountLabel = MobileUI.CreateLabel("+0", MobileUI.TitleFontSize + 6, Colors.Gold, center: true);
        vbox.AddChild(_amountLabel);

        var buttonRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddChild(buttonRow);

        var okButton = new Button
        {
            CustomMinimumSize = new Vector2(100, 100),
            FocusMode = Control.FocusModeEnum.None
        };

        var okColor = new Color(0.20f, 0.65f, 0.30f, 1f); // зелёная
        var normal = MobileUI.MakeStyle(okColor, 20);
        var pressed = MobileUI.MakeStyle(okColor.Lightened(0.2f), 20);
        okButton.AddThemeStyleboxOverride("normal", normal);
        okButton.AddThemeStyleboxOverride("hover", normal);
        okButton.AddThemeStyleboxOverride("pressed", pressed);
        okButton.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var check = new Label { Text = "✓", MouseFilter = Control.MouseFilterEnum.Ignore };
        check.AddThemeFontSizeOverride("font_size", 52);
        check.AddThemeColorOverride("font_color", Colors.White);
        center.AddChild(check);
        okButton.AddChild(center);

        okButton.Pressed += Hide;
        buttonRow.AddChild(okButton);
    }

    /// <summary>
    /// Показывает попап с уже начисленной суммой. Деньги начисляются раньше (в SaveSystem),
    /// этот метод только отображает окно.
    /// </summary>
    public void Show(int amount)
    {
        _amountLabel.Text = $"+{amount} монет";
        _root.Visible = true;
    }

    public void Hide()
    {
        _root.Visible = false;
    }
}
