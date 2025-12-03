using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.IO;

public class PenDrawing : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    public Transform drawingPoint;

    [Header("Settings")]
    public float pointDistance = 0.01f;
    public float lineWidth = 0.01f;

    [Header("Cube Settings")]
    public float cubeLifetime = 5f;

    [Header("Gesture Recording")]
    public bool recordGesture = false;
    public string gestureName = "";
    public string gesturesFolder = "Assets/Gestures";

    private DollarRecognizer recognizer;
    private List<Vector2> gesturePoints = new List<Vector2>();

    private bool isHeld = false;
    private bool isDrawing = false;

    private LineRenderer line;
    private List<Vector3> points = new List<Vector3>();
    private GameObject lastCube;

    void Awake()
    {
        recognizer = new DollarRecognizer();
        LoadGestureTemplates();
    }

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        grabInteractable.activated.AddListener(OnStartDrawing);
        grabInteractable.deactivated.AddListener(OnStopDrawing);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);

        grabInteractable.activated.RemoveListener(OnStartDrawing);
        grabInteractable.deactivated.RemoveListener(OnStopDrawing);
    }

    void Update()
    {
        if (!isHeld || !isDrawing || line == null)
            return;

        Vector3 pos = drawingPoint.position;

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], pos) > pointDistance)
        {
            points.Add(pos);
            line.positionCount = points.Count;
            line.SetPosition(points.Count - 1, pos);

            Vector2 screenPos = Camera.main.WorldToScreenPoint(pos);
            gesturePoints.Add(screenPos);
        }
    }

    // ---------------- GRAB HANDLERS ----------------

    private void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        StopDrawingInternal();
    }

    // ---------------- DRAWING TOGGLE ----------------

    private void OnStartDrawing(ActivateEventArgs args)
    {
        if (!isHeld) return;
        BeginDrawing();
    }

    private void OnStopDrawing(DeactivateEventArgs args)
    {
        StopDrawingInternal();
    }

    // ---------------- START DRAWING ----------------

    private void BeginDrawing()
    {
        if (isDrawing) return;

        isDrawing = true;
        points.Clear();
        gesturePoints.Clear();

        GameObject lineObj = new GameObject("DrawnLine");
        line = lineObj.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.widthMultiplier = lineWidth;
        line.numCornerVertices = 8;
        line.numCapVertices = 8;

        var shader = Shader.Find("Sprites/Default");
        line.material = new Material(shader);
        line.startColor = Color.white;
        line.endColor = Color.white;

        if (recordGesture)
            Debug.Log("📘 Recording started for: " + gestureName);
    }

    // ---------------- STOP DRAWING ----------------

    private void StopDrawingInternal()
    {
        if (!isDrawing) return;
        isDrawing = false;

        if (recordGesture)
        {
            SaveGestureToJSON();
            Debug.Log("📗 Recording stopped.");
        }
        else
        {
            RecognizeAndSpawnObject();
        }

        if (line != null)
            Destroy(line.gameObject);

        points.Clear();
        gesturePoints.Clear();
    }

    // ---------------- SAVE GESTURE ----------------

    private void SaveGestureToJSON()
    {
        if (gesturePoints.Count < 5)
        {
            Debug.Log("❌ Gesture too small — NOT saved.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gestureName))
        {
            Debug.Log("❌ gestureName is empty — NOT saved.");
            return;
        }

        if (!Directory.Exists(gesturesFolder))
            Directory.CreateDirectory(gesturesFolder);

        string filePath = Path.Combine(
            gesturesFolder,
            $"{gestureName}_{System.DateTime.Now.Ticks}.json"
        );

        string json = JsonUtility.ToJson(new GestureJSON(gestureName, gesturePoints.ToArray()), true);
        File.WriteAllText(filePath, json);

        Debug.Log("💾 Saved gesture: " + filePath);
    }

    // ---------------- RECOGNIZE + SPAWN ----------------

    private void RecognizeAndSpawnObject()
    {
        if (gesturePoints.Count < 5)
        {
            Debug.Log("❌ Not enough points to recognize.");
            return;
        }

        // ---------- LINE DETECTOR ----------
        float totalLength = 0f;
        for (int i = 1; i < gesturePoints.Count; i++)
            totalLength += Vector2.Distance(gesturePoints[i], gesturePoints[i - 1]);

        Vector2 overall = gesturePoints[gesturePoints.Count - 1] - gesturePoints[0];
        float straightness = overall.magnitude / totalLength;

        if (straightness > 0.95f)
        {
            Debug.Log("📏 STRAIGHT LINE detected → SPAWN BOARD");
            SpawnCubeWithLifetime(); // now spawns Board instead
            return;
        }
        // ------------------------------------

        var result = recognizer.Recognize(gesturePoints);

        if (result.Match == null)
        {
            Debug.Log("❌ No gesture matched.");
            return;
        }

        float score = result.Score;
        string bestName = result.Match.Name.ToLower();

        Debug.Log($"🔍 Gesture: {bestName} (score={score})");

        if (score < 0.80f)
        {
            Debug.Log("❌ Match too weak — no spawn.");
            return;
        }

        if (bestName == "spherecircle")
        {
            Debug.Log("⚪ SphereCircle matched → SPAWN SPHERE");
            SpawnSphere();
        }
        else
        {
            Debug.Log("❌ Unknown gesture name: " + bestName);
        }
    }

    // ---------------- SPAWNERS ----------------

    public void SpawnCubeWithLifetime()
    {
        if (points.Count == 0) return;

        Vector3 avg = Vector3.zero;
        foreach (var p in points) avg += p;
        avg /= points.Count;

        if (lastCube != null)
            Destroy(lastCube, cubeLifetime);

        // -----------------------------
        // ✔ REPLACED CUBE WITH BOARD
        // -----------------------------
        GameObject boardPrefab = Resources.Load<GameObject>("Board");

        if (boardPrefab == null)
        {
            Debug.LogError("❌ Board.prefab NOT FOUND in Resources folder!");
            return;
        }

        GameObject board = Instantiate(boardPrefab, avg, Quaternion.identity);

        lastCube = board;
    }

    public void SpawnSphere()
    {
        if (points.Count == 0) return;

        Vector3 avg = Vector3.zero;
        foreach (var p in points) avg += p;
        avg /= points.Count;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = avg;
        sphere.transform.localScale = Vector3.one * 0.25f;
    }

    // ---------------- LOAD GESTURES ----------------

    private void LoadGestureTemplates()
    {
        if (!Directory.Exists(gesturesFolder))
        {
            Debug.Log("⚠ No gestures folder.");
            return;
        }

        string[] files = Directory.GetFiles(gesturesFolder, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file).Trim();

            if (string.IsNullOrEmpty(json) || !json.Contains("{"))
            {
                Debug.LogWarning("❌ Invalid JSON deleted: " + file);
                File.Delete(file);
                continue;
            }

            GestureJSON g;
            try
            {
                g = JsonUtility.FromJson<GestureJSON>(json);
            }
            catch
            {
                Debug.LogWarning("❌ Parse failure — deleted: " + file);
                File.Delete(file);
                continue;
            }

            if (g == null || string.IsNullOrWhiteSpace(g.name) || g.points == null || g.points.Length < 5)
            {
                Debug.LogWarning("❌ Invalid gesture deleted: " + file);
                File.Delete(file);
                continue;
            }

            recognizer.SavePattern(g.name, g.ToVector2List());
        }

        Debug.Log($"📚 Loaded gesture templates: {files.Length}");
    }

    // ---------------- JSON CLASS ----------------

    [System.Serializable]
    public class GestureJSON
    {
        public string name;
        public Vector2[] points;

        public GestureJSON(string name, Vector2[] points)
        {
            this.name = name;
            this.points = points;
        }

        public List<Vector2> ToVector2List()
        {
            return new List<Vector2>(points);
        }
    }
}
