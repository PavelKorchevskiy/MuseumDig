using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class ConcretionDefinition : ResourceDefinition
{
    public List<LootEntry> PossibleContents { get; set; } // Что может быть внутри
    public int HitsToOpen { get; set; } = 5;
}