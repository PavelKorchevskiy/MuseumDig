using Godot;
using System;

/// <summary>
/// Детали выбранного навыка — нижний лист поверх дерева навыков, того же
/// вида, что DisplayCaseUI/CollectionExhibitUI (MobileUI.CreateBottomSheet),
/// а не постоянная боковая колонка: на экране 540px (project.godot) боковой
/// панели рядом с деревом просто негде стоять. Открывается тапом по узлу,
/// закрывается крестиком снизу.
/// </summary>
public partial class SkillDetailPanel : CanvasLayer
{
    public event Action OnSkillUpgraded;

    // Контент короткий (иконка, название, описание, кнопка) — нижней
    // половины экрана достаточно, верх дерева остаётся виден под листом.
    private const float SheetTop = 0.45f;

    private TextureRect _iconRect;
    private Label _nameLabel;
    private Label _descLabel;
    private Label _levelLabel;
    private Button _upgradeBtn;

    private string _currentSkillId;

    public override void _Ready()
    {
        Layer = 103; // выше панели прогресса (102), чтобы лист рисовался над деревом
        Visible = false;

        MobileUI.CreateBottomSheet(this, SheetTop, out var content);

        _iconRect = new TextureRect
        {
            CustomMinimumSize = new Vector2(72, 72),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        content.AddChild(_iconRect);

        _nameLabel = MobileUI.CreateLabel("", MobileUI.TitleFontSize, MobileUI.TitleColor, center: true);
        content.AddChild(_nameLabel);

        content.AddChild(new HSeparator());

        _descLabel = MobileUI.CreateLabel("", MobileUI.BodyFontSize, Colors.White, center: true, wrap: true);
        content.AddChild(_descLabel);

        _levelLabel = MobileUI.CreateLabel("", MobileUI.BodyFontSize, MobileUI.MutedColor, center: true);
        content.AddChild(_levelLabel);

        _upgradeBtn = MobileUI.CreateButton("", MobileUI.PrimaryColor);
        _upgradeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _upgradeBtn.Pressed += OnUpgradePressed;
        content.AddChild(_upgradeBtn);

        content.AddChild(MobileUI.CreateCloseRow(Hide));
    }

    public void ShowSkill(string skillId)
    {
        _currentSkillId = skillId;
        var skill = SkillData.GetSkill(skillId);
        if (skill == null) return;

        Visible = true;

        _iconRect.Texture = ResourceLoader.Exists(skill.IconPath)
            ? GD.Load<Texture2D>(skill.IconPath)
            : GD.Load<Texture2D>("res://icon.svg");

        _nameLabel.Text = LocalizationManager.Tr(skill.NameKey);
        _descLabel.Text = LocalizationManager.Tr(skill.DescriptionKey);

        UpdateButtonState();
    }

    public void Hide()
    {
        Visible = false;
    }

    public void Refresh()
    {
        if (Visible && _currentSkillId != null)
        {
            UpdateButtonState();
        }
    }

    private void UpdateButtonState()
    {
        var skill = SkillData.GetSkill(_currentSkillId);
        int currentLevel = SkillSystem.Instance.GetSkillLevel(_currentSkillId);
        bool canUpgrade = SkillSystem.Instance.CanUpgrade(_currentSkillId);

        if (currentLevel >= skill.MaxLevel)
        {
            _levelLabel.Text = LocalizationManager.TrFormat("ui.skills.max_level", currentLevel);
            _upgradeBtn.Text = LocalizationManager.Tr("ui.skills.maxed");
            _upgradeBtn.Disabled = true;
            _upgradeBtn.Modulate = new Color(0.5f, 0.5f, 0.5f);
        }
        else
        {
            int cost = skill.CostPerLevel[currentLevel];
            _levelLabel.Text = LocalizationManager.TrFormat("ui.skills.current_level", currentLevel, skill.MaxLevel);
            _upgradeBtn.Text = LocalizationManager.TrFormat("ui.skills.upgrade_cost", cost);
            _upgradeBtn.Disabled = !canUpgrade;
            _upgradeBtn.Modulate = canUpgrade ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private void OnUpgradePressed()
    {
        if (SkillSystem.Instance.UpgradeSkill(_currentSkillId))
        {
            OnSkillUpgraded?.Invoke();
            Refresh();
        }
    }
}