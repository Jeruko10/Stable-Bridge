using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DataCollectionManager : MonoBehaviour
{
    [SerializeField] string firebaseProjectId;
    [SerializeField] string firebaseApiKey;

    public static DataCollectionManager Instance { get; private set; }

    public string ParticipantId { get; private set; }
    public int Age { get; private set; }
    public string Gender { get; private set; }
    public bool IsParticipantSet { get; private set; }

    DateTime sessionStartTime;
    int movesCount;
    int hintsUsed;
    int currentLevelIndex;
    bool sessionActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LevelManager.LevelLoaded += OnLevelLoaded;
    }

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
        sessionStartTime = DateTime.UtcNow;
        movesCount = 0;
        hintsUsed = 0;
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

    public void RecordMove() { if (sessionActive) movesCount++; }
    public void RecordHint() { if (sessionActive) hintsUsed++; }

    IEnumerator SubmitSession(bool success, float completionSeconds)
    {
        if (!IsParticipantSet)
        {
            Debug.LogWarning("[DataCollection] Session not saved: participant not set.");
            yield break;
        }

        if (string.IsNullOrEmpty(firebaseProjectId) || string.IsNullOrEmpty(firebaseApiKey))
        {
            Debug.LogError("[DataCollection] Firebase Project ID or API Key is not set in the Inspector.");
            yield break;
        }

        string collection = $"level_{currentLevelIndex + 1}";
        string json = BuildFirestoreDocument(success, completionSeconds);
        string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/{collection}?key={firebaseApiKey}";
        Debug.Log($"[DataCollection] Posting to: {url}");

        byte[] body = Encoding.UTF8.GetBytes(json);
        using UnityWebRequest request = new(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DataCollection] Failed to save session: {request.error}\n{request.downloadHandler.text}");
        else
            Debug.Log($"[DataCollection] Level {currentLevelIndex} session saved to collection '{collection}'.");
    }

    string BuildFirestoreDocument(bool success, float completionSeconds)
    {
        string timestamp = sessionStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        return $@"{{
  ""fields"": {{
    ""participant_id"":          {{""stringValue"":  ""{ParticipantId}""}},
    ""age"":                     {{""integerValue"": ""{Age}""}},
    ""gender"":                  {{""stringValue"":  ""{Gender}""}},
    ""level_index"":             {{""integerValue"": ""{currentLevelIndex}""}},
    ""session_start"":           {{""timestampValue"":""{timestamp}""}},
    ""completion_time_seconds"": {{""doubleValue"":  {completionSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},
    ""moves_count"":             {{""integerValue"": ""{movesCount}""}},
    ""hints_used"":              {{""integerValue"": ""{hintsUsed}""}},
    ""success"":                 {{""booleanValue"": {(success ? "true" : "false")}}}
  }}
}}";
    }
}
