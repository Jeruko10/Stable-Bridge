using System.Collections.Generic;
using UnityEngine;

public class BlockSegment : MonoBehaviour
{
    [SerializeField] NavigationMatrix mobilityAt0Degrees = new();
    [SerializeField] NavigationMatrix mobilityAt90Degrees = new();
    [SerializeField] NavigationMatrix mobilityAt270Degrees = new();
    [SerializeField] NavigationMatrix mobilityAt360Degrees = new();
    
    public Mobility MobilityType;

    public enum Mobility
    {
        Free,
        RotateOnly,
        SlideOnly,
        Pinned
    }

    void Start()
    {
    }
}
