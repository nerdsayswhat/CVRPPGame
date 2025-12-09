using System;
using UnityEngine;

public class WorldScaleController : MonoBehaviour
{

 
    public Transform world;
    public Transform scaler;

    public float antScalefactor;
    public float birdScalefactor;

    SwitchCharacter.CharacterType currentScaledType;

    void Awake()
    {
        currentScaledType = SwitchCharacter.CharacterType.Human;
        scaler.localScale = Vector3.one;
    }
    public void SetWorldScale(SwitchCharacter.CharacterType newType)
    {
        
        if(newType == SwitchCharacter.CharacterType.Human)
        {
            ScaleDown();
            return;
        }
        if(currentScaledType == newType)return ;
        if(newType == SwitchCharacter.CharacterType.Ant) ScaleTo(antScalefactor);
        else if(newType == SwitchCharacter.CharacterType.Bird) ScaleTo(birdScalefactor);
        
        currentScaledType = newType;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void ScaleTo(float factor)
    {
        
        world.parent = scaler;
        scaler.localScale = factor * Vector3.one;
        world.parent = null;
       

    }
        void ScaleDown()
    {
        
        world.parent = scaler;
        scaler.localScale = Vector3.one;
        world.parent = null;
       currentScaledType = SwitchCharacter.CharacterType.Human;

    }
}
