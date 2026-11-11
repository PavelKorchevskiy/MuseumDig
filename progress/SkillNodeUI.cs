using Godot;
using System;

public partial class SkillNodeUI : Control
{
    public event Action<string> OnNodeClicked;

    private TextureRect _iconRect;
    private PanelContainer _panel;
    private StyleBoxFlat _styleBox;
    
    private string _skillId;
    private SkillDefinition _skill;

    // Должно совпадать с CellSize в SkillTreeTab.cs (там же считаются Position
    // узлов) — свой размер узел всё равно переустанавливает в _Ready().
    private const float CellSize = 80f;

    // Тап/свайп: дерево лежит в ScrollContainer, который листается пальцем,
    // поэтому клик по узлу засчитываем только при отпускании и только если
    // палец не уехал дальше порога — иначе любой скролл дерева открывал бы
    // панель навыка под пальцем. Порог как в CameraController.TapThresholdPx.
    private const float TapThresholdPx = 18f;
    private Vector2 _pressPos;
    private bool _dragging;

    // Цвета состояний
    private static readonly Color ColorLocked = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ColorAvailable = new Color(0.6f, 0.6f, 0.6f);
    private static readonly Color ColorMuseum = new Color(0.4f, 0.7f, 1.0f); // Голубой
    private static readonly Color ColorDigging = new Color(0.4f, 1.0f, 0.5f); // Зеленый
    private static readonly Color ColorGold = new Color(1.0f, 0.85f, 0.2f);

    public override void _Ready()
    {
        // === ФИКСИРОВАННЫЙ РАЗМЕР ЯЧЕЙКИ ===
        CustomMinimumSize = new Vector2(CellSize, CellSize);
        Size = new Vector2(CellSize, CellSize);
        ClipContents = true; // Обрезаем всё, что выходит за пределы
        MouseFilter = MouseFilterEnum.Stop;
        GuiInput += OnGuiInput;

        _panel = new PanelContainer();
        // Явно задаём размер панели, а не растягиваем на весь родитель
        _panel.Size = new Vector2(CellSize, CellSize);
        _panel.Position = Vector2.Zero;
        _panel.MouseFilter = MouseFilterEnum.Ignore;

        _styleBox = new StyleBoxFlat();
        _styleBox.CornerRadiusTopLeft = 8;
        _styleBox.CornerRadiusTopRight = 8;
        _styleBox.CornerRadiusBottomLeft = 8;
        _styleBox.CornerRadiusBottomRight = 8;
        _panel.AddThemeStyleboxOverride("panel", _styleBox);

        _iconRect = new TextureRect();
        // Явно задаём размер иконки
        _iconRect.Size = new Vector2(CellSize, CellSize);
        _iconRect.Position = Vector2.Zero;
        _iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _iconRect.MouseFilter = MouseFilterEnum.Ignore; // Иконка не перехватывает клики
        _panel.AddChild(_iconRect);

        AddChild(_panel);
    }

    public void Setup(SkillDefinition skill, int currentLevel, int availablePoints)
    {
        _skillId = skill.Id;
        _skill = skill;

        // Загрузка иконки (если нет, будет заглушка)
        if (ResourceLoader.Exists(skill.IconPath))
        {
            _iconRect.Texture = GD.Load<Texture2D>(skill.IconPath);
        }
        else
        {
            // Временная заглушка, если иконки еще нет
            _iconRect.Texture = GD.Load<Texture2D>("res://icon.svg");
        }

        UpdateVisualState(currentLevel, availablePoints);
    }

    private void UpdateVisualState(int currentLevel, int availablePoints)
    {
        bool isMaxed = currentLevel >= _skill.MaxLevel;
        bool isUnlocked = currentLevel > 0;
        bool canUpgrade = SkillSystem.Instance.CanUpgrade(_skillId);

        Color baseColor = _skill.Branch == SkillBranch.Museum ? ColorMuseum : ColorDigging;

        if (!isUnlocked && !canUpgrade)
        {
            // 1. Недоступна (темная)
            _styleBox.BgColor = ColorLocked;
            _styleBox.BorderWidthLeft = 0;
            _iconRect.Modulate = new Color(0.3f, 0.3f, 0.3f);
        }
        else if (!isUnlocked && canUpgrade)
        {
            // 2. Доступна, но не вложена (серая, светлее)
            _styleBox.BgColor = ColorAvailable;
            _styleBox.BorderWidthLeft = 0;
            _iconRect.Modulate = new Color(0.8f, 0.8f, 0.8f);
        }
        else if (isUnlocked && !isMaxed)
        {
            // 3. Частично выкуплена (цветная)
            _styleBox.BgColor = baseColor;
            _styleBox.BorderWidthLeft = 0;
            _iconRect.Modulate = Colors.White;
        }
        else if (isMaxed)
        {
            // 4. Полностью выкуплена (цветная + золотая рамка)
            _styleBox.BgColor = baseColor;
            _styleBox.BorderWidthLeft = 3;
            _styleBox.BorderWidthRight = 3;
            _styleBox.BorderWidthTop = 3;
            _styleBox.BorderWidthBottom = 3;
            _styleBox.BorderColor = ColorGold;
            _iconRect.Modulate = Colors.White;
        }
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _pressPos = mouseEvent.Position;
                _dragging = false;
            }
            else if (!_dragging && (mouseEvent.Position - _pressPos).Length() <= TapThresholdPx)
            {
                OnNodeClicked?.Invoke(_skillId);
            }
        }
        else if (@event is InputEventMouseMotion motionEvent &&
                 (motionEvent.ButtonMask & MouseButtonMask.Left) != 0)
        {
            if ((motionEvent.Position - _pressPos).Length() > TapThresholdPx)
            {
                _dragging = true;
            }
        }
    }
}