using Godot;

public partial class PlacedFurniture : Resource
{
    // Уникальный ID этого экземпляра мебели в зале
    [Export] public string InstanceId = "";
    
    // ID типа мебели (например, "display_case_2x1")
    [Export] public string FurnitureTypeId = "";
    
    // Позиция в сетке зала (верхний левый угол)
    public Vector2I Position;
    public Vector2I Size;
    
    // Ссылка на сам объект мебели (витрина/пьедестал)
    public Furniture Furniture;
}