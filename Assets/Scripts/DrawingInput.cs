using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingInput : MonoBehaviour
{
    public InputActionReference startDrawing;
    public InputActionReference stopDrawing;

    public System.Action OnStartDrawing;
    public System.Action OnStopDrawing;

    private void OnEnable()
    {
        if (startDrawing != null) startDrawing.action.performed += StartDraw;
        if (stopDrawing != null) stopDrawing.action.performed += StopDraw;

        startDrawing?.action.Enable();
        stopDrawing?.action.Enable();
    }

    private void OnDisable()
    {
        if (startDrawing != null) startDrawing.action.performed -= StartDraw;
        if (stopDrawing != null) stopDrawing.action.performed -= StopDraw;
    }

    void StartDraw(InputAction.CallbackContext ctx)
    {
        OnStartDrawing?.Invoke();
    }

    void StopDraw(InputAction.CallbackContext ctx)
    {
        OnStopDrawing?.Invoke();
    }
}