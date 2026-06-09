using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class LevelsBaker
{
    const string LevelsFolder = "Assets/Resources/Levels";
    const string PythonDataPath = "Assets/Resources/PythonData";
    const int YOffset = 1;

    [MenuItem("Tools/Bake Levels from JSON")]
    static void Bake()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelsBakingConfig");
        if (guids.Length == 0)
        {
            Debug.LogError("LevelsBaker: Create a LevelsBakingConfig asset first (Assets > Create > Baking Config).");
            return;
        }
        var cfg = AssetDatabase.LoadAssetAtPath<LevelsBakingConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

        var data = JsonConvert.DeserializeObject<ProblemsData>(
            File.ReadAllText($"{PythonDataPath}/Problems.json"));

        var solutions = JsonConvert.DeserializeObject<Dictionary<string, List<SolutionEntry>>>(
            File.ReadAllText($"{PythonDataPath}/Solutions.json"));

        if (data.problems == null || data.problems.Count == 0)
        {
            Debug.LogError("LevelsBaker: No problems in Problems.json.");
            return;
        }

        if (AssetDatabase.IsValidFolder(LevelsFolder))
            AssetDatabase.DeleteAsset(LevelsFolder);
        AssetDatabase.CreateFolder("Assets/Resources", "Levels");

        for (int i = 0; i < data.problems.Count; i++)
        {
            ProblemEntry problem = data.problems[i];
            string levelFolder = $"{LevelsFolder}/Level{i + 1}";
            string solutionsFolder = $"{levelFolder}/Solutions";
            AssetDatabase.CreateFolder(LevelsFolder, $"Level{i + 1}");
            AssetDatabase.CreateFolder(levelFolder, "Solutions");

            // Solutions from Solutions.json keyed by problem index
            var solutionAssets = new List<LevelSolution>();
            string key = problem.index.ToString();
            if (solutions.TryGetValue(key, out var solutionList))
            {
                for (int s = 0; s < solutionList.Count; s++)
                {
                    var sol = ScriptableObject.CreateInstance<LevelSolution>();
                    sol.Initialize(solutionList[s].placements.Select(p =>
                    {
                        // HORIZ: pivot is rightmost cell when mirrored (flip X moves root to right end).
                        // Inverted (flip Y) = Deg180 + toggleFlip, which cancels mirror's shift.
                        bool needsCorrectionH = p.orientation != "VERT" && p.is_mirror;
                        // VERT: pivot is topmost cell only when inverted (Deg270).
                        bool needsCorrectionV = p.orientation == "VERT" && p.is_inverted;
                        int dx = needsCorrectionH ? p.size[0] - 1 : 0;
                        int dy = needsCorrectionV ? p.size[1] - 1 : 0;
                        return new LevelSolution.Placement
                        {
                            blockId  = p.id - 10,
                            tile     = new Vector2Int(p.position[0] + dx, p.position[1] + dy + YOffset),
                            rotation = ToRotation(p.orientation, p.is_inverted),
                            flipped  = p.is_mirror ^ p.is_inverted
                        };
                    }).ToList());
                    AssetDatabase.CreateAsset(sol, $"{solutionsFolder}/Solution{s + 1}.asset");
                    solutionAssets.Add(sol);
                }
            }
            else
            {
                Debug.LogWarning($"LevelsBaker: No solutions found for problem index {problem.index}.");
            }

            // Blocks
            var blocks = new List<BlockPlacementData>();

            foreach (var tower in problem.towers)
            {
                if (cfg.towerPrefab == null) continue;
                blocks.Add(new BlockPlacementData(
                    blockPrefab: cfg.towerPrefab,
                    mobilityType: Block.Mobility.Fixed,
                    pivotIndex: 0,
                    flipped: false,
                    rotation: BoardGrid.Rotation.Deg0,
                    startingTile: new Vector2Int(tower[0], tower[1] + YOffset),
                    slideTiles: new List<Vector2Int>()
                ));
            }

            foreach (var item in problem.inventory)
            {
                Block prefab = cfg.FindPrefab(item.w, item.h, item.is_stair);
                if (prefab == null)
                {
                    Debug.LogWarning($"LevelsBaker: No prefab for block ({item.w}x{item.h}, stair={item.is_stair}). Add it to LevelsBakingConfig.");
                    continue;
                }
                blocks.Add(new BlockPlacementData(
                    blockPrefab: prefab,
                    mobilityType: Block.Mobility.Free,
                    pivotIndex: cfg.FindPivotIndex(item.w, item.h, item.is_stair),
                    flipped: false,
                    rotation: BoardGrid.Rotation.Deg0,
                    startingTile: Vector2Int.zero,
                    slideTiles: new List<Vector2Int>(),
                    blockId: item.id - 10
                ));
            }

            var layout = ScriptableObject.CreateInstance<LevelLayout>();
            layout.SetFromBaker(
                size: new Vector2Int(data.config.grid_w, data.config.grid_h),
                start: new Vector2Int(problem.knight[0], problem.knight[1] + YOffset),
                end: new Vector2Int(problem.princess[0], problem.princess[1] + YOffset),
                blockData: blocks,
                solutionData: solutionAssets
            );
            AssetDatabase.CreateAsset(layout, $"{levelFolder}/Level{i + 1}.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"LevelsBaker: Baked {data.problems.Count} level(s) into {LevelsFolder}.");
    }

    static BoardGrid.Rotation ToRotation(string orientation, bool isInverted) => orientation switch
    {
        "VERT" => isInverted ? BoardGrid.Rotation.Deg270 : BoardGrid.Rotation.Deg90,
        _      => isInverted ? BoardGrid.Rotation.Deg180 : BoardGrid.Rotation.Deg0,
    };

    // ── JSON data classes ────────────────────────────────────────────────────

    class ProblemsData
    {
        public GridConfig config;
        public List<ProblemEntry> problems;
    }

    class GridConfig
    {
        public int grid_w;
        public int grid_h;
    }

    class ProblemEntry
    {
        public int index;
        public int[] knight;
        public int[] princess;
        public List<int[]> towers;
        public List<InventoryEntry> inventory;
    }

    class InventoryEntry
    {
        public int id;
        public int w;
        public int h;
        public bool is_stair;
    }

    class SolutionEntry
    {
        public List<PlacementEntry> placements;
    }

    class PlacementEntry
    {
        public int id;
        public int[] position;
        public int[] size;
        public string orientation;
        public bool is_mirror;
        public bool is_inverted;
    }
}
