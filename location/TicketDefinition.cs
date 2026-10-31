using Godot;

[GlobalClass]
public partial class TicketDefinition : Resource
{
    // Уникальный ID, например: "ticket_texas"
    [Export] public string Id = "";
    
    // Название для отображения в инвентаре и магазине
    [Export] public string DisplayName = "";
    
    // Описание (например: "Дает доступ к уникальным окаменелостям Перми")
    [Export] public string Description = "";
    
    // ГЛАВНОЕ: ID локации, на которую этот билет дает доступ
    [Export] public string TargetLocationId = "";
    
    // Цена покупки в магазине (0 = не продается, только выпадает)
    [Export] public int ShopPrice = 0;
    
    // Можно ли его продать обратно в инвентаре? (Для билетов обычно false)
    [Export] public bool IsSellable = false;
}