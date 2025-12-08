using System;
using UnityEngine;

public class WorldScaleController : MonoBehaviour
{

    bool scaledUp = false;
    public Transform world;
    public Transform scaler;

    public float factor;
    SwitchCharacter.CharacterType prevType;
    public void SetWorldScale(SwitchCharacter.CharacterType newType)
    {
        Debug.Log("Scale To: " + newType.ToString());

        if(newType == SwitchCharacter.CharacterType.Ant) ScaleUp();
        if(newType != SwitchCharacter.CharacterType.Ant) ScaleDown();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void ScaleDown()
    {
        Debug.Log("Scale Down");
        if(!scaledUp)return;
        world.parent = scaler;
        scaler.localScale = Vector3.one;
        world.parent = null;
        scaledUp = false;

    }

    // Update is called once per frame
    void ScaleUp()
    {
        Debug.Log("Scale Up");

        if(scaledUp)return;
        world.parent = scaler;
        scaler.localScale = factor * Vector3.one;
        world.parent = null;
        scaledUp = true;

    }
}
