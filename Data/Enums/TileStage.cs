using Godot;
using System;

// Стадия физического состояния плитки
public enum TileStage
{
    Intact,          // Целая
    Dug1,            // Откопана на 1/3
    Dug2,            // Откопана на 2/3
    FullyDug       // Откопана до конца (пустая)
}