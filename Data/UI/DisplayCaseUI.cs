using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DisplayCaseUI : CanvasLayer
{
    private VBoxContainer _mainContainer;
    private Label _titleLabel;
    private VBoxContainer _slotsContainer;
    private VBoxContainer _inventoryListContainer;
    private Button _moveBtn;
    private Button _sellBtn;
    private Button _closeBtn;
    
    // Confirmation Popup
    private ConfirmationDialog _sellConfirmDialog;

    private Room _currentRoom;
    private PlacedFurniture _currentPlacedFurniture;

    public override void _Ready()
    {
        Layer = 90; // Ниже чем основные меню, но выше игры
        Visible = false;

        // === Создаем UI программно ===
        _mainContainer = new VBoxContainer();
        _mainContainer.SetAnchorsPreset(Control.LayoutPreset.RightWide); // Прижимаем вправо
        _mainContainer.OffsetLeft = -300; // Ширина панели 300px
        _mainContainer.OffsetRight = 0;
        _mainContainer.OffsetTop = 50;
        _mainContainer.OffsetBottom = -50;
        _mainContainer.AddThemeConstantOverride("separation", 15);
        
        var bg = new ColorRect { Color = new Color(0.1f, 0.1f, 0.15f, 0.95f) };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _mainContainer.AddChild(bg);

        // Заголовок
        _titleLabel = new Label();
        _titleLabel.AddThemeFontSizeOverride("font_size", 20);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _mainContainer.AddChild(_titleLabel);

        var separator1 = new HSeparator();
        _mainContainer.AddChild(separator1);

        // Секция слотов
        var slotsTitle = new Label { Text = "Содержимое:" };
        _mainContainer.AddChild(slotsTitle);
        
        _slotsContainer = new VBoxContainer();
        _slotsContainer.AddThemeConstantOverride("separation", 5);
        _mainContainer.AddChild(_slotsContainer);

        var separator2 = new HSeparator();
        _mainContainer.AddChild(separator2);

        // Секция инвентаря
        var invTitle = new Label { Text = "Доступные экспонаты:" };
        _mainContainer.AddChild(invTitle);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _inventoryListContainer = new VBoxContainer();
        _inventoryListContainer.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(_inventoryListContainer);
        _mainContainer.AddChild(scroll);

        var separator3 = new HSeparator();
        _mainContainer.AddChild(separator3);

        // Кнопки действий
        var btnContainer = new HBoxContainer();
        btnContainer.AddThemeConstantOverride("separation", 10);

        _moveBtn = new Button { Text = "📦 Переместить", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _moveBtn.Pressed += OnMovePressed;
        btnContainer.AddChild(_moveBtn);

        _sellBtn = new Button { Text = "💰 Продать", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _sellBtn.Pressed += OnSellPressed;
        btnContainer.AddChild(_sellBtn);

        _mainContainer.AddChild(btnContainer);

        _closeBtn = new Button { Text = "Закрыть", CustomMinimumSize = new Vector2(0, 40) };
        _closeBtn.Pressed += OnClosePressed;
        _mainContainer.AddChild(_closeBtn);

        AddChild(_mainContainer);

        // Диалог подтверждения продажи
        _sellConfirmDialog = new ConfirmationDialog();
        _sellConfirmDialog.Title = "Подтверждение продажи";
        _sellConfirmDialog.DialogText = "Вы уверены? Витрина будет продана, а все экспонаты вернутся в инвентарь.";
        _sellConfirmDialog.Confirmed += ConfirmSell;
        AddChild(_sellConfirmDialog);
    }

    public void Open(Room room, PlacedFurniture placed)
    {
        _currentRoom = room;
        _currentPlacedFurniture = placed;

        _titleLabel.Text = placed.Furniture.DisplayName;
        
        RenderSlots();
        RenderInventory();
        
        Visible = true;

        // Зум камеры к витрине
        var museum = GetTree().CurrentScene as Museum;
        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
        if (camera != null)
        {
            int globalX = room.GlobalOffset.X + placed.Position.X;
            int globalY = room.GlobalOffset.Y + placed.Position.Y;
            var isoPos = IsoUtils.GridToIso(globalX, globalY);
            Vector2 worldPos = new Vector2(
                MuseumConstants.GridOffsetX + isoPos.X + IsoUtils.TileWidth / 2f,
                MuseumConstants.GridOffsetY + isoPos.Y + IsoUtils.TileHeight
            );
            camera.ZoomToObject(worldPos, 1.8f);
        }
    }

    private void RenderSlots()
    {
        foreach (var child in _slotsContainer.GetChildren()) child.QueueFree();

        int capacity = _currentPlacedFurniture.GetMaxCapacity();
        for (int i = 0; i < capacity; i++)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);

            var itemLabel = new Label { Text = "Пусто", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            var removeBtn = new Button { Text = "Убрать", Disabled = true };

            if (_currentPlacedFurniture.Items != null && i < _currentPlacedFurniture.Items.Count)
            {
                var item = _currentPlacedFurniture.Items[i];
                var res = GameData.GetResource(item.ResourceId);
                itemLabel.Text = $"{res?.DisplayName}";
                removeBtn.Disabled = false;
                
                string resId = item.ResourceId;
                removeBtn.Pressed += () => RemoveItem(resId);
            }

            row.AddChild(itemLabel);
            row.AddChild(removeBtn);
            _slotsContainer.AddChild(row);
        }
    }

    private void RenderInventory()
    {
        foreach (var child in _inventoryListContainer.GetChildren()) child.QueueFree();

        var allItems = InventorySystem.Instance.GetAllItems();
        bool hasExhibits = false;

        foreach (var invItem in allItems)
        {
            var res = GameData.GetResource(invItem.ResourceId);
            if (res is FossilDefinition fossil && fossil.CanExhibitAlone && !fossil.IsCollection)
            {
                hasExhibits = true;
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 10);

                var infoLabel = new Label { Text = $"{res.DisplayName} (x{invItem.Amount})", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                var addBtn = new Button { Text = "Добавить" };

                // Проверяем, не заполнена ли витрина
                if (_currentPlacedFurniture.Items.Count >= _currentPlacedFurniture.GetMaxCapacity())
                {
                    addBtn.Disabled = true;
                    addBtn.Text = "Нет места";
                }
                else
                {
                    string resId = invItem.ResourceId;
                    addBtn.Pressed += () => AddItem(resId);
                }

                row.AddChild(infoLabel);
                row.AddChild(addBtn);
                _inventoryListContainer.AddChild(row);
            }
        }

        if (!hasExhibits)
        {
            _inventoryListContainer.AddChild(new Label { Text = "Нет подходящих экспонатов", Modulate = new Color(0.7f, 0.7f, 0.7f) });
        }
    }

    private void AddItem(string resourceId)
    {
        var res = GameData.GetResource(resourceId);
        var newItem = new FoundItem(resourceId, 1);

        if (_currentPlacedFurniture.AddItem(newItem))
        {
            InventorySystem.Instance.RemoveItem(resourceId, 1);
            RenderSlots();
            RenderInventory();
            SaveSystem.Instance?.MarkDirty();
            
            // Обновляем вид, чтобы появился спрайт экспоната
            var museum = GetTree().CurrentScene as Museum;
            museum?.RefreshRoomView();
        }
    }

    private void RemoveItem(string resourceId)
    {
        var removed = _currentPlacedFurniture.RemoveItem(resourceId);
        if (removed != null)
        {
            InventorySystem.Instance.AddItem(removed.ResourceId, removed.Amount);
            RenderSlots();
            RenderInventory();
            SaveSystem.Instance?.MarkDirty();
            
            var museum = GetTree().CurrentScene as Museum;
            museum?.RefreshRoomView();
        }
    }

        private void OnMovePressed()
    {
        GD.Print($"[DisplayCaseUI] 📦 Начинаем перемещение витрины с {_currentPlacedFurniture.Items.Count} экспонатами. TypeId: {_currentPlacedFurniture.FurnitureTypeId}");

        // 1. Удаляем витрину с текущего места (но объект в памяти остается живым!)
        _currentRoom.RemoveFurniture(_currentPlacedFurniture);
        
        // 2. Сразу перерисовываем, чтобы старая витрина исчезла
        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
        
        // 3. Закрываем UI и сбрасываем камеру
        OnClosePressed();

        // 4. Передаем ВЕСЬ объект _currentPlacedFurniture для перемещения
        museum?.StartMovingFurniture(_currentRoom.Id, _currentPlacedFurniture);
    }

    private void OnSellPressed()
    {
        _sellConfirmDialog.PopupCentered();
    }

    private void ConfirmSell()
    {
        var museum = GetTree().CurrentScene as Museum;
        if (museum != null)
        {
            museum.SellCurrentDisplayCase(_currentRoom.Id, _currentPlacedFurniture);
        }
        OnClosePressed();
    }

    private void OnClosePressed()
    {
        Visible = false;
        
        // Сбрасываем зум камеры
        var museum = GetTree().CurrentScene as Museum;
        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
        camera?.ResetZoom();
    }
}