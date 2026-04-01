using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationObserver : MonoBehaviour
{
    [SerializeField] float observationTime = 1.2f;
    [SerializeField] float idleTimeRequirement = 1f;
    [SerializeField] float linearThreshold = 0.01f;
    [SerializeField] float angularThreshold = 0.01f;

    public event Action<IEnumerable<Block>> SimulationEnded;

    readonly List<BodyData> blockData = new();
    bool isRunning = false;
    float simulationTimer = 0f;

    class BodyData
    {
        public Block Block { get; set; }
        public Rigidbody Rigidbody { get; set; }
        public float IdleTime { get; set; }
    }

    public void Initialize(IEnumerable<Block> blocks)
    {
        isRunning = true;

        foreach (Block block in blocks)
        {
            if (block.TryGetComponent(out Rigidbody rb))
            {
                BodyData data = new()
                {
                    Block = block,
                    Rigidbody = rb,
                    IdleTime = 0f
                };

                blockData.Add(data);
            }

            if (block.MobilityType == Block.Mobility.Free)
            {
                rb.isKinematic = false; // Activate physics
                block.Snapped = false;

                // Apply small random force
                // RandomExtensions.Shared.RandomDirection2D
            }
        }
    }

    void Update()
    {
        if (!isRunning) return;

        foreach (BodyData data in blockData)
            UpdateBody(data);

        if (simulationTimer < observationTime) simulationTimer += Time.deltaTime;
        else EndObservation();
    }

    void UpdateBody(BodyData data)
    {
        if (data.IdleTime > idleTimeRequirement) return;

        bool isMoving = data.Rigidbody.linearVelocity.magnitude > linearThreshold || data.Rigidbody.angularVelocity.magnitude > angularThreshold;

        if (isMoving) data.IdleTime = 0f;
        else data.IdleTime += Time.deltaTime;
    }

    void EndObservation()
    {
        isRunning = false;

        List<Block> unstableBlocks = new();
        
        foreach (BodyData data in blockData)
            if (data.IdleTime < idleTimeRequirement)
                unstableBlocks.Add(data.Block);

        SimulationEnded?.Invoke(unstableBlocks);
    }
}