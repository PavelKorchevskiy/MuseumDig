using Godot;

// Наследуемся от TextureButton. Это решает 99% проблем с кликами в Godot 4.
public partial class DroppedTicket : TextureButton
{
    private string _ticketId;

    public override void _Ready()
    {
        // Гарантируем, что кнопка ловит клики и находится поверх всего
        MouseFilter = MouseFilterEnum.Stop;
        ZAsRelative = false;
        ZIndex = 1000;
        
        // Задаем минимальный размер для удобного клика (хитбокс)
        CustomMinimumSize = new Vector2(64, 64);

        // Подписываемся на встроенный сигнал нажатия кнопки
        Pressed += OnClicked;
    }

    public void Setup(string ticketId, Vector2 worldPosition)
    {
        _ticketId = ticketId;
        Position = worldPosition;

        // Используем правильный путь к ассету
        string iconPath = $"res://assets/museum/tickets/{_ticketId}.png";
        if (ResourceLoader.Exists(iconPath))
        {
            TextureNormal = GD.Load<Texture2D>(iconPath);
        }
        else
        {
            // Fallback, если специфичной иконки нет
            TextureNormal = GD.Load<Texture2D>("res://assets/museum/tickets/ticket.png");
        }

        // Анимация появления: подпрыгивание + вращение
        var tween = CreateTween();
        tween.SetParallel();
        
        tween.TweenProperty(this, "position:y", Position.Y - 30f, 0.3f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "position:y", Position.Y, 0.3f)
             .SetDelay(0.3f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
             
        tween.TweenProperty(this, "rotation", Mathf.Pi * 2, 0.6f)
             .SetTrans(Tween.TransitionType.Sine);
    }

    private void OnClicked()
    {
        GD.Print($"[DroppedTicket] ✅ Клик успешен! Собираем: {_ticketId}");
        
        // Отключаем кнопку, чтобы нельзя было кликнуть дважды
        Disabled = true;
        
        // Добавляем в инвентарь
        InventorySystem.Instance.AddItem(_ticketId, 1);
        
        // Плавное исчезновение
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, 0.2f);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.2f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}