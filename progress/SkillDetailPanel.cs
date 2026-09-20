using Godot;
using System;

public partial class SkillDetailPanel : VBoxContainer
{
    public event Action OnSkillUpgraded;

    private TextureRect _iconRect;
    private Label _nameLabel;
    private Label _descLabel;
    private Label _levelLabel;
    private Button _upgradeBtn;

    private string _currentSkillId;

        public override void _Ready()
    {
        CustomMinimumSize = new Vector2(280, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;   // ← Растягиваем по вертикали
        SizeFlagsHorizontal = SizeFlags.Fill;        // ← Заполняем по горизонтали
        AddThemeConstantOverride("separation", 12);

        // Иконка
        _iconRect = new TextureRect();
        _iconRect.CustomMinimumSize = new Vector2(64, 64);
        _iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _iconRect.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_iconRect);

        // Название
        _nameLabel = new Label();
        _nameLabel.AddThemeFontSizeOverride("font_size", 18);
        _nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_nameLabel);

        // Разделитель
        var separator = new HSeparator();
        AddChild(separator);

        // Описание
        _descLabel = new Label();
        _descLabel.AddThemeFontSizeOverride("font_size", 14);
        _descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _descLabel.CustomMinimumSize = new Vector2(250, 60); // ← Минимальная высота для текста
        AddChild(_descLabel);

        // Уровень
        _levelLabel = new Label();
        _levelLabel.AddThemeFontSizeOverride("font_size", 14);
        _levelLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        _levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_levelLabel);

        // Фиксированный отступ вместо Spacer (30 пикселей)
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 30); // ← Фиксированная высота!
        AddChild(spacer);

        // Кнопка улучшения
        _upgradeBtn = new Button();
        _upgradeBtn.Text = LocalizationManager.Tr("ui.skills.upgrade");
        _upgradeBtn.CustomMinimumSize = new Vector2(250, 45); // ← Большая и заметная
        _upgradeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; // ← Центрируем
        _upgradeBtn.Pressed += OnUpgradePressed;
        AddChild(_upgradeBtn);

        Visible = false;
        
        GD.Print($"[SkillDetailPanel] ✅ Готов. Кнопка: '{_upgradeBtn.Text}', размер: {_upgradeBtn.CustomMinimumSize}");
    }

    public void ShowSkill(string skillId)
    {
        _currentSkillId = skillId;
        var skill = SkillData.GetSkill(skillId);
        if (skill == null) return;

        Visible = true;

        if (ResourceLoader.Exists(skill.IconPath))
            _iconRect.Texture = GD.Load<Texture2D>(skill.IconPath);
        else
            _iconRect.Texture = GD.Load<Texture2D>("res://icon.svg");

        _nameLabel.Text = LocalizationManager.Tr(skill.NameKey);
        _descLabel.Text = LocalizationManager.Tr(skill.DescriptionKey);

        UpdateButtonState();
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