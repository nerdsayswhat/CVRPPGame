using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class PenDrawing : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;   // <-- IMPORTANT: correct XRGrabInteractable
    public Transform drawingPoint;

    [Header("Settings")]
    public float pointDistance = 0.01f;
    public float lineWidth = 0.01f;

    [Header("Cube Settings")]
    public float cubeLifetime = 5f; // set this in Inspector

    private bool isHeld = false;
    private bool isDrawing = false;

    private LineRenderer line;
    private List<Vector3> points = new List<Vector3>();
    private GameObject lastCube;   // keep track so only one cube reference

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
        }
    }

    // ----- GRAB -----

    private void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

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

    // ----- Drawing -----

    private void BeginDrawing()
    {
        if (isDrawing) return;

        isDrawing = true;
        points.Clear();

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

    private void StopDrawingInternal()
    {
        if (!isDrawing) return;

        isDrawing = false;

        // spawn cube at line average position
        if (points.Count > 0)
        {
            SpawnCubeWithLifetime();
        }

        // delete drawn line
        if (line != null)
            Destroy(line.gameObject);

        points.Clear();
    }

    // ----- UNITY EVENT WRAPPERS FOR INSPECTOR -----
    public void StartDrawingEvent() => OnStartDrawing(null);
    public void StopDrawingEvent() => OnStopDrawing(null);

    // ----- SPAWN CUBE WITH LIFETIME -----
    public void SpawnCubeWithLifetime()
    {
        if (points.Count == 0) return;

        Vector3 avg = Vector3.zero;
        foreach (var p in points) avg += p;
        avg /= points.Count;

        // If there is an old cube, destroy it after cubeLifetime
        if (lastCube != null)
        {
            Destroy(lastCube, cubeLifetime);
        }

        // Spawn the new cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = avg;
        cube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        // The new cube stays indefinitely
        lastCube = cube;
    }

} // <-- final closing brace for the PenDrawing class
