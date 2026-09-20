using Godot;

public partial class CollectionExhibitUI : CanvasLayer
{
    private VBoxContainer _mainContainer;
    private Label _titleLabel;
    private Button _moveBtn;
    private Button _storeBtn;
    private Button _closeBtn;
    private ConfirmationDialog _storeConfirmDialog;

    private Room _currentRoom;
    private PlacedFurniture _currentPlacedFurniture;

    public override void _Ready()
    {
        Layer = 90;
        Visible = false;

        _mainContainer = new VBoxContainer();
        _mainContainer.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _mainContainer.OffsetLeft = -300;
        _mainContainer.OffsetRight = 0;
        _mainContainer.OffsetTop = 50;
        _mainContainer.OffsetBottom = -50;
        _mainContainer.AddThemeConstantOverride("separation", 15);
        
        var bg = new ColorRect { Color = new Color(0.1f, 0.1f, 0.15f, 0.95f) };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _mainContainer.AddChild(bg);

        _titleLabel = new Label();
        _titleLabel.AddThemeFontSizeOverride("font_size", 20);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _mainContainer.AddChild(_titleLabel);

        var separator1 = new HSeparator();
        _mainContainer.AddChild(separator1);

        var infoLabel = new Label { Text = "Собранный экспонат", HorizontalAlignment = HorizontalAlignment.Center };
        infoLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        _mainContainer.AddChild(infoLabel);

        var separator2 = new HSeparator();
        _mainContainer.AddChild(separator2);

        var btnContainer = new VBoxContainer();
        btnContainer.AddThemeConstantOverride("separation", 10);
        btnContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        btnContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        _moveBtn = new Button { Text = "📦 Переместить", CustomMinimumSize = new Vector2(0, 40) };
        _moveBtn.Pressed += OnMovePressed;
        btnContainer.AddChild(_moveBtn);

        _storeBtn = new Button { Text = "📥 Убрать на склад", CustomMinimumSize = new Vector2(0, 40) };
        _storeBtn.Pressed += OnStorePressed;
        btnContainer.AddChild(_storeBtn);

        _mainContainer.AddChild(btnContainer);

        var separator3 = new HSeparator();
        _mainContainer.AddChild(separator3);

        _closeBtn = new Button { Text = "Закрыть", CustomMinimumSize = new Vector2(0, 40) };
        _closeBtn.Pressed += OnClosePressed;
        _mainContainer.AddChild(_closeBtn);

        AddChild(_mainContainer);

        // Диалог подтверждения
        _storeConfirmDialog = new ConfirmationDialog();
        _storeConfirmDialog.Title = "Подтверждение";
        _storeConfirmDialog.DialogText = "Вы уверены? Экспонат будет убран со стены и вернется в ваш инвентарь.";
        _storeConfirmDialog.Confirmed += ConfirmStore;
        AddChild(_storeConfirmDialog);
    }

    public void Open(Room room, PlacedFurniture placed)
    {
        _currentRoom = room;
        _currentPlacedFurniture = placed;
        _titleLabel.Text = placed.Furniture.DisplayName;
        Visible = true;

        // Зум камеры к экспонату
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
        _storeConfirmDialog.PopupCentered();
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
        var museum = GetTree().CurrentScene as Museum;
        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
        camera?.ResetZoom();
    }
}