using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class FossilInventory : Node
{
	public static FossilInventory Instance { get; private set; }
	
	private Dictionary<string, HashSet<int>> _collectedPieces = new();
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public bool AddPiece(string fossilId, int pieceIndex)
	{
		if (!_collectedPieces.ContainsKey(fossilId))
		{
			_collectedPieces[fossilId] = new HashSet<int>();
		}
		
		if (_collectedPieces[fossilId].Contains(pieceIndex))
		{
			GD.Print($"Piece {pieceIndex} of '{fossilId}' already collected (duplicate)");
			return false;
		}
		
		_collectedPieces[fossilId].Add(pieceIndex);
		GD.Print($"Collected piece {pieceIndex} of fossil '{fossilId}' ({_collectedPieces[fossilId].Count}/4)");
		
		if (IsFossilComplete(fossilId))
		{
			GD.Print($"Fossil '{fossilId}' is complete!");
		}
		
		SaveSystem.Instance?.MarkDirty(); // <-- ДОБАВЛЕНО
		return true;
	}
	
	public bool IsFossilComplete(string fossilId)
	{
		if (!_collectedPieces.ContainsKey(fossilId))
			return false;
		
		return _collectedPieces[fossilId].Count >= 4;
	}
	
	public Dictionary<string, HashSet<int>> GetAllPieces()
	{
		return _collectedPieces;
	}
	
	// Конвертация HashSet -> List для JSON
	public Dictionary<string, List<int>> GetSaveData()
	{
		var result = new Dictionary<string, List<int>>();
		foreach (var kvp in _collectedPieces)
		{
			result[kvp.Key] = kvp.Value.ToList();
		}
		return result;
	}
	
	// Загрузка из сохранения
	public void LoadFromSaveData(Dictionary<string, List<int>> data)
	{
		_collectedPieces.Clear();
		if (data == null) return;
		
		foreach (var kvp in data)
		{
			_collectedPieces[kvp.Key] = new HashSet<int>(kvp.Value);
		}
		GD.Print($"Loaded {_collectedPieces.Count} fossil types");
	}
}
