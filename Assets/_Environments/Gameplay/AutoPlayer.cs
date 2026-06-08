using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class AutoPlayer : MonoBehaviour
{
    enum FailReason { Unstable, IncompletePath }

    struct Result
    {
        public int levelIndex;
        public int solutionIndex;
        public bool passed;
        public FailReason? failReason;
    }

    [ContextMenu("Run")]
    public void Run() => StartCoroutine(RunAll());

    IEnumerator RunAll()
    {
        LevelLayout[] layouts = Resources.LoadAll<LevelLayout>("Levels")
            .OrderBy(l => { var m = Regex.Match(l.name, @"\d+"); return m.Success ? int.Parse(m.Value) : 0; })
            .ToArray();

        var results = new List<Result>();

        for (int li = 0; li < layouts.Length; li++)
        {
            LevelLayout layout = layouts[li];

            if (layout.Solutions.Count == 0)
                continue;

            for (int si = 0; si < layout.Solutions.Count; si++)
            {
                LevelManager.LoadLevel(li);
                Level level = LevelManager.Current;
                level.SkipProgression = true;

                bool hadUnstableBlocks = false;
                level.SimulationObserver.StabilityKnown += blocks =>
                    hadUnstableBlocks = blocks.Any();

                bool done = false;
                bool levelSuccess = false;
                level.LevelComplete += s => { levelSuccess = s; done = true; };

                ApplySolution(level, layout, layout.Solutions[si]);
                level.ExitEditMode();

                yield return new WaitUntil(() => done);

                results.Add(new Result
                {
                    levelIndex = li,
                    solutionIndex = si,
                    passed = levelSuccess,
                    failReason = levelSuccess ? (FailReason?)null :
                        hadUnstableBlocks ? FailReason.Unstable : FailReason.IncompletePath
                });
            }
        }

        LevelManager.ExitLevel();
        PrintSummary(results, layouts);
    }

    static void ApplySolution(Level level, LevelLayout layout, LevelSolution solution)
    {
        Dictionary<int, Block> blockMap = BuildBlockMap(level, layout);

        foreach (LevelSolution.Placement placement in solution.Placements)
        {
            if (!blockMap.TryGetValue(placement.blockId, out Block block))
            {
                Debug.LogWarning($"[AutoPlayer] Block id {placement.blockId} not found in level.");
                continue;
            }

            block.SetRotation(block.Pivot, placement.rotation);
            if (block.IsFlipped != placement.flipped)
                block.Flip(block.Pivot);

            switch (block.MobilityType)
            {
                case Block.Mobility.Free:
                    if (!level.Grid.TryPlaceBlock(block, placement.tile, block.Pivot))
                        Debug.LogWarning($"[AutoPlayer] Failed to place block {block.name} at {placement.tile}.");
                    else
                        level.Inventory.Remove(block);
                    break;

                case Block.Mobility.SlideOnly:
                    int idx = Array.IndexOf(block.SlidePositions, placement.tile);
                    if (idx >= 0) block.SlidePositionIndex = idx;
                    break;

                // RotateOnly: rotation already applied above, tile is fixed
            }
        }
    }

    static Dictionary<int, Block> BuildBlockMap(Level level, LevelLayout layout)
    {
        var map = new Dictionary<int, Block>();
        int inventoryIndex = 0;

        foreach (BlockPlacementData data in layout.Blocks)
        {
            Block block;

            if (data.MobilityType == Block.Mobility.Free)
            {
                if (inventoryIndex >= level.Inventory.Count) continue;
                block = level.Inventory[inventoryIndex++];
            }
            else
            {
                Vector2Int lookupTile = data.MobilityType == Block.Mobility.SlideOnly && data.SlideTiles.Count > 0
                    ? data.SlideTiles[0]
                    : data.StartingTile;
                block = level.Grid.GetBlockAtTile(lookupTile)?.GetParent();
                if (block == null) continue;
            }

            map[data.BlockId] = block;
        }

        return map;
    }

    static void PrintSummary(List<Result> results, LevelLayout[] layouts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n========== AUTO PLAYER SUMMARY ==========");

        for (int li = 0; li < layouts.Length; li++)
        {
            List<Result> levelResults = results.Where(r => r.levelIndex == li).ToList();

            if (levelResults.Count == 0)
            {
                sb.AppendLine($"Level {li + 1}: No solutions registered");
                continue;
            }

            bool levelOk = levelResults.All(r => r.passed);
            sb.AppendLine($"Level {li + 1}: {(levelOk ? "OK" : "FAIL")}");

            foreach (Result r in levelResults)
            {
                string detail = r.passed ? "PASS" : r.failReason switch
                {
                    FailReason.Unstable       => "FAIL — unstable blocks",
                    FailReason.IncompletePath => "FAIL — incomplete path",
                    _                         => "FAIL"
                };
                sb.AppendLine($"  Solution {r.solutionIndex + 1}: {detail}");
            }
        }

        int passCount = results.Count(r => r.passed);
        int totalCount = results.Count;
        sb.AppendLine(passCount == totalCount
            ? $"\nAll {totalCount} solution(s) passed!"
            : $"\n{passCount}/{totalCount} solutions passed.");
        sb.AppendLine("==========================================");

        Debug.Log(sb.ToString());
    }
}
