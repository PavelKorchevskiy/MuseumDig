using Godot;
using System;
using System.Text;

/// <summary>
/// Экран заданий: вкладки "Ежедневные" и "Достижения".
/// Открывается кнопкой заданий в MuseumBottomBar. Оформление — как у ProgressUI
/// (полноэкранная панель MobileUI, вкладки, крупный крестик снизу).
/// </summary>
public partial class QuestsUI : CanvasLayer
{
    public static QuestsUI Instance { get; private set; }

    private enum Tab { Daily, Achievements }
    private Tab _currentTab = Tab.Daily;

    private Button _tabDailyButton;
    private Button _tabAchievementsButton;
    private Label _resetTimerLabel;
    private VBoxContainer _list;
    private Panel _dailyBadge;
    private Panel _achievementsBadge;

    private const float HeaderHeight = 200f;

    // Список перестраиваем только когда реально изменилось состояние
    // (прогресс/награды), а не каждый кадр — иначе рвались бы нажатия и скролл.
    private string _lastSignature = "";
    private float _refreshTimer = 0f;
    private const float RefreshInterval = 0.5f;

    private static readonly Color DoneColor = new Color(0.25f, 0.6f, 0.3f, 1f);
    private static readonly Color RewardColor = new Color(1f, 0.85f, 0.4f);

    public override void _Ready()
    {
        Instance = this;
        Layer = 102;
        Visible = false;

        CreateMainPanel();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
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

        // Высокая шапка: опускает список, чтобы до кнопки "Забрать" у верхнего
        // задания было легко дотянуться большим пальцем.
        var header = new VBoxContainer();
        header.CustomMinimumSize = new Vector2(0, HeaderHeight);
        header.Alignment = BoxContainer.AlignmentMode.Center;
        header.AddThemeConstantOverride("separation", 10);
        mainVBox.AddChild(header);

        header.AddChild(MobileUI.CreateLabel(
            LocalizationManager.Tr("ui.quests.title"), MobileUI.TitleFontSize + 8, MobileUI.TitleColor, center: true));

        // Таймер до обновления ежедневных заданий
        _resetTimerLabel = MobileUI.CreateLabel("", MobileUI.SmallFontSize, MobileUI.MutedColor, center: true);
        header.AddChild(_resetTimerLabel);

        // Страница со списком + вкладки под ней (над крестиком)
        mainVBox.AddChild(MobileUI.CreateTabbedPage(out var page, out var tabs));
        page.AddChild(MobileUI.CreateScroll(out _list));

        _tabDailyButton = CreateTabButton(LocalizationManager.Tr("ui.quests.tab_daily"), Tab.Daily);
        _tabAchievementsButton = CreateTabButton(LocalizationManager.Tr("ui.quests.tab_achievements"), Tab.Achievements);
        tabs.AddChild(_tabDailyButton);
        tabs.AddChild(_tabAchievementsButton);

        // Красные кружки на вкладках: там есть неполученная награда
        _dailyBadge = MobileUI.CreateBadge(_tabDailyButton);
        _achievementsBadge = MobileUI.CreateBadge(_tabAchievementsButton);

        mainVBox.AddChild(MobileUI.CreateCloseRow(() => Visible = false));

        AddChild(panel);
    }

    private Button CreateTabButton(string text, Tab tab)
    {
        var button = MobileUI.CreateTab(text);
        button.Pressed += () => SwitchTab(tab);
        return button;
    }

    public void Open()
    {
        Visible = true;
        SwitchTab(_currentTab);
    }

    private void SwitchTab(Tab tab)
    {
        _currentTab = tab;
        MobileUI.SetTabActive(_tabDailyButton, tab == Tab.Daily);
        MobileUI.SetTabActive(_tabAchievementsButton, tab == Tab.Achievements);
        // Таймер прячем прозрачностью, а не Visible, чтобы шапка не меняла раскладку
        _resetTimerLabel.Modulate = (tab == Tab.Daily) ? Colors.White : Colors.Transparent;

        Rebuild();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        if (_currentTab == Tab.Daily)
        {
            var left = QuestSystem.Instance.GetTimeUntilReset();
            _resetTimerLabel.Text = string.Format(
                LocalizationManager.Tr("ui.quests.reset_in"),
                $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}");
        }

        _refreshTimer += (float)delta;
        if (_refreshTimer < RefreshInterval) return;
        _refreshTimer = 0f;

        if (BuildSignature() != _lastSignature) Rebuild();
        UpdateBadges();
    }

    private void UpdateBadges()
    {
        _dailyBadge.Visible = QuestSystem.Instance.HasClaimableDaily();
        _achievementsBadge.Visible = QuestSystem.Instance.HasClaimableAchievements();
    }

    // ===== ПОСТРОЕНИЕ СПИСКА =====

    private string BuildSignature()
    {
        var qs = QuestSystem.Instance;
        var sb = new StringBuilder();
        sb.Append(_currentTab).Append('|');

        if (_currentTab == Tab.Daily)
        {
            sb.Append(qs.GetDailyReward()).Append('|');
            foreach (var quest in qs.GetDailyQuests())
            {
                sb.Append(qs.GetDailyProgress(quest.Id)).Append('/')
                  .Append(qs.GetDailyTarget(quest.Id))
                  .Append(qs.IsDailyClaimed(quest.Id) ? 'c' : '-').Append(';');
            }
            sb.Append(qs.IsDailyBonusClaimed() ? 'c' : '-');
        }
        else
        {
            foreach (var achievement in qs.GetAchievements())
            {
                sb.Append(qs.GetAchievementProgress(achievement))
                  .Append(qs.IsAchievementClaimed(achievement) ? 'c' : '-').Append(';');
            }
        }
        return sb.ToString();
    }

    private void Rebuild()
    {
        MobileUI.ClearChildren(_list);

        if (_currentTab == Tab.Daily) BuildDailyList();
        else BuildAchievementsList();

        _lastSignature = BuildSignature();
        UpdateBadges();
    }

    private void BuildDailyList()
    {
        var qs = QuestSystem.Instance;
        string rewardText = string.Format(LocalizationManager.Tr("ui.quests.reward_gold"), qs.GetDailyReward());

        int completed = 0;
        foreach (var quest in qs.GetDailyQuests())
        {
            string questId = quest.Id;
            bool isCompleted = qs.IsDailyCompleted(questId);
            if (isCompleted) completed++;

            _list.AddChild(CreateEntry(
                qs.GetDailyTitle(quest),
                null,
                rewardText,
                qs.GetDailyProgress(questId),
                qs.GetDailyTarget(questId),
                isCompleted,
                qs.IsDailyClaimed(questId),
                () => { if (qs.ClaimDaily(questId)) Rebuild(); }));
        }

        // Бонус за все задания дня
        int total = qs.GetDailyQuests().Count;
        _list.AddChild(CreateEntry(
            LocalizationManager.Tr("ui.quests.daily.bonus"),
            null,
            QuestSystem.GetItemName(QuestSystem.DailyBonusItemId),
            completed,
            total,
            completed >= total,
            qs.IsDailyBonusClaimed(),
            () => { if (qs.ClaimDailyBonus()) Rebuild(); }));
    }

    private void BuildAchievementsList()
    {
        var qs = QuestSystem.Instance;

        // Сначала те, где можно забрать награду, затем в процессе, в конце — полученные
        AddAchievements(qs, a => qs.IsAchievementCompleted(a) && !qs.IsAchievementClaimed(a));
        AddAchievements(qs, a => !qs.IsAchievementCompleted(a) && !qs.IsAchievementClaimed(a));
        AddAchievements(qs, a => qs.IsAchievementClaimed(a));
    }

    private void AddAchievements(QuestSystem qs, Func<AchievementDefinition, bool> filter)
    {
        foreach (var achievement in qs.GetAchievements())
        {
            if (!filter(achievement)) continue;

            string achievementId = achievement.Id;
            _list.AddChild(CreateEntry(
                LocalizationManager.Tr(achievement.TitleKey),
                LocalizationManager.TrFormat(achievement.DescriptionKey, achievement.Target),
                QuestSystem.FormatRewards(achievement.Rewards),
                qs.GetAchievementProgress(achievement),
                achievement.Target,
                qs.IsAchievementCompleted(achievement),
                qs.IsAchievementClaimed(achievement),
                () => { if (qs.ClaimAchievement(achievementId)) Rebuild(); }));
        }
    }

    /// <summary>
    /// Одна карточка: слева название/описание/награда/прогресс, справа кнопка "Забрать".
    /// </summary>
    private Control CreateEntry(string title, string description, string rewardText,
        int progress, int target, bool completed, bool claimed, Action onClaim)
    {
        var row = MobileUI.CreateRow(out var hbox);
        if (claimed) row.Modulate = new Color(1f, 1f, 1f, 0.55f);

        var info = new VBoxContainer();
        info.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        info.AddThemeConstantOverride("separation", 4);
        info.MouseFilter = Control.MouseFilterEnum.Ignore;
        hbox.AddChild(info);

        var titleLabel = MobileUI.CreateLabel(title, MobileUI.BodyFontSize, Colors.White, wrap: true);
        titleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        info.AddChild(titleLabel);

        if (!string.IsNullOrEmpty(description))
        {
            var descLabel = MobileUI.CreateLabel(description, MobileUI.SmallFontSize, MobileUI.MutedColor, wrap: true);
            descLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            info.AddChild(descLabel);
        }

        var rewardLabel = MobileUI.CreateLabel(
            string.Format(LocalizationManager.Tr("ui.quests.reward"), rewardText),
            MobileUI.SmallFontSize, RewardColor, wrap: true);
        rewardLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        info.AddChild(rewardLabel);

        // Полоса прогресса с числами поверх
        var bar = new ProgressBar();
        bar.MinValue = 0;
        bar.MaxValue = Math.Max(1, target);
        bar.Value = progress;
        bar.ShowPercentage = false;
        bar.CustomMinimumSize = new Vector2(0, 22);
        bar.MouseFilter = Control.MouseFilterEnum.Ignore;
        bar.AddThemeStyleboxOverride("background", MobileUI.MakeStyle(new Color(0.08f, 0.08f, 0.12f, 1f), 8));
        bar.AddThemeStyleboxOverride("fill", MobileUI.MakeStyle(completed ? DoneColor : MobileUI.PrimaryColor, 8));

        var barLabel = MobileUI.CreateLabel($"{progress} / {target}", 15, Colors.White, center: true);
        barLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        barLabel.VerticalAlignment = VerticalAlignment.Center;
        barLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        bar.AddChild(barLabel);
        info.AddChild(bar);

        // Кнопка
        string buttonText = claimed
            ? LocalizationManager.Tr("ui.quests.claimed")
            : LocalizationManager.Tr("ui.quests.claim");

        var button = MobileUI.CreateButton(buttonText, DoneColor, MobileUI.RowButtonHeight, 124f, MobileUI.SmallFontSize);
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.Disabled = claimed || !completed;
        button.Pressed += () => onClaim?.Invoke();
        hbox.AddChild(button);

        return row;
    }
}
