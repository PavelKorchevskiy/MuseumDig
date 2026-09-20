using Godot;
using System.Collections.Generic;

public partial class ProgressUI : CanvasLayer
{
    public static ProgressUI Instance { get; private set; }

    private enum Tab { SkillTree, UnlockedSkeletons, UnlockedFindings }
    private Tab _currentTab = Tab.SkillTree;

    private Label _availablePointsLabel;
    private Control _tabContent;
    private Button _resetButton;

    private Button _tabSkillTreeButton;
    private Button _tabSkeletonsButton;
    private Button _tabFindingsButton;

    private SkillTreeTab _skillTreeTab;
    private UnlockedSkeletonsTab _skeletonsTab;
    private UnlockedFindingsTab _findingsTab;

    public override void _Ready()
    {
        Instance = this;
        Layer = 102;
        Visible = false;

        CreateMainPanel();
        SwitchTab(Tab.SkillTree);
    }

    private void CreateMainPanel()
    {
        var panel = new PanelContainer();
        panel.Name = "MainPanel";
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        style.CornerRadiusTopLeft = 10;
        style.CornerRadiusTopRight = 10;
        style.CornerRadiusBottomLeft = 10;
        style.CornerRadiusBottomRight = 10;
        panel.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        panel.AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 15);
        margin.AddChild(mainVBox);

        // Заголовок с очками и кнопкой сброса
        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 20);

        var titleLabel = new Label();
        titleLabel.Text = LocalizationManager.Tr("ui.progress.title");
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        headerHBox.AddChild(titleLabel);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerHBox.AddChild(spacer);

        _availablePointsLabel = new Label();
        _availablePointsLabel.AddThemeFontSizeOverride("font_size", 18);
        _availablePointsLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.4f));
        headerHBox.AddChild(_availablePointsLabel);

        _resetButton = new Button();
        _resetButton.Text = LocalizationManager.Tr("ui.progress.reset_skills");
        _resetButton.CustomMinimumSize = new Vector2(150, 0);
        _resetButton.Pressed += OnResetPressed;
        headerHBox.AddChild(_resetButton);

        var closeButton = new Button();
        closeButton.Text = "X";
        closeButton.CustomMinimumSize = new Vector2(40, 40);
        closeButton.Pressed += () => Visible = false;
        headerHBox.AddChild(closeButton);

        mainVBox.AddChild(headerHBox);

        // Вкладки
        var tabsContainer = new HBoxContainer();
        tabsContainer.AddThemeConstantOverride("separation", 10);
        mainVBox.AddChild(tabsContainer);

        _tabSkillTreeButton = CreateTabButton("ui.progress.tab_skills", Tab.SkillTree);
        _tabSkeletonsButton = CreateTabButton("ui.progress.tab_skeletons", Tab.UnlockedSkeletons);
        _tabFindingsButton = CreateTabButton("ui.progress.tab_findings", Tab.UnlockedFindings);

        tabsContainer.AddChild(_tabSkillTreeButton);
        tabsContainer.AddChild(_tabSkeletonsButton);
        tabsContainer.AddChild(_tabFindingsButton);

        // Контент вкладок
        _tabContent = new Control();
        _tabContent.Name = "TabContent";
        _tabContent.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainVBox.AddChild(_tabContent);

        // Создаём вкладки
        _skillTreeTab = new SkillTreeTab();
        _skeletonsTab = new UnlockedSkeletonsTab();
        _findingsTab = new UnlockedFindingsTab();

        _tabContent.AddChild(_skillTreeTab);
        _tabContent.AddChild(_skeletonsTab);
        _tabContent.AddChild(_findingsTab);

        AddChild(panel);
    }

    private Button CreateTabButton(string localizationKey, Tab tab)
    {
        var button = new Button();
        button.Text = LocalizationManager.Tr(localizationKey);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.CustomMinimumSize = new Vector2(0, 40);
        button.Pressed += () => SwitchTab(tab);
        return button;
    }

     private void SwitchTab(Tab newTab)
    {
        _currentTab = newTab;

        _tabSkillTreeButton.Disabled = (_currentTab == Tab.SkillTree);
        _tabSkeletonsButton.Disabled = (_currentTab == Tab.UnlockedSkeletons);
        _tabFindingsButton.Disabled = (_currentTab == Tab.UnlockedFindings);

        // === ЯВНО УСТАНАВЛИВАЕМ ВИДИМОСТЬ ===
        _skillTreeTab.Visible = (_currentTab == Tab.SkillTree);
        _skeletonsTab.Visible = (_currentTab == Tab.UnlockedSkeletons);
        _findingsTab.Visible = (_currentTab == Tab.UnlockedFindings);

        // Обновляем содержимое вкладки при переключении
        if (_currentTab == Tab.SkillTree) _skillTreeTab.Refresh();
        else if (_currentTab == Tab.UnlockedSkeletons) _skeletonsTab.Refresh();
        else if (_currentTab == Tab.UnlockedFindings) _findingsTab.Refresh();

        UpdatePointsDisplay();
    }
    public override void _Process(double delta)
    {
        if (!Visible) return;
        UpdatePointsDisplay();
    }

    private void UpdatePointsDisplay()
    {
        int points = SkillSystem.Instance.GetAvailablePoints();
        _availablePointsLabel.Text = LocalizationManager.TrFormat("ui.progress.available_points", points);

        bool canReset = SkillSystem.Instance.CanReset();
        _resetButton.Disabled = !canReset;
        _resetButton.Modulate = canReset ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
    }

    private void OnResetPressed()
    {
        if (SkillSystem.Instance.ResetSkills())
        {
            GD.Print("[ProgressUI] Навыки сброшены!");
            UpdatePointsDisplay();
            _skillTreeTab.Refresh();
        }
    }

    public void Open()
    {
        Visible = true;
        UpdatePointsDisplay();
        if (_currentTab == Tab.SkillTree) _skillTreeTab.Refresh();
        else if (_currentTab == Tab.UnlockedSkeletons) _skeletonsTab.Refresh();
        else if (_currentTab == Tab.UnlockedFindings) _findingsTab.Refresh();
    }
}