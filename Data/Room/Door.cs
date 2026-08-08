using Godot;

public partial class Door : Resource
{
    // Направление двери (какая стена)
    [Export] public Direction Direction;
    
    // Позиция двери в сетке зала (всегда середина стены)
    public Vector2I Position;
    
    // ID соседнего зала (null, если зала нет — можно купить)
    public string ConnectedRoomId;
    
    // Это выход на улицу (только для двери вниз в главном зале)
    [Export] public bool IsExitToStreet = false;
    
    // Можно ли через эту дверь пройти (false = стена, можно купить зал)
    public bool HasConnection => !string.IsNullOrEmpty(ConnectedRoomId) || IsExitToStreet;
}