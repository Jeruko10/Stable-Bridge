using UnityEngine;

public class BoardView : MonoBehaviour
{
    public GameObject PiecePrefab;

    public void Render(BoardState board)
    {
        foreach (var piece in board.Pieces.Values)
        {
            var go = Instantiate(PiecePrefab, transform);
            var view = go.GetComponent<PieceView>();

            view.Initialize(piece);
        }
    }
}