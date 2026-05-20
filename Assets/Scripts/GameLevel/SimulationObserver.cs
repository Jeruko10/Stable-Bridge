using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
public class SimulationObserver : MonoBehaviour
{
    [Header("Simulation Frames")]
    [SerializeField] int stabilityRequiredFrames = 60;
    [SerializeField] int unstabilityFrameLimit = 500;

    [Header("Stability Detection")]
    [SerializeField] float positionThreshold = 0.001f;
    [SerializeField] float rotationThreshold = 0.1f;

    [Header("Movement Detection")]
    [SerializeField] float linearVelocityThreshold = 0.01f;
    [SerializeField] float angularVelocityThreshold = 1f;

    [Header("Space Limitation")]
    [SerializeField] float minHeight = -10f;

    [Header("Debugging")]
    [SerializeField] bool showDebugColors = false;

    public event Action SimulationEnded;
    public event Action<IEnumerable<Block>> StabilityKnown;

    readonly Dictionary<Block, StabilityState> blockStates = new();
    readonly Dictionary<Block, int> blockStabilityTimers = new();
    readonly Dictionary<Block, Vector3> originPositions = new();
    readonly Dictionary<Block, Quaternion> originRotations = new();
    bool simulationFinished = true, stabilityChecked = false, instantSimulation = false;
    BoardGrid grid;
    int simulationFramesTimer = 0;

    enum StabilityState { Pendant, Stable, Unstable }

    void Awake()
    {
        grid = GetComponent<BoardGrid>();
    }

    public void Initialize(IEnumerable<Block> blocks, bool instantSimulation = false)
    {
        Physics.simulationMode = SimulationMode.Script;

        simulationFinished = false;
        stabilityChecked = false;
        this.instantSimulation = instantSimulation;
        simulationFramesTimer = 0;
        blockStates.Clear();
        blockStabilityTimers.Clear();
        originPositions.Clear();
        originRotations.Clear();

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
            originPositions.Add(block, block.transform.position);
            originRotations.Add(block, block.transform.rotation);
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

        if (showDebugColors) DebugColorBlocks();
    }

    void CheckStability()
    {
        stabilityChecked = true;
        List<Block> unstableBlocks = new();

        foreach (var pair in blockStates)
        {
            if (pair.Value == StabilityState.Stable) pair.Key.Rigidbody.isKinematic = true;
            else unstableBlocks.Add(pair.Key);
        }

        StabilityKnown?.Invoke(unstableBlocks);
    }

    void UpdateBlockState(Block block)
    {        
        if (!blockStates.ContainsKey(block)) return;

        bool positionUnstable = Vector3.Distance(block.transform.position, originPositions[block]) > positionThreshold;
        bool rotationUnstable = Quaternion.Angle(block.transform.rotation, originRotations[block]) > rotationThreshold;
        bool isMoving = block.Rigidbody.linearVelocity.magnitude > linearVelocityThreshold || block.Rigidbody.angularVelocity.magnitude > angularVelocityThreshold;

        if ((!isMoving || block.transform.position.y < minHeight) && blockStates[block] == StabilityState.Unstable)
        {
            if (!isMoving) Debug.Log($"{block.name} broken: idle");
            if (block.transform.position.y < minHeight) Debug.Log($"{block.name} broken: falling");
            
            RemoveBlock(block);
            return;
        }

        if (blockStates[block] == StabilityState.Pendant)
        {
            if (positionUnstable || rotationUnstable)
            {
                if (positionUnstable) Debug.Log($"{block.name} unstable: position");
                if (rotationUnstable) Debug.Log($"{block.name} unstable: rotation");

                blockStates[block] = StabilityState.Unstable;
                return;
            }

            blockStabilityTimers[block]++;
            if (blockStabilityTimers[block] >= stabilityRequiredFrames) blockStates[block] = StabilityState.Stable;
        }
    }

    bool AllBlocksStable() => blockStates.Values.All(state => state == StabilityState.Stable);

    void RemoveBlock(Block block)
    {
        blockStates.Remove(block);
        blockStabilityTimers.Remove(block);
        originPositions.Remove(block);
        originRotations.Remove(block);
        grid.RemoveBlock(block);
        block.Destroy();
    }

    void EndSimulation()
    {
        simulationFinished = true;

        if (!stabilityChecked) CheckStability();

        foreach (Block block in blockStates.Keys.ToList())
            if (blockStates[block] == StabilityState.Unstable) RemoveBlock(block);

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
                _ => Color.white
            };
        }
    }
}