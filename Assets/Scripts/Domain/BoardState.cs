using System.Collections.Generic;
using UnityEngine;

public class BoardState
{
    public Dictionary<Vector3Int, PieceInstance> Pieces = new();

    public void AddPiece(PieceInstance piece)
    {
        Pieces[piece.Position] = piece;
    }

    public PieceInstance GetPiece(Vector3Int position)
    {
        Pieces.TryGetValue(position, out var piece);
        return piece;
    }
}