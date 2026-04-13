using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        LevelManager.Current.SuccessKnown += OnSuccessKnown;
    }

    void OnSuccessKnown(bool success)
    {
        if (success)
        {
            // TODO
        }
        else
        {
            // BLa
        }
    }
}