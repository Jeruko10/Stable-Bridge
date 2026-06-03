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

    [MenuItem("Tools/Bake Levels from JSON")]
    static void Bake()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelsBakingConfig");
        if (guids.Length == 0)
        {
            Debug.LogError("LevelsBaker: Create a LevelsBakingConfig asset first (Assets > Create > Level > Baking Config).");
            return;
        }
        var cfg = AssetDatabase.LoadAssetAtPath<LevelsBakingConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

        string problemsText = File.ReadAllText($"{PythonDataPath}/Problems.json");
        var data = JsonConvert.DeserializeObject<ProblemsData>(problemsText);

        if (data.stage2 == null || data.stage2.Count == 0)
        {
            Debug.LogError("LevelsBaker: No stage2 entries in Problems.json.");
            return;
        }

        if (AssetDatabase.IsValidFolder(LevelsFolder))
            AssetDatabase.DeleteAsset(LevelsFolder);
        AssetDatabase.CreateFolder("Assets/Resources", "Levels");

        for (int i = 0; i < data.stage2.Count; i++)
        {
            Stage2Entry entry = data.stage2[i];
            string levelFolder = $"{LevelsFolder}/Level{i + 1}";
            string solutionsFolder = $"{levelFolder}/Solutions";
            AssetDatabase.CreateFolder(LevelsFolder, $"Level{i + 1}");
            AssetDatabase.CreateFolder(levelFolder, "Solutions");

            // Solutions
            var solutionAssets = new List<LevelSolution>();
            for (int s = 0; s < entry.solution_list.Count; s++)
            {
                var sol = ScriptableObject.CreateInstance<LevelSolution>();
                sol.Initialize(entry.solution_list[s].placements.Select(p => new LevelSolution.Placement
                {
                    blockId = p.id,
                    tile = new Vector2Int(p.position[0], p.position[1]),
                    rotation = ToRotation(p.orientation, p.is_inverted),
                    flipped = p.is_mirror
                }).ToList());
                AssetDatabase.CreateAsset(sol, $"{solutionsFolder}/Solution{s + 1}.asset");
                solutionAssets.Add(sol);
            }

            // Level layout
            var blocks = new List<BlockPlacementData>();

            foreach (var tower in entry.problem.towers)
            {
                if (cfg.towerPrefab == null) continue;
                blocks.Add(new BlockPlacementData(
                    blockPrefab: cfg.towerPrefab,
                    mobilityType: Block.Mobility.Fixed,
                    pivotIndex: 0,
                    flipped: false,
                    rotation: BoardGrid.Rotation.Deg0,
                    startingTile: new Vector2Int(tower[0], tower[1]),
                    slideTiles: new List<Vector2Int>()
                ));
            }

            foreach (var item in entry.problem.inventory)
            {
                Block prefab = cfg.FindPrefab(item.w, item.h, item.is_stair);
                if (prefab == null)
                {
                    Debug.LogWarning($"LevelsBaker: No prefab mapping for block ({item.w}x{item.h}, stair={item.is_stair}). Add it to LevelsBakingConfig.");
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
                    blockId: item.id
                ));
            }

            var layout = ScriptableObject.CreateInstance<LevelLayout>();
            layout.SetFromBaker(
                size: new Vector2Int(data.config.grid_w, data.config.grid_h),
                start: new Vector2Int(entry.problem.knight[0], entry.problem.knight[1]),
                end: new Vector2Int(entry.problem.princess[0], entry.problem.princess[1]),
                blockData: blocks,
                solutionData: solutionAssets
            );
            AssetDatabase.CreateAsset(layout, $"{levelFolder}/Level{i + 1}.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"LevelsBaker: Baked {data.stage2.Count} level(s) into {LevelsFolder}.");
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
        public List<Stage2Entry> stage2;
    }

    class GridConfig
    {
        public int grid_w;
        public int grid_h;
    }

    class Stage2Entry
    {
        public ProblemEntry problem;
        public List<SolutionEntry> solution_list;
    }

    class ProblemEntry
    {
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
        public string orientation;
        public bool is_mirror;
        public bool is_inverted;
    }
}
