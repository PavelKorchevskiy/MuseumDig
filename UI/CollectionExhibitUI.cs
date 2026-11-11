using Godot;

public partial class CollectionExhibitUI : CanvasLayer
{
    // Верхний край панели (доля высоты экрана). Меньше число = выше панель.
    private const float SheetTop = 0.60f;

    private Label _titleLabel;
    private Button _moveBtn;
    private Button _storeBtn;

    private ConfirmOverlay _storeConfirm;

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

        content.AddChild(MobileUI.CreateLabel(
            LocalizationManager.Tr("ui.exhibit.info"),
            MobileUI.BodyFontSize, MobileUI.MutedColor, center: true));

        // Растягиваем свободное место между текстом и кнопками
        content.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        // Кнопки действий в один ряд
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);
        content.AddChild(actions);

        _moveBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.exhibit.move"), MobileUI.PrimaryColor);
        _moveBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _moveBtn.Pressed += OnMovePressed;
        actions.AddChild(_moveBtn);

        _storeBtn = MobileUI.CreateButton(LocalizationManager.Tr("ui.exhibit.store"), MobileUI.WarningColor);
        _storeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _storeBtn.Pressed += OnStorePressed;
        actions.AddChild(_storeBtn);

        // Красный крестик внизу, как в инвентаре
        content.AddChild(MobileUI.CreateCloseRow(OnClosePressed));

        // Подтверждение (поверх всего, добавляется последним)
        _storeConfirm = new ConfirmOverlay(this,
            LocalizationManager.Tr("ui.exhibit.store"), MobileUI.WarningColor);
    }

    public void Open(Room room, PlacedFurniture placed)
    {
        _currentRoom = room;
        _currentPlacedFurniture = placed;
        _titleLabel.Text = placed.Furniture.DisplayName;
        Visible = true;

        MobileUI.FocusCameraOnFurniture(this, room, placed, SheetTop);
    }

    private void OnMovePressed()
    {
        _currentRoom.RemoveFurniture(_currentPlacedFurniture);

        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();

        OnClosePressed();

        museum?.StartMovingFurniture(_currentRoom.Id, _currentPlacedFurniture);
    }

    private void OnStorePressed()
    {
        _storeConfirm.Show(
            LocalizationManager.Tr("ui.exhibit.store_confirm"),
            ConfirmStore);
    }

    private void ConfirmStore()
    {
        var museum = GetTree().CurrentScene as Museum;
        if (museum != null)
        {
            museum.StoreCollectionExhibit(_currentRoom.Id, _currentPlacedFurniture);
        }
        OnClosePressed();
    }

    private void OnClosePressed()
    {
        Visible = false;
        _storeConfirm.Hide();
        MobileUI.ResetCamera(this);
    }
}
