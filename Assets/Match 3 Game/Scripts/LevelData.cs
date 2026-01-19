using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BlockedCell
{
    public int x; // X coordinate of the blocked position
    public int y; // Y coordinate of the blocked position
}

[CreateAssetMenu(fileName = "Level Data", menuName = "Epic Loop/Level Data")]

public class LevelData : ScriptableObject
{
    public int gridWidth = 5; // Width of the grid
    public int gridHeight = 10; // Height of the grid
    public int GridSeed = 1; // Speed of the grid pieces falling
    public int SpecialPiecesAmount = 2;
    public int movesCount = 20; // Number of moves allowed in the level
    public PieceType target1Piece; // Type of piece counted for target 1
    public PieceType target2Piece; // Type of piece counted for target 2
    public int target1Count = 10; // Target count for the first type of piece
    public int target2Count = 5;
    public int timeLimit = 60; // Time limit for the level in seconds
    public bool isTimedLevel;
    public bool isMovesLevel;
    public BlockedCell[] blockedCells; //Array of blocked cells in the grid

}
