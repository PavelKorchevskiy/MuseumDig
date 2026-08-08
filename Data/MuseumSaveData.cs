using System.Collections.Generic;

public class MuseumSaveData
{
    public int CurrentRoomX { get; set; }
    public int CurrentRoomY { get; set; }
    public List<RoomSaveData> Rooms { get; set; }
}

public class RoomSaveData
{
    public string Id { get; set; }
    public int GlobalPositionX { get; set; }
    public int GlobalPositionY { get; set; }
    public List<PlacedFurnitureSaveData> Furniture { get; set; }
    public Dictionary<int, string> Doors { get; set; }
}

public class PlacedFurnitureSaveData
{
    public string InstanceId { get; set; }
    public string FurnitureTypeId { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeX { get; set; }
    public int SizeY { get; set; }
    public FurnitureSaveData FurnitureSaveData { get; set; }
}

public class FurnitureSaveData
{
    public string FurnitureType { get; set; }
    public string PedestalCollectionId { get; set; }
    public List<FoundItem> DisplayCaseItems { get; set; }
    public Dictionary<string, int> PedestalParts { get; set; }
}