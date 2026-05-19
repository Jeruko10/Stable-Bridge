using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SimulationObserver : MonoBehaviour
{
    [SerializeField] int stabilityRequiredFrames = 60;
    [SerializeField] int unstabilityFrameLimit = 500;
    [SerializeField] float positionThreshold = 0.001f;
    [SerializeField] float rotationThreshold = 0.1f;
    [SerializeField] float removalImpulse = 10000f;

    public event Action SimulationEnded;
    public event Action<IEnumerable<Block>> StabilityKnown;

    readonly Dictionary<Block, StabilityState> blockStates = new();
    readonly Dictionary<Block, int> blockStabilityTimers = new();
    readonly Dictionary<Block, Vector3> prevPositions = new();
    readonly Dictionary<Block, Quaternion> prevRotations = new();
    bool simulationFinished = true, stabilityChecked = false, instantSimulation = false;
    int simulationFramesTimer = 0;

    enum StabilityState { Pendant, Stable, Unstable, Removed }

    public void Initialize(IEnumerable<Block> blocks, bool instantSimulation = false)
    {
        Physics.simulationMode = SimulationMode.Script;

        simulationFinished = false;
        stabilityChecked = false;
        this.instantSimulation = instantSimulation;
        simulationFramesTimer = 0;
        blockStates.Clear();
        blockStabilityTimers.Clear();
        prevPositions.Clear();
        prevRotations.Clear();

        foreach (Block block in blocks)
        {
            StabilityState initialState;

            if (block.MobilityType == Block.Mobility.Free || block.MobilityType == Block.Mobility.Fixed)
            {
                initialState = StabilityState.Pendant;
                block.SetPhysics(true);
            }
            else initialState = StabilityState.Stable;

            blockStates.Add(block, initialState);
            blockStabilityTimers.Add(block, 0);
            prevPositions.Add(block, block.transform.position);
            prevRotations.Add(block, block.transform.rotation);
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

        DebugColorBlocks();
    }

    void CheckStability()
    {
        stabilityChecked = true;
        List<Block> unstableBlocks = new();

        foreach (var pair in blockStates)
            if (pair.Value == StabilityState.Stable) pair.Key.Rigidbody.isKinematic =  true;
            else unstableBlocks.Add(pair.Key);

        StabilityKnown?.Invoke(unstableBlocks);
    }

    void UpdateBlockState(Block block)
    {        
        if (blockStates[block] == StabilityState.Removed) return;

        bool positionMoved = Vector3.Distance(block.transform.position, prevPositions[block]) > positionThreshold;
        bool rotationMoved = Quaternion.Angle(block.transform.rotation, prevRotations[block]) > rotationThreshold;
        bool isMoving = positionMoved || rotationMoved;

        prevPositions[block] = block.transform.position;
        prevRotations[block] = block.transform.rotation;

        if (!isMoving && blockStates[block] == StabilityState.Unstable) RemoveBlock(block);

        if (blockStates[block] == StabilityState.Pendant)
        {
            if (isMoving)
            {
                if (positionMoved) Debug.Log($"{block.name} unstable: position delta > {positionThreshold}");
                if (rotationMoved) Debug.Log($"{block.name} unstable: rotation delta > {rotationThreshold}°");
                blockStates[block] = StabilityState.Unstable;
                return;
            }

            blockStabilityTimers[block]++;
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

        if (!stabilityChecked) CheckStability();

        foreach (var pair in blockStates)
            if (pair.Value == StabilityState.Unstable) RemoveBlock(pair.Key);

        Physics.simulationMode = SimulationMode.FixedUpdate;
        SimulationEnded?.Invoke();
    }

    void DebugColorBlocks()
    {
        foreach (var pair in blockStates)
        {
            pair.Key.Color = pair.Value switch
            {
                StabilityState.Pendant => Color.yellow,
                StabilityState.Stable => Color.green,
                StabilityState.Unstable => Color.red,
                StabilityState.Removed => Color.darkRed,
                _ => Color.white
            };
        }
    }
}