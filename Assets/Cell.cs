using UnityEngine;

public struct Cell
{
    public byte type; // 0: Air, 1: Sand, 2: Wall, 3: ConveyorRight, 4: ConveyorLeft
    public Color32 color;
    public bool hasMoved;
}
