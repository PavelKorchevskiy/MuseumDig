using Godot;
using System.Collections.Generic;

public partial class UnlockedSkeletonsTab : Control
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
        titleLabel.Text = LocalizationManager.Tr("ui.progress.skeletons_title");
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
        _grid.Columns = 5;
        _grid.AddThemeConstantOverride("h_separation", 20);
        _grid.AddThemeConstantOverride("v_separation", 20);
        _grid.SizeFlagsHorizontal = SizeFlags.ExpandFill; // ← РАСШИРЯЕМ ПО ГОРИЗОНТАЛИ
        _scroll.AddChild(_grid);

        Refresh();
    }

        public void Refresh()
    {
        GD.Print($"[UnlockedSkeletonsTab] 🔄 Refresh вызван");
        
        // Очистка старых элементов
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        var unlocked = ProgressSystem.Instance.GetUnlockedCollectionsList();
        GD.Print($"[UnlockedSkeletonsTab] 📊 Получено {unlocked.Count} коллекций из ProgressSystem");

        if (unlocked.Count == 0)
        {
            GD.Print($"[UnlockedSkeletonsTab] ⚠️ Список пуст, показываем заглушку");
            var emptyLabel = new Label();
            emptyLabel.Text = LocalizationManager.Tr("ui.progress.no_skeletons");
            emptyLabel.AddThemeFontSizeOverride("font_size", 16);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _grid.AddChild(emptyLabel);
            return;
        }

                foreach (var collection in unlocked)
        {
            GD.Print($"[UnlockedSkeletonsTab] ✅ Создаём карточку для: {collection.DisplayName}");
            var card = CreateSkeletonCard(collection);
            _grid.AddChild(card);
        }
        
        GD.Print($"[UnlockedSkeletonsTab] 📏 _grid размер: {_grid.Size}, _scroll размер: {_scroll.Size}");
        GD.Print($"[UnlockedSkeletonsTab] 👁️ _skeletonsTab видима: {this.Visible}");
    }

    private Control CreateSkeletonCard(CollectionDefinition collection)
    {
        var card = new VBoxContainer();
        card.CustomMinimumSize = new Vector2(120, 0);
        card.AddThemeConstantOverride("separation", 5);

        // Иконка
        var iconRect = new TextureRect();
        iconRect.CustomMinimumSize = new Vector2(96, 96);
        iconRect.Size = new Vector2(96, 96);
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

        if (ResourceLoader.Exists(collection.TexturePath))
        {
            iconRect.Texture = GD.Load<Texture2D>(collection.TexturePath);
        }
        else
        {
            iconRect.Texture = GD.Load<Texture2D>("res://icon.svg");
        }
        card.AddChild(iconRect);

        // Имя
        var nameLabel = new Label();
        nameLabel.Text = collection.DisplayName;
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        card.AddChild(nameLabel);

        // Редкость (цветная метка)
        var rarityLabel = new Label();
        rarityLabel.Text = LocalizationManager.Tr($"rarity.{collection.Rarity.ToString().ToLower()}");
        rarityLabel.AddThemeFontSizeOverride("font_size", 12);
        rarityLabel.HorizontalAlignment = HorizontalAlignment.Center;
        rarityLabel.AddThemeColorOverride("font_color", GetRarityColor(collection.Rarity));
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