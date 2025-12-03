using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

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

    // ---------------------------
    // $1 GESTURE RECOGNITION
    // ---------------------------
    private DollarRecognizer recognizer;
    private List<Vector2> gesturePoints = new List<Vector2>();
    public string lastRecognizedGesture = "";

    [Header("Gesture Recording")]
    public bool recordGesture = false;
    public string recordGestureName = "";

    // ---------------------------

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
        if (!isHeld || !isDrawing || line == null) return;

        Vector3 pos = drawingPoint.position;

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], pos) > pointDistance)
        {
            points.Add(pos);
            line.positionCount = points.Count;
            line.SetPosition(points.Count - 1, pos);

            // Add 3D → 2D point for recognizer
            Vector2 screenPos = Camera.main.WorldToScreenPoint(pos);
            gesturePoints.Add(screenPos);
        }
    }

    // ----- GRAB -----
    private void OnGrab(SelectEnterEventArgs args) => isHeld = true;

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        StopDrawingInternal();
    }

    // ----- ACTIVATE / DEACTIVATE -----
    private void OnStartDrawing(ActivateEventArgs args)
    {
        if (!isHeld) return;
        BeginDrawing();
    }

    private void OnStopDrawing(DeactivateEventArgs args)
    {
        StopDrawingInternal();
    }

    // ----- Drawing Begin -----
    private void BeginDrawing()
    {
        if (isDrawing) return;

        isDrawing = true;
        points.Clear();
        gesturePoints.Clear();

        // Recording mode log
        if (recordGesture)
            Debug.Log("🎤 Gesture Recording STARTED…");

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
    }

    // ----- Drawing Stop -----
    private void StopDrawingInternal()
    {
        if (!isDrawing) return;

        isDrawing = false;

        // ------ RECORDING MODE ------
        if (recordGesture)
        {
            Debug.Log("🎤 Gesture Recording STOPPED");

            if (gesturePoints.Count < 5)
            {
                Debug.LogError("❌ Recording FAILED — too few points.");
            }
            else if (string.IsNullOrWhiteSpace(recordGestureName))
            {
                Debug.LogError("❌ Recording FAILED — gesture name empty.");
            }
            else
            {
                recognizer.SavePattern(recordGestureName, gesturePoints);
                Debug.Log("✅ Gesture Recorded Successfully: " + recordGestureName);
            }

            recordGesture = false;
            recordGestureName = "";
        }
        else
        {
            // ------ NORMAL GESTURE MODE ------
            if (gesturePoints.Count > 2)
            {
                var result = recognizer.Recognize(gesturePoints);

                if (result.Match != null)
                {
                    lastRecognizedGesture = result.Match.Name;
                    Debug.Log("GESTURE RECOGNIZED: " + lastRecognizedGesture);
                }
                else
                {
                    Debug.Log("No gesture recognized.");
                }
            }
        }

        // Spawn objects based on gesture
        if (points.Count > 0)
        {
            List<Vector2> flat = MakeFlat(points);
            var shape = recognizer.Recognize(flat);

            if (shape.Match != null)
            {
                if (shape.Match.Name == "Line")
                {
                    SpawnSphere();
                }
                else if (shape.Match.Name == "Square")
                {
                    SpawnCubeWithLifetime();
                }
            }
        }

        if (line != null)
            Destroy(line.gameObject);

        points.Clear();
        gesturePoints.Clear();
    }

    // ----- FLATTEN TO 2D -----
    public List<Vector2> MakeFlat(List<Vector3> points)
    {
        if (points == null || points.Count == 0)
            return new List<Vector2>();

        Vector3 mean = Vector3.zero;
        foreach (var p in points) mean += p;
        mean /= points.Count;

        // covariance matrix
        float xx = 0, xy = 0, xz = 0;
        float yy = 0, yz = 0, zz = 0;

        foreach (var p in points)
        {
            Vector3 r = p - mean;
            xx += r.x * r.x;
            xy += r.x * r.y;
            xz += r.x * r.z;
            yy += r.y * r.y;
            yz += r.y * r.z;
            zz += r.z * r.z;
        }

        float[,] cov = new float[3, 3] {
            { xx, xy, xz },
            { xy, yy, yz },
            { xz, yz, zz }
        };

        Vector3 eigen1 = PowerIteration(cov);
        Vector3 eigen2 = PowerIteration(RemoveComponent(cov, eigen1));

        Vector3 axisX = eigen1.normalized;
        Vector3 axisY = eigen2.normalized;

        List<Vector2> result = new List<Vector2>(points.Count);

        foreach (var p in points)
        {
            Vector3 d = p - mean;
            float u = Vector3.Dot(d, axisX);
            float v = Vector3.Dot(d, axisY);
            result.Add(new Vector2(u, v));
        }

        return result;
    }

    private Vector3 PowerIteration(float[,] m)
    {
        Vector3 v = new Vector3(1, 1, 1).normalized;

        for (int i = 0; i < 10; i++)
        {
            Vector3 mv = new Vector3(
                m[0, 0] * v.x + m[0, 1] * v.y + m[0, 2] * v.z,
                m[1, 0] * v.x + m[1, 1] * v.y + m[1, 2] * v.z,
                m[2, 0] * v.x + m[2, 1] * v.y + m[2, 2] * v.z
            );
            v = mv.normalized;
        }
        return v;
    }

    private float[,] RemoveComponent(float[,] m, Vector3 axis)
    {
        float[,] r = new float[3, 3];
        float ax = axis.x, ay = axis.y, az = axis.z;

        float[,] P = {
            { ax * ax, ax * ay, ax * az },
            { ay * ax, ay * ay, ay * az },
            { az * ax, az * ay, az * az }
        };

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                r[i, j] = m[i, j] - P[i, j];

        return r;
    }

    // ----- SPAWN OBJECTS -----
    public void SpawnCubeWithLifetime()
    {
        if (points.Count == 0) return;

        Vector3 avg = Vector3.zero;
        foreach (var p in points) avg += p;
        avg /= points.Count;

        if (lastCube != null)
            Destroy(lastCube, cubeLifetime);

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = avg;
        cube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        lastCube = cube;
    }

    public void SpawnSphere()
    {
        if (points.Count == 0) return;

        Vector3 avg = Vector3.zero;
        foreach (var p in points) avg += p;
        avg /= points.Count;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = avg;
        sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
    }

    // ----- DEFAULT TEMPLATES -----
    private void LoadGestureTemplates()
    {
        List<Vector2> lineGesture = new List<Vector2>()
        {
            new Vector2(0,0),
            new Vector2(100,0),
            new Vector2(200,0),
        };
        recognizer.SavePattern("Line", lineGesture);

        List<Vector2> squareGesture = new List<Vector2>()
        {
            new Vector2(0,0),
            new Vector2(100,0),
            new Vector2(100,100),
            new Vector2(0,100),
            new Vector2(0,0),
        };
        recognizer.SavePattern("Square", squareGesture);
    }
}
