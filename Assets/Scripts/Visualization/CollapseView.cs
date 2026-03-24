using UnityEngine;

public class CollapseView : MonoBehaviour
{
    public void TriggerCollapse()
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
        }
    }
}