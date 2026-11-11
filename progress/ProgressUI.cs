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
        panel.AddThemeStyleboxOverride("panel", MobileUI.MakeStyle(MobileUI.PanelColor, 20));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        panel.AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(mainVBox);

        // Заголовок + очки
        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 10);

        var titleLabel = MobileUI.CreateLabel(
            LocalizationManager.Tr("ui.progress.title"), MobileUI.TitleFontSize, MobileUI.TitleColor);
        titleRow.AddChild(titleLabel);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleRow.AddChild(spacer);

        _availablePointsLabel = MobileUI.CreateLabel("", MobileUI.BodyFontSize, new Color(0.4f, 1f, 0.4f));
        titleRow.AddChild(_availablePointsLabel);

        mainVBox.AddChild(titleRow);

        // Кнопка сброса навыков — отдельной строкой, чтобы не тесниться под палец
        _resetButton = MobileUI.CreateButton(
            LocalizationManager.Tr("ui.progress.reset_skills"), MobileUI.WarningColor, MobileUI.RowButtonHeight);
        _resetButton.Pressed += OnResetPressed;
        mainVBox.AddChild(_resetButton);

        // Страница с содержимым + вкладки под ней (над крестиком)
        mainVBox.AddChild(MobileUI.CreateTabbedPage(out var page, out var tabsContainer));

        _tabSkillTreeButton = CreateTabButton("ui.progress.tab_skills", Tab.SkillTree);
        _tabSkeletonsButton = CreateTabButton("ui.progress.tab_skeletons", Tab.UnlockedSkeletons);
        _tabFindingsButton = CreateTabButton("ui.progress.tab_findings", Tab.UnlockedFindings);

        tabsContainer.AddChild(_tabSkillTreeButton);
        tabsContainer.AddChild(_tabSkeletonsButton);
        tabsContainer.AddChild(_tabFindingsButton);

        // Контент вкладок
        _tabContent = new Control();
        _tabContent.Name = "TabContent";
        _tabContent.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _tabContent.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        page.AddChild(_tabContent);

        // Создаём вкладки
        _skillTreeTab = new SkillTreeTab();
        _skeletonsTab = new UnlockedSkeletonsTab();
        _findingsTab = new UnlockedFindingsTab();

        _tabContent.AddChild(_skillTreeTab);
        _tabContent.AddChild(_skeletonsTab);
        _tabContent.AddChild(_findingsTab);

        // Закрытие — крупный крестик снизу, как в остальных мобильных экранах
        mainVBox.AddChild(MobileUI.CreateCloseRow(() => Visible = false));

        AddChild(panel);
    }

    private Button CreateTabButton(string localizationKey, Tab tab)
    {
        var button = MobileUI.CreateTab(LocalizationManager.Tr(localizationKey));
        button.Pressed += () => SwitchTab(tab);
        return button;
    }

     private void SwitchTab(Tab newTab)
    {
        _currentTab = newTab;

        MobileUI.SetTabActive(_tabSkillTreeButton, _currentTab == Tab.SkillTree);
        MobileUI.SetTabActive(_tabSkeletonsButton, _currentTab == Tab.UnlockedSkeletons);
        MobileUI.SetTabActive(_tabFindingsButton, _currentTab == Tab.UnlockedFindings);

        // Сброс навыков относится только к дереву навыков
        _resetButton.Visible = (_currentTab == Tab.SkillTree);

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