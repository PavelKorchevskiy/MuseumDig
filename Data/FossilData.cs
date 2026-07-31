using Godot;

[GlobalClass]
public partial class FossilData : Resource
{
	[Export] public string FossilId = "";
	[Export] public string PieceName = "";
	[Export] public int TotalPieces = 4;
	[Export] public int PieceIndex = 0;
}
