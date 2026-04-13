using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationObserver : MonoBehaviour
{
    [SerializeField] bool instantSimulation = false;
    [SerializeField] int stabilityRequiredFrames = 60;
    [SerializeField] int unstabilityFrameLimit = 500;
    [SerializeField] float linearThreshold = 0.01f;
    [SerializeField] float angularThreshold = 0.01f;
    [SerializeField] float removalImpulse = 10000f;

    public event Action SimulationEnded;
    public event Action<IEnumerable<Block>> StabilityKnown;

    readonly Dictionary<Block, StabilityState> blockStates = new();
    readonly Dictionary<Block, float> blockStabilityTimers = new();
    bool simulationFinished = true, stabilityChecked = false;
    int simulationFramesTimer = 0;

    enum StabilityState { Pendant, Stable, Unstable, Removed }

    public void Initialize(IEnumerable<Block> blocks)
    {
        Physics.simulationMode = SimulationMode.Script;

        simulationFinished = false;
        stabilityChecked = false;
        simulationFramesTimer = 0;
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

    void FixedUpdate()
    {
        if (simulationFinished) return;

        int framesToProcess = instantSimulation ? stabilityRequiredFrames : 1;

        for (int i = 0; i < framesToProcess; i++)
        {
            if (simulationFinished) break;

            Physics.Simulate(Time.fixedDeltaTime);
            
            if (simulationFramesTimer >= stabilityRequiredFrames && !stabilityChecked) CheckStability();

            if (AllBlocksStable() || simulationFramesTimer >= unstabilityFrameLimit)
            {
                EndSimulation();
                break;
            }

            simulationFramesTimer++;

            List<Block> blocks = blockStates.Keys.ToList();
            foreach (Block block in blocks) UpdateBlockState(block);
        }
    }

    void CheckStability()
    {
        stabilityChecked = true;
        List<Block> unstableBlocks = new();

        foreach (var pair in blockStates)
            if (pair.Value != StabilityState.Stable) unstableBlocks.Add(pair.Key);
            else pair.Key.SetPhysics(false);

        StabilityKnown?.Invoke(unstableBlocks);
    }

    void UpdateBlockState(Block block)
    {
        if (blockStates[block] == StabilityState.Removed) return;

        bool isMoving = block.Rigidbody.linearVelocity.magnitude > linearThreshold || block.Rigidbody.angularVelocity.magnitude > angularThreshold;
        
        if (!isMoving && blockStates[block] == StabilityState.Unstable) RemoveBlock(block);

        if (blockStates[block] == StabilityState.Pendant)
        {
            if (isMoving) { blockStates[block] = StabilityState.Unstable; return; }

            blockStabilityTimers[block] += Time.fixedDeltaTime;
            if (blockStabilityTimers[block] >= stabilityRequiredFrames) blockStates[block] = StabilityState.Stable;
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

        foreach (var pair in blockStates)
            if (pair.Value != StabilityState.Removed) pair.Key.SetPhysics(false);

        Physics.simulationMode = SimulationMode.FixedUpdate;
        SimulationEnded?.Invoke();
    }
}