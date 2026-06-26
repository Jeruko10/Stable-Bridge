using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DataCollectionManager : MonoBehaviour
{
    const string SupabaseUrl = "https://jwtfydyzxqvjpwotbwaf.supabase.co/rest/v1";

    [SerializeField] string supabaseAnonKey;

    public static DataCollectionManager Instance { get; private set; }

    public string ParticipantId { get; private set; }
    public int Age { get; private set; }
    public string Gender { get; private set; }
    public bool IsParticipantSet { get; private set; }

    string sessionId;
    DateTime sessionStartTime;
    int hintsUsed;
    int currentLevelIndex;
    bool sessionActive;

    readonly List<TransformationData> transformations = new();

    struct TransformationData
    {
        public string type;
        public int blockIndex;
        public string blockType;
        public float secondsElapsed;
        public int rotationBefore, rotationAfter;
        public bool flippedBefore, flippedAfter;
        public int? tileXBefore, tileYBefore;
        public int? tileXAfter, tileYAfter;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => LevelManager.LevelLoaded += OnLevelLoaded;

    void OnDestroy() => LevelManager.LevelLoaded -= OnLevelLoaded;

    public void SetParticipant(int age, string gender)
    {
        ParticipantId = Guid.NewGuid().ToString();
        Age = age;
        Gender = gender;
        IsParticipantSet = true;
    }

    void OnLevelLoaded(Level level)
    {
        currentLevelIndex = LevelManager.LastLevelIndex;
        sessionId = Guid.NewGuid().ToString();
        sessionStartTime = DateTime.UtcNow;
        hintsUsed = 0;
        transformations.Clear();
        sessionActive = true;
        level.LevelComplete += OnLevelComplete;
    }

    void OnLevelComplete(bool success)
    {
        if (!sessionActive) return;
        sessionActive = false;

        float completionSeconds = (float)(DateTime.UtcNow - sessionStartTime).TotalSeconds;
        StartCoroutine(SubmitSession(success, completionSeconds));
    }

    public void RecordRotation(Block block, int rotationBefore, int rotationAfter)
    {
        if (!sessionActive) return;
        transformations.Add(new TransformationData
        {
            type = "rotate",
            blockIndex = GetBlockIndex(block),
            blockType = block.Prefab != null ? block.Prefab.name : block.name,
            secondsElapsed = ElapsedSeconds(),
            rotationBefore = rotationBefore,
            rotationAfter = rotationAfter,
        });
    }

    public void RecordFlip(Block block, bool flippedBefore, bool flippedAfter)
    {
        if (!sessionActive) return;
        transformations.Add(new TransformationData
        {
            type = "flip",
            blockIndex = GetBlockIndex(block),
            blockType = block.Prefab != null ? block.Prefab.name : block.name,
            secondsElapsed = ElapsedSeconds(),
            flippedBefore = flippedBefore,
            flippedAfter = flippedAfter,
        });
    }

    public void RecordMove(Block block, Vector2Int? tileBefore, Vector2Int tileAfter)
    {
        if (!sessionActive) return;
        transformations.Add(new TransformationData
        {
            type = "move",
            blockIndex = GetBlockIndex(block),
            blockType = block.Prefab != null ? block.Prefab.name : block.name,
            secondsElapsed = ElapsedSeconds(),
            tileXBefore = tileBefore?.x,
            tileYBefore = tileBefore?.y,
            tileXAfter = tileAfter.x,
            tileYAfter = tileAfter.y,
        });
    }

    public void RecordHint() { if (sessionActive) hintsUsed++; }

    float ElapsedSeconds() => (float)(DateTime.UtcNow - sessionStartTime).TotalSeconds;

    int GetBlockIndex(Block block) => LevelManager.Current != null ? LevelManager.Current.Inventory.IndexOf(block) : -1;

    IEnumerator SubmitSession(bool success, float completionSeconds)
    {
        if (!IsParticipantSet)
        {
            Debug.LogWarning("[DataCollection] Session not saved: participant not set.");
            yield break;
        }

        if (string.IsNullOrEmpty(supabaseAnonKey))
        {
            Debug.LogError("[DataCollection] Supabase anon key not set in Inspector.");
            yield break;
        }

        yield return StartCoroutine(UpsertParticipant());
        yield return StartCoroutine(InsertSession(success, completionSeconds));

        if (transformations.Count > 0)
            yield return StartCoroutine(InsertTransformations());
    }

    IEnumerator UpsertParticipant()
    {
        string json = $"{{\"id\":\"{ParticipantId}\",\"age\":{Age},\"gender\":\"{EscapeJson(Gender)}\"}}";

        using UnityWebRequest request = BuildRequest($"{SupabaseUrl}/participants", json);
        request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DataCollection] Failed to save participant: {request.error}\n{request.downloadHandler.text}");
    }

    IEnumerator InsertSession(bool success, float completionSeconds)
    {
        string timestamp = sessionStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string cs = completionSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string json = $"{{" +
            $"\"id\":\"{sessionId}\"," +
            $"\"participant_id\":\"{ParticipantId}\"," +
            $"\"level_index\":{currentLevelIndex}," +
            $"\"session_start\":\"{timestamp}\"," +
            $"\"completion_time_seconds\":{cs}," +
            $"\"moves_count\":{transformations.Count}," +
            $"\"hints_used\":{hintsUsed}," +
            $"\"success\":{(success ? "true" : "false")}" +
            $"}}";

        using UnityWebRequest request = BuildRequest($"{SupabaseUrl}/sessions", json);
        request.SetRequestHeader("Prefer", "return=minimal");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DataCollection] Failed to save session: {request.error}\n{request.downloadHandler.text}");
        else
            Debug.Log($"[DataCollection] Level {currentLevelIndex + 1} session saved ({transformations.Count} transformations pending).");
    }

    IEnumerator InsertTransformations()
    {
        StringBuilder sb = new("[");
        for (int i = 0; i < transformations.Count; i++)
        {
            TransformationData t = transformations[i];
            sb.Append("{");
            sb.Append($"\"session_id\":\"{sessionId}\",");
            sb.Append($"\"order_index\":{i},");
            sb.Append($"\"type\":\"{t.type}\",");
            sb.Append($"\"block_index\":{t.blockIndex},");
            sb.Append($"\"block_type\":\"{EscapeJson(t.blockType)}\",");
            sb.Append($"\"seconds_elapsed\":{t.secondsElapsed.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

            if (t.type == "rotate")
            {
                sb.Append($",\"rotation_before\":{t.rotationBefore}");
                sb.Append($",\"rotation_after\":{t.rotationAfter}");
            }
            else if (t.type == "flip")
            {
                sb.Append($",\"flipped_before\":{(t.flippedBefore ? "true" : "false")}");
                sb.Append($",\"flipped_after\":{(t.flippedAfter ? "true" : "false")}");
            }
            else if (t.type == "move")
            {
                if (t.tileXBefore.HasValue)
                {
                    sb.Append($",\"tile_x_before\":{t.tileXBefore}");
                    sb.Append($",\"tile_y_before\":{t.tileYBefore}");
                }
                sb.Append($",\"tile_x_after\":{t.tileXAfter}");
                sb.Append($",\"tile_y_after\":{t.tileYAfter}");
            }

            sb.Append(i < transformations.Count - 1 ? "}," : "}");
        }
        sb.Append("]");

        using UnityWebRequest request = BuildRequest($"{SupabaseUrl}/transformations", sb.ToString());
        request.SetRequestHeader("Prefer", "return=minimal");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DataCollection] Failed to save transformations: {request.error}\n{request.downloadHandler.text}");
        else
            Debug.Log($"[DataCollection] {transformations.Count} transformations saved.");
    }

    UnityWebRequest BuildRequest(string url, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", supabaseAnonKey);
        request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
        return request;
    }

    static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
