using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HipFollower : MonoBehaviour
{
    [Header("References")]
    public Transform xrCamera;       // Assign your XR Camera (Main Camera under XR Origin)
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    [Header("Offsets (relative to player's head)")]
    public float hipHeightRatio = 0.55f;   // Percentage down from head to hip
    public float forwardOffset = 0.15f;    // Forward from body
    public float sideOffset = 0.10f;       // To the side (positive = right hip, negative = left)
    public float smoothing = 12f;          // How smooth the pen moves

    private bool isHeld = false;

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    void Update()
    {
        if (isHeld) return; // Don't follow hip while in hand

        FollowHip();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void FollowHip()
    {
        if (xrCamera == null) return;

        // 1?? HEAD POSITION & ROTATION
        Vector3 headPos = xrCamera.position;

        // 2?? HIP HEIGHT (works seated or standing for ALL heights)
        float headHeight = xrCamera.localPosition.y;
        float hipHeight = headHeight * hipHeightRatio;

        Vector3 hipPos = new Vector3(
            headPos.x,
            xrCamera.position.y - (xrCamera.localPosition.y - hipHeight),
            headPos.z
        );

        // 3?? ROTATION: Take head forward & remove vertical tilt
        Vector3 flatForward = xrCamera.forward;
        flatForward.y = 0;
        flatForward.Normalize();

        // 4?? OFFSET FINAL POSITION
        Vector3 finalPos = hipPos
            + flatForward * forwardOffset
            + xrCamera.right * sideOffset;

        // 5?? SMOOTH MOVE THE PEN
        transform.position = Vector3.Lerp(transform.position, finalPos, smoothing * Time.deltaTime);

        // 6?? ROTATE TO FACE FORWARD LIKE A HOLSTERED TOOL
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(flatForward),
            smoothing * Time.deltaTime
        );
    }
}
