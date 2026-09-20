using Godot;
using System.Collections.Generic;

public partial class UnlockedFindingsTab : Control
{
    private ScrollContainer _scroll;
    private GridContainer _grid;

    public override void _Ready()
    {
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 15);
        AddChild(mainVBox);

        var titleLabel = new Label();
        titleLabel.Text = LocalizationManager.Tr("ui.progress.findings_title");
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainVBox.AddChild(titleLabel);

        _scroll = new ScrollContainer();
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.CustomMinimumSize = new Vector2(800, 500); // ← ЯВНЫЙ РАЗМЕР
        mainVBox.AddChild(_scroll);

        _grid = new GridContainer();
        _grid.Columns = 3;
        _grid.AddThemeConstantOverride("h_separation", 30);
        _grid.AddThemeConstantOverride("v_separation", 20);
        _grid.SizeFlagsHorizontal = SizeFlags.ExpandFill; // ← РАСШИРЯЕМ ПО ГОРИЗОНТАЛИ
        _scroll.AddChild(_grid);

        Refresh();
    }

        public void Refresh()
    {
        GD.Print($"[UnlockedFindingsTab] 🔄 Refresh вызван");
        
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        var unlocked = ProgressSystem.Instance.GetUnlockedResourcesList();
        GD.Print($"[UnlockedFindingsTab] 📊 Получено {unlocked.Count} ресурсов из ProgressSystem");

        if (unlocked.Count == 0)
        {
            GD.Print($"[UnlockedFindingsTab] ⚠️ Список пуст, показываем заглушку");
            var emptyLabel = new Label();
            emptyLabel.Text = LocalizationManager.Tr("ui.progress.no_findings");
            emptyLabel.AddThemeFontSizeOverride("font_size", 16);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _grid.AddChild(emptyLabel);
            return;
        }

        foreach (var resource in unlocked)
        {
            GD.Print($"[UnlockedFindingsTab] ✅ Создаём карточку для: {resource.DisplayName}");
            var card = CreateFindingCard(resource);
            _grid.AddChild(card);
        }
    }
    private Control CreateFindingCard(ResourceDefinition resource)
    {
        var card = new VBoxContainer();
        card.CustomMinimumSize = new Vector2(140, 0);
        card.AddThemeConstantOverride("separation", 5);
        card.Alignment = BoxContainer.AlignmentMode.Center;

        // Иконка
        var iconRect = new TextureRect();
        iconRect.CustomMinimumSize = new Vector2(80, 80);
        iconRect.Size = new Vector2(80, 80);
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

        string iconPath = $"res://assets/museum/items/common/{resource.Id}.png";
        if (ResourceLoader.Exists(iconPath))
        {
            iconRect.Texture = GD.Load<Texture2D>(iconPath);
        }
        else
        {
            iconRect.Texture = GD.Load<Texture2D>("res://icon.svg");
        }
        card.AddChild(iconRect);

        // Имя
        var nameLabel = new Label();
        nameLabel.Text = resource.DisplayName;
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        card.AddChild(nameLabel);

        // Редкость
        var rarityLabel = new Label();
        rarityLabel.Text = LocalizationManager.Tr($"rarity.{resource.Rarity.ToString().ToLower()}");
        rarityLabel.AddThemeFontSizeOverride("font_size", 11);
        rarityLabel.HorizontalAlignment = HorizontalAlignment.Center;
        rarityLabel.AddThemeColorOverride("font_color", GetRarityColor(resource.Rarity));
        card.AddChild(rarityLabel);

        return card;
    }

    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.8f, 0.8f, 0.8f),
            Rarity.Uncommon => new Color(0.4f, 1.0f, 0.4f),
            Rarity.Rare => new Color(0.4f, 0.7f, 1.0f),
            Rarity.Epic => new Color(0.8f, 0.4f, 1.0f),
            Rarity.Legendary => new Color(1.0f, 0.85f, 0.2f),
            _ => Colors.White
        };
    }
}