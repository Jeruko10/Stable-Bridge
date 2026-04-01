using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationObserver : MonoBehaviour
{
    [SerializeField] float stabilityRequiredTime = 1f;
    [SerializeField] float unstabilityTimeLimit = 8f;
    [SerializeField] float linearThreshold = 0.01f;
    [SerializeField] float angularThreshold = 0.01f;
    [SerializeField] float removalImpulse = 10000f;

    public event Action<IEnumerable<Block>> SimulationEnded;

    readonly Dictionary<Block, StabilityState> blockStates = new();
    readonly Dictionary<Block, float> blockStabilityTimers = new();
    bool simulationFinished = true;
    float simulationTimer = 0f;

    enum StabilityState { Pendant, Stable, Unstable, Removed }

    public void Initialize(IEnumerable<Block> blocks)
    {
        simulationFinished = false;
        blockStates.Clear();
        blockStabilityTimers.Clear();

        foreach (Block block in blocks)
        {
            StabilityState initialState;
            
            if (block.MobilityType == Block.Mobility.Free)
            {
                initialState = StabilityState.Pendant;
                block.SetPhysics(true);
            }
            else initialState = StabilityState.Stable;

            blockStates.Add(block, initialState);
            blockStabilityTimers.Add(block, 0f);
        }
    }

    void Update()
    {
        if (simulationFinished) return;

        if (AllBlocksStable() || simulationTimer >= unstabilityTimeLimit)
        {
            EndSimulation();
            return;
        }

        simulationTimer += Time.deltaTime;

        List<Block> blocks = blockStates.Keys.ToList();

        foreach (Block block in blocks)
            UpdateBlockState(block);
    }

    void UpdateBlockState(Block block)
    {
        if (blockStates[block] == StabilityState.Removed) return;

        bool isMoving = block.Rigidbody.linearVelocity.magnitude > linearThreshold || block.Rigidbody.angularVelocity.magnitude > angularThreshold;
        
        if (!isMoving && blockStates[block] == StabilityState.Unstable)
            RemoveBlock(block);

        if (blockStates[block] == StabilityState.Pendant)
        {
            if (isMoving)
            {
                blockStates[block] = StabilityState.Unstable;
                return;
            }

            blockStabilityTimers[block] += Time.deltaTime;

            if (blockStabilityTimers[block] >= stabilityRequiredTime)
                blockStates[block] = StabilityState.Stable;
        }
    }

    bool AllBlocksStable() => blockStates.Values.All(state => state == StabilityState.Stable || state == StabilityState.Removed);

    void RemoveBlock(Block block)
    {
        blockStates[block] = StabilityState.Removed;
        block.Rigidbody.AddForce(new(0f, 0f, -removalImpulse));
    }

    void EndSimulation()
    {
        simulationFinished = true;
        List<Block> removedBlocks = new();

        foreach (var pair in blockStates)
            if (pair.Value == StabilityState.Removed) removedBlocks.Add(pair.Key);
            else pair.Key.SetPhysics(false);

        SimulationEnded?.Invoke(removedBlocks);
    }
}