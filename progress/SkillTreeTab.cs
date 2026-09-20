using Godot;
using System.Collections.Generic;

public partial class SkillTreeTab : Control
{
    private ScrollContainer _scroll;
    private Control _canvas;
    private SkillDetailPanel _detailPanel;
    
    private Dictionary<string, SkillNodeUI> _nodes = new();
    private const float CellSize = 64f; // Уменьшили с 80 до 64
    private const float Spacing = 30f;  // Уменьшили с 40 до 30
    private const float BranchOffset = 400f; // Расстояние между ветками

    public override void _Ready()
    {
        var mainHBox = new HBoxContainer();
        mainHBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainHBox.AddThemeConstantOverride("separation", 20);
        AddChild(mainHBox);

        // Левая часть: Дерево
        _scroll = new ScrollContainer();
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.CustomMinimumSize = new Vector2(800, 600);
        mainHBox.AddChild(_scroll);

        _canvas = new Control();
        _canvas.CustomMinimumSize = new Vector2(1200, 800); // Увеличили для двух веток
        _canvas.Draw += OnCanvasDraw;
        _scroll.AddChild(_canvas);

        // Правая часть: Панель деталей
        _detailPanel = new SkillDetailPanel();
        _detailPanel.OnSkillUpgraded += Refresh;
        mainHBox.AddChild(_detailPanel);

        Refresh();
    }

    public void Refresh()
    {
        GD.Print($"[SkillTreeTab] 🔄 Refresh вызван");
        
        foreach (var node in _nodes.Values)
        {
            node.QueueFree();
        }
        _nodes.Clear();

        // Рисуем заголовки веток
        DrawBranchTitle("Ветка Музея", 50f, new Color(0.4f, 0.7f, 1.0f));
        DrawBranchTitle("Ветка Раскопок", BranchOffset + 50f, new Color(0.4f, 1.0f, 0.5f));

        // Создаём узлы для Музея
        var museumSkills = SkillData.GetSkillsByBranch(SkillBranch.Museum);
        CreateNodesForBranch(museumSkills, 0f);

        // Создаём узлы для Раскопок (со смещением вправо)
        var diggingSkills = SkillData.GetSkillsByBranch(SkillBranch.Digging);
        CreateNodesForBranch(diggingSkills, BranchOffset);

        _canvas.QueueRedraw();
        _detailPanel.Refresh();
        
        GD.Print($"[SkillTreeTab] ✅ Создано {_nodes.Count} узлов");
    }

    private void DrawBranchTitle(string text, float xOffset, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", color);
        label.Position = new Vector2(xOffset, 10f);
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
            
            float posX = skill.GridPosition.X * (CellSize + Spacing) + 50f + xOffset;
            float posY = skill.GridPosition.Y * (CellSize + Spacing) + 50f;
            node.Position = new Vector2(posX, posY);
            node.Size = new Vector2(CellSize, CellSize);
            
            node.OnNodeClicked += OnNodeClicked;
            _canvas.AddChild(node);
            _nodes[skill.Id] = node;
        }
    }

    private void OnNodeClicked(string skillId)
    {
        GD.Print($"[SkillTreeTab] 🖱️ Клик на узел: {skillId}");
        _detailPanel.ShowSkill(skillId);
    }

        private void OnCanvasDraw()
    {
        var goldColor = new Color(1.0f, 0.85f, 0.2f);      // Золотой — активная связь
        var grayColor = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Серый — неактивная связь

        // Рисуем вертикальную разделительную линию между ветками
        var separatorColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        _canvas.DrawLine(
            new Vector2(BranchOffset - 20f, 0f), 
            new Vector2(BranchOffset - 20f, _canvas.CustomMinimumSize.Y), 
            separatorColor, 
            2f
        );

        // === РИСУЕМ ВСЕ СВЯЗИ МЕЖДУ УЗЛАМИ ===
        foreach (var skill in SkillData.GetAllSkills())
        {
            if (skill.Prerequisites == null) continue;

            foreach (var prereqId in skill.Prerequisites)
            {
                if (_nodes.TryGetValue(prereqId, out var prereqNode) && 
                    _nodes.TryGetValue(skill.Id, out var targetNode))
                {
                    // Центры узлов
                    Vector2 start = prereqNode.Position + new Vector2(CellSize / 2f, CellSize / 2f);
                    Vector2 end = targetNode.Position + new Vector2(CellSize / 2f, CellSize / 2f);
                    
                    // Выбираем цвет: золотой если пререквизит выкуплен, серый если нет
                    Color lineColor = SkillSystem.Instance.IsSkillUnlocked(prereqId) ? goldColor : grayColor;
                    
                    _canvas.DrawLine(start, end, lineColor, 3f);
                }
            }
        }
    }
}