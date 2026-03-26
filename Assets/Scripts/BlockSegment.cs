using UnityEngine;

public class BlockSegment : MonoBehaviour
{
    [SerializeField] NavigationMatrix mobilityAt0Degrees = new();
    [SerializeField] NavigationMatrix mobilityAt90Degrees = new();
    [SerializeField] NavigationMatrix mobilityAt270Degrees = new();
    [SerializeField] NavigationMatrix mobilityAt360Degrees = new();
}