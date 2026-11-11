using Godot;
using System.Collections.Generic;

public partial class SkillTreeTab : Control
{
    private ScrollContainer _scroll;
    private Control _canvas;
    private SkillDetailPanel _detailPanel;

    private Dictionary<string, SkillNodeUI> _nodes = new();

    // === Раскладка под экран 540px (project.godot: viewport 540x960) ===
    // 2 колонки на ветку, 4 колонки всего — обе ветки видно целиком по
    // ширине, скроллить нужно только вертикально (см. SkillData.cs —
    // позиции навыков пересчитаны под 2 колонки по глубине зависимостей).
    // Подобрано под доступную ширину вкладки (экран 540px минус поля ProgressUI
    // 16+16 = ~508px): раньше дерево занимало ~376px и справа оставалось пустое
    // место — теперь раскладка использует почти всю ширину.
    private const float CellSize = 80f;
    private const float ColumnSpacing = 26f;   // между колонками внутри ветки
    private const float BranchGap = 60f;       // между ветками
    private const float RowSpacing = 44f;
    private const float TopOffset = 48f;       // место под заголовок ветки
    private const float LeftMargin = 22f;
    private const float BranchWidth = CellSize * 2f + ColumnSpacing; // ширина ветки (2 колонки)

    private const float MuseumX0 = LeftMargin;
    private const float DiggingX0 = MuseumX0 + BranchWidth + BranchGap;
    private const float CanvasWidth = DiggingX0 + BranchWidth + LeftMargin;
    private const float CanvasHeight = TopOffset + CellSize * 4f + RowSpacing * 3f + 40f; // 4 строки (0..3)

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        _scroll = new ScrollContainer();
        _scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        // Ветки целиком вмещаются по ширине — горизонтальный скролл не нужен
        // и только мешал бы вертикальному свайпу пальцем.
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(_scroll);

        _canvas = new Control();
        _canvas.CustomMinimumSize = new Vector2(CanvasWidth, CanvasHeight);
        _canvas.Draw += OnCanvasDraw;
        _scroll.AddChild(_canvas);

        // Детали навыка — нижний лист поверх всего экрана прогресса (как
        // DisplayCaseUI/CollectionExhibitUI), а не постоянная боковая
        // колонка: на экране 540px ей просто негде стоять рядом с деревом.
        _detailPanel = new SkillDetailPanel();
        _detailPanel.OnSkillUpgraded += Refresh;
        AddChild(_detailPanel);

        Refresh();
    }

    public void Refresh()
    {
        foreach (var node in _nodes.Values)
        {
            node.QueueFree();
        }
        _nodes.Clear();

        DrawBranchTitle(LocalizationManager.Tr("ui.skills.branch_museum"), MuseumX0, new Color(0.4f, 0.7f, 1.0f));
        DrawBranchTitle(LocalizationManager.Tr("ui.skills.branch_digging"), DiggingX0, new Color(0.4f, 1.0f, 0.5f));

        var museumSkills = SkillData.GetSkillsByBranch(SkillBranch.Museum);
        CreateNodesForBranch(museumSkills, MuseumX0);

        var diggingSkills = SkillData.GetSkillsByBranch(SkillBranch.Digging);
        CreateNodesForBranch(diggingSkills, DiggingX0);

        _canvas.QueueRedraw();
        _detailPanel.Refresh();
    }

    private void DrawBranchTitle(string text, float xOffset, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", color);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(BranchWidth, 0);
        label.Position = new Vector2(xOffset, 4f);
        _canvas.AddChild(label);
    }

    private void CreateNodesForBranch(List<SkillDefinition> skills, float xOffset)
    {
        foreach (var skill in skills)
        {
            var node = new SkillNodeUI();
            int level = SkillSystem.Instance.GetSkillLevel(skill.Id);
            int points = SkillSystem.Instance.GetAvailablePoints();

            node.Ready += () => node.Setup(skill, level, points);

            float posX = skill.GridPosition.X * (CellSize + ColumnSpacing) + xOffset;
            float posY = skill.GridPosition.Y * (CellSize + RowSpacing) + TopOffset;
            node.Position = new Vector2(posX, posY);
            node.Size = new Vector2(CellSize, CellSize);

            node.OnNodeClicked += OnNodeClicked;
            _canvas.AddChild(node);
            _nodes[skill.Id] = node;
        }
    }

    private void OnNodeClicked(string skillId)
    {
        _detailPanel.ShowSkill(skillId);
    }

    private void OnCanvasDraw()
    {
        var goldColor = new Color(1.0f, 0.85f, 0.2f);      // Золотой — активная связь
        var grayColor = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Серый — неактивная связь

        // Разделительная линия между ветками — посередине промежутка между ними
        var separatorColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        float separatorX = MuseumX0 + BranchWidth + BranchGap / 2f;
        _canvas.DrawLine(
            new Vector2(separatorX, 0f),
            new Vector2(separatorX, CanvasHeight),
            separatorColor,
            2f
        );

        // === Рисуем все связи между узлами ===
        foreach (var skill in SkillData.GetAllSkills())
        {
            if (skill.Prerequisites == null) continue;

            foreach (var prereqId in skill.Prerequisites)
            {
                if (_nodes.TryGetValue(prereqId, out var prereqNode) &&
                    _nodes.TryGetValue(skill.Id, out var targetNode))
                {
                    Vector2 start = prereqNode.Position + new Vector2(CellSize / 2f, CellSize / 2f);
                    Vector2 end = targetNode.Position + new Vector2(CellSize / 2f, CellSize / 2f);

                    Color lineColor = SkillSystem.Instance.IsSkillUnlocked(prereqId) ? goldColor : grayColor;

                    _canvas.DrawLine(start, end, lineColor, 3f);
                }
            }
        }
    }
}