using UnityEngine;

[System.Serializable]
public class NavigationMatrix
{
    [SerializeField] bool topLeft, topCenter, topRight, left, center, right, bottomLeft, bottomCenter, bottomRight;

    public bool[] Values
    {
        get
        {
            return new bool[]
            {
                topLeft, topCenter, topRight,
                left, center, right,
                bottomLeft, bottomCenter, bottomRight
            };
        }
    }
}