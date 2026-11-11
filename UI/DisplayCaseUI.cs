using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DisplayCaseUI : CanvasLayer
{
    // Верхний край панели (доля высоты экрана). Меньше число = выше панель.
    // 0.075 даст почти полноэкранную панель, как у инвентаря.
    private const float SheetTop = 0.36f;

    private Label _titleLabel;
    private ScrollContainer _scroll;
    private VBoxContainer _slotsContainer;
    private VBoxContainer _inventoryListContainer;
    private Button _moveBtn;
    private Button _sellBtn;

    private ConfirmOverlay _sellConfirm;

    private Room _currentRoom;
    private PlacedFurniture _currentPlacedFurniture;

    public override void _Ready()
    {
        Layer = 101; // выше нижней панели (100), как у инвентаря
        Visible = false;

        MobileUI.CreateBottomSheet(this, SheetTop, out var content);

        // Заголовок
        _titleLabel = MobileUI.CreateLabel("", MobileUI.TitleFontSize, MobileUI.TitleColor, center: true);
        content.AddChild(_titleLabel);

        // Прокручиваемая часть: содержимое витрины + экспонаты из инвентаря
        _scroll = MobileUI.CreateScroll(out var list);
        content.AddChild(_scroll);

        list.AddChild(MobileUI.CreateLabel(
            LocalizationManager.Tr("ui.case.contents"), MobileUI.BodyFontSize, MobileUI.MutedColor));

        _slotsContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _slotsContainer.AddThemeConstantOverride("separation", 10);
        list.AddChild(_slotsContainer);

        list.AddChild(MobileUI.CreateLabel(
            LocalizationManager.Tr("ui.case.available"), MobileUI.BodyFontSize, MobileUI.MutedColor));

        _inventoryListContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _inventoryListContainer.AddThemeConstantOverride("separation", 10);
        list.AddChild(_inventoryListContainer);

        // Кнопки действий
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);
        content.AddChild(actions);

        _moveBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.case.move"), MobileUI.PrimaryColor);
        _moveBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _moveBtn.Pressed += OnMovePressed;
        actions.AddChild(_moveBtn);

        _sellBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.case.sell"), MobileUI.WarningColor);
        _sellBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _sellBtn.Pressed += OnSellPressed;
        actions.AddChild(_sellBtn);

        // Красный крестик внизу, как в инвентаре
        content.AddChild(MobileUI.CreateCloseRow(OnClosePressed));

        // Подтверждение продажи (поверх всего, добавляется последним)
        _sellConfirm = new ConfirmOverlay(this,
            LocalizationManager.Tr("ui.case.sell"), MobileUI.DangerColor);
    }

    public void Open(Room room, PlacedFurniture placed)
    {
        _currentRoom = room;
        _currentPlacedFurniture = placed;

        _titleLabel.Text = placed.Furniture.DisplayName;

        RenderSlots();
        RenderInventory();
        _scroll.ScrollVertical = 0;

        Visible = true;

        MobileUI.FocusCameraOnFurniture(this, room, placed, SheetTop);
    }

    private void RenderSlots()
    {
        MobileUI.ClearChildren(_slotsContainer);

        int capacity = _currentPlacedFurniture.GetMaxCapacity();
        for (int i = 0; i < capacity; i++)
        {
            var row = MobileUI.CreateRow(out var hbox);

            bool filled = _currentPlacedFurniture.Items != null && i < _currentPlacedFurniture.Items.Count;
            if (filled)
            {
                var item = _currentPlacedFurniture.Items[i];
                var res = GameData.GetResource(item.ResourceId);

                var nameLabel = MobileUI.CreateLabel(res?.DisplayName ?? item.ResourceId,
                    MobileUI.BodyFontSize, Colors.White, wrap: true);
                nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                nameLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                hbox.AddChild(nameLabel);

                var removeBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.case.remove"),
                    MobileUI.DangerColor, MobileUI.RowButtonHeight, 130f);
                removeBtn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                string resId = item.ResourceId;
                removeBtn.Pressed += () => RemoveItem(resId);
                hbox.AddChild(removeBtn);
            }
            else
            {
                var emptyLabel = MobileUI.CreateLabel(LocalizationManager.Tr("ui.case.empty"),
                    MobileUI.BodyFontSize, MobileUI.MutedColor);
                emptyLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                emptyLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                hbox.AddChild(emptyLabel);
            }

            _slotsContainer.AddChild(row);
        }
    }

    private void RenderInventory()
    {
        MobileUI.ClearChildren(_inventoryListContainer);

        var allItems = InventorySystem.Instance.GetAllItems();
        bool hasExhibits = false;
        bool isFull = _currentPlacedFurniture.Items.Count >= _currentPlacedFurniture.GetMaxCapacity();

        foreach (var invItem in allItems)
        {
            var res = GameData.GetResource(invItem.ResourceId);
            if (res is FossilDefinition fossil && fossil.CanExhibitAlone && !fossil.IsCollection)
            {
                hasExhibits = true;

                var row = MobileUI.CreateRow(out var hbox);

                var infoLabel = MobileUI.CreateLabel($"{res.DisplayName} (x{invItem.Amount})",
                    MobileUI.BodyFontSize, Colors.White, wrap: true);
                infoLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                infoLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                hbox.AddChild(infoLabel);

                Button addBtn;
                if (isFull)
                {
                    addBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.case.no_space"),
                        MobileUI.NeutralColor, MobileUI.RowButtonHeight, 150f);
                    addBtn.Disabled = true;
                }
                else
                {
                    addBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.case.add"),
                        MobileUI.PrimaryColor, MobileUI.RowButtonHeight, 150f);
                    string resId = invItem.ResourceId;
                    addBtn.Pressed += () => AddItem(resId);
                }
                addBtn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                hbox.AddChild(addBtn);

                _inventoryListContainer.AddChild(row);
            }
        }

        if (!hasExhibits)
        {
            _inventoryListContainer.AddChild(MobileUI.CreateLabel(
                LocalizationManager.Tr("ui.case.no_items"),
                MobileUI.BodyFontSize, MobileUI.MutedColor, center: true, wrap: true));
        }
    }

    private void AddItem(string resourceId)
    {
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
        _sellConfirm.Show(
            LocalizationManager.Tr("ui.case.sell_confirm"),
            ConfirmSell);
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
        _sellConfirm.Hide();
        MobileUI.ResetCamera(this);
    }
}
