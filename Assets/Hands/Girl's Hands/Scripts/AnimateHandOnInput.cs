using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class AnimateHandOnInput : MonoBehaviour
{
    public InputActionProperty triggerAnimationAction;
    public InputActionProperty gripAnimationAction;
    public InputActionProperty thumbStickAnimationAction;
    public Animator handAnimator;

    // Update is called once per frame
    void Update()
    {
        float triggerValue = triggerAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Trigger", triggerValue);

        float gripValue = gripAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Grip", gripValue);

        float thumbStickValue = gripAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("thumStick", thumbStickValue);
    }
}
