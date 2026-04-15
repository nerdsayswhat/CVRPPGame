using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.IO;

public class PenDrawing : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    public Transform drawingPoint;

    [Header("Drawing Settings")]
    public float pointDistance = 0.01f;
    public float lineWidth = 0.01f;

    [Header("Spawn Settings")]
    public float cubeLifetime = 5f;

    [Header("Spawn Materials")]
public Material boardMaterial;
public Material sphereMaterial;

    [Header("Outline Materials")]
public Material boardOutlineMaterial;
public Material sphereOutlineMaterial;

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
        if (grabInteractable == null) return;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        grabInteractable.activated.AddListener(OnStartDrawing);
        grabInteractable.deactivated.AddListener(OnStopDrawing);
    }

    void OnDisable()
    {
        if (grabInteractable == null) return;

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

    // ---------------- GRAB ----------------

    private void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        StopDrawingInternal();
    }

    // ---------------- DRAW INPUT ----------------

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

        Shader shader = Shader.Find("Sprites/Default");

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
        if (gesturePoints.Count < 5) return;

        if (string.IsNullOrWhiteSpace(gestureName)) return;

        if (!Directory.Exists(gesturesFolder))
            Directory.CreateDirectory(gesturesFolder);

        string filePath =
            Path.Combine(
                gesturesFolder,
                $"{gestureName}_{System.DateTime.Now.Ticks}.json"
            );

        string json =
            JsonUtility.ToJson(
                new GestureJSON(gestureName, gesturePoints.ToArray()),
                true
            );

        File.WriteAllText(filePath, json);

        Debug.Log("💾 Saved Gesture: " + filePath);
    }

    // ---------------- RECOGNITION ----------------

    private void RecognizeAndSpawnObject()
    {
        if (gesturePoints.Count < 5) return;

        float totalLength = 0f;

        for (int i = 1; i < gesturePoints.Count; i++)
        {
            totalLength +=
                Vector2.Distance(
                    gesturePoints[i],
                    gesturePoints[i - 1]
                );
        }

        Vector2 overall =
            gesturePoints[gesturePoints.Count - 1] -
            gesturePoints[0];

        float straightness =
            overall.magnitude / totalLength;

        float endpointDistance =
            Vector2.Distance(
                gesturePoints[0],
                gesturePoints[gesturePoints.Count - 1]
            );

        bool looksLine =
            straightness > 0.75f &&
            endpointDistance > Screen.width * 0.05f;

        if (looksLine)
        {
            SpawnCubeWithLifetime();
            return;
        }

        var result =
            recognizer.Recognize(gesturePoints);

        if (result.Match == null) return;

        float score = result.Score;

        string bestName =
            result.Match.Name.ToLower();

        if (score < 0.80f) return;

        if (bestName == "spherecircle")
        {
            SpawnSphere();
        }
    }

    // ---------------- SPAWN BOARD ----------------

    public void SpawnCubeWithLifetime()
{
    if (points.Count == 0) return;

    Vector3 avg = Vector3.zero;

    foreach (var p in points)
        avg += p;

    avg /= points.Count;

    if (lastCube != null)
        Destroy(lastCube, cubeLifetime);

    GameObject boardPrefab =
        Resources.Load<GameObject>("Board");

    if (boardPrefab == null)
    {
        Debug.LogError("Board prefab NOT FOUND.");
        return;
    }

    GameObject board =
        Instantiate(boardPrefab, avg, Quaternion.identity);

    Renderer renderer =
        board.GetComponentInChildren<Renderer>();

    if (renderer != null)
    {
        renderer.materials = new Material[]
        {
            boardMaterial,
            boardOutlineMaterial
        };
    }

    lastCube = board;
}

    // ---------------- SPAWN SPHERE ----------------

   public void SpawnSphere()
{
    if (points.Count == 0) return;

    Vector3 avg = Vector3.zero;

    foreach (var p in points)
        avg += p;

    avg /= points.Count;

    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

    sphere.transform.position = avg;
    sphere.transform.localScale = Vector3.one * 0.25f;

    Renderer renderer = sphere.GetComponent<Renderer>();

    if (renderer != null)
    {
        renderer.materials = new Material[]
        {
            sphereMaterial,
            sphereOutlineMaterial
        };
    }
}

    // ---------------- LOAD GESTURES ----------------

    private void LoadGestureTemplates()
    {
        if (!Directory.Exists(gesturesFolder))
            return;

        string[] files =
            Directory.GetFiles(gesturesFolder, "*.json");

        foreach (string file in files)
        {
            string json =
                File.ReadAllText(file).Trim();

            GestureJSON g =
                JsonUtility.FromJson<GestureJSON>(json);

            if (g == null) continue;

            recognizer.SavePattern(
                g.name,
                g.ToVector2List()
            );
        }
    }

    // ---------------- JSON CLASS ----------------

    [System.Serializable]
    public class GestureJSON
    {
        public string name;
        public Vector2[] points;

        public GestureJSON(
            string name,
            Vector2[] points
        )
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
