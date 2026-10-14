using Godot;
using System.Collections.Generic;

public partial class LootSummaryBar : HBoxContainer
{
    private Dictionary<string, Control> _itemCache = new();

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        Alignment = AlignmentMode.Center;
    }

    /// <summary>
    /// Обновляет панель, вычисляя содержимое из состояния плиток.
    /// </summary>
    public void Refresh(DigSite digSite)
    {
        var loot = CollectSessionLoot(digSite);

        // Удаляем старые элементы, которых больше нет
        var toRemove = new List<string>();
        foreach (var key in _itemCache.Keys)
        {
            if (!loot.ContainsKey(key))
            {
                _itemCache[key].QueueFree();
                toRemove.Add(key);
            }
        }
        foreach (var key in toRemove)
        {
            _itemCache.Remove(key);
        }

        // Создаём или обновляем элементы
        foreach (var entry in loot)
        {
            if (!_itemCache.ContainsKey(entry.Key))
            {
                var item = CreateLootItem(entry.Key, entry.Value);
                AddChild(item);
                _itemCache[entry.Key] = item;
            }
            else
            {
                UpdateLootItem(_itemCache[entry.Key], entry.Value);
            }
        }
    }

    /// <summary>
    /// Вычисляет найденные предметы из состояния плиток.
    /// </summary>
    private Dictionary<string, int> CollectSessionLoot(DigSite digSite)
    {
        var result = new Dictionary<string, int>();

        foreach (var child in digSite.GetChildren())
        {
            if (child is Tile tile && tile._loot != null && tile._loot.Collected)
            {
                string id = tile._loot.ResourceId;
                if (!result.ContainsKey(id))
                {
                    result[id] = 0;
                }
                result[id] += tile._loot.Amount;
            }
        }

        return result;
    }

       private Control CreateLootItem(string resourceId, int amount)
    {
        var container = new Control();
        container.CustomMinimumSize = new Vector2(52, 52);
        container.Size = new Vector2(52, 52);
        container.SetMeta("resource_id", resourceId);

        // Фон для выравнивания
        var background = new ColorRect();
        background.Color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        background.Size = new Vector2(48, 48);
        background.Position = new Vector2(2, 2);
        container.AddChild(background);

        // Иконка с фиксированным размером
        var icon = new TextureRect();
        icon.Name = "Icon";
        icon.Size = new Vector2(44, 44);
        icon.Position = new Vector2(4, 4);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        
        string iconPath = GetIconPath(resourceId);
        if (ResourceLoader.Exists(iconPath))
        {
            icon.Texture = GD.Load<Texture2D>(iconPath);
        }
        else
        {
            icon.Texture = GD.Load<Texture2D>("res://icon.svg");
        }
        container.AddChild(icon);

        // Количество в левом нижнем углу
        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.Text = amount.ToString();
        amountLabel.AddThemeFontSizeOverride("font_size", 14);
        amountLabel.AddThemeColorOverride("font_color", Colors.White);
        amountLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
        amountLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        amountLabel.AddThemeConstantOverride("shadow_offset_y", 1);
        amountLabel.Position = new Vector2(4, 34);
        container.AddChild(amountLabel);

        return container;
    }
    private void UpdateLootItem(Control item, int amount)
    {
        var amountLabel = item.GetNodeOrNull<Label>("AmountLabel");
        if (amountLabel != null)
        {
            amountLabel.Text = amount.ToString();
        }
    }

    private string GetIconPath(string resourceId)
    {
        var resource = GameData.GetResource(resourceId);

        if (resource is ConcretionDefinition)
        {
            string shortName = resourceId.Replace("_concretion", "");
            return $"res://assets/museum/items/common/concretion/{shortName}_1.png";
        }

        var collection = GameData.GetCollection(resourceId);
        if (collection != null) return collection.TexturePath;

        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            string partName = fossil.Id.Replace($"{fossil.CollectionId}_", "");
            return $"res://assets/museum/items/{fossil.CollectionId}/{partName}.png";
        }

        if (resource is FossilDefinition standalone && standalone.VariantCount > 1)
        {
            string variantPath = $"res://assets/museum/items/common/{resourceId}/1.png";
            if (ResourceLoader.Exists(variantPath)) return variantPath;
        }

        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath)) return commonPath;

        return "res://icon.svg";
    }
}