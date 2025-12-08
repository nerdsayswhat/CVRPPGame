using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class SwitchCharacter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    CharacterType currentCharacter;
    public GameObject [] humanHands;
    public GameObject []antHands;
    public GameObject [] birdWings;

    WorldScaleController scaleController;

    public InputActionProperty XButton;

    bool XwasPressedThisFrame;
    float previousX;
    public enum CharacterType
    {
        Human,
        Ant,
        Bird
    }
    void Start()
    {
       TransformIntoHuman();
       scaleController = GetComponent<WorldScaleController>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = XButton.action.ReadValue<float>();
        Debug.Log(x);
        XwasPressedThisFrame = false;
        if(previousX != x && x > 0) XwasPressedThisFrame = true;
        previousX = x;

        if(XwasPressedThisFrame) ToggleToNext();

        if(Keyboard.current.leftArrowKey.wasPressedThisFrame) Switch(CharacterType.Human);
        
        if(Keyboard.current.upArrowKey.wasPressedThisFrame) Switch(CharacterType.Ant);

        if(Keyboard.current.rightArrowKey.wasPressedThisFrame) Switch(CharacterType.Bird);

    }

    public void ToggleToNext()
    {
         if(currentCharacter == CharacterType.Human) {
            Switch(CharacterType.Ant);
            return;
        }

         if(currentCharacter == CharacterType.Ant){ 
            Switch(CharacterType.Bird);
            return;
         }
         if(currentCharacter == CharacterType.Bird) {
            Switch (CharacterType.Human);
         return;
         }
    }

    public void Switch(CharacterType newType)
    {

        if(currentCharacter == newType) return;

        if(newType == CharacterType.Human) TransformIntoHuman();
        if(newType == CharacterType.Ant) TranssformIntoAnt();
        if(newType == CharacterType.Bird) TransformIntoBird();

        scaleController.SetWorldScale(newType);
        currentCharacter = newType;
        
    }


    void TransformIntoHuman()
    {
        foreach(var hand in humanHands)
        {
            hand.SetActive(true);   
        }
        foreach(var hand in birdWings)
        {
            hand.SetActive(false);   
        }
        foreach(var hand in antHands)
        {
            hand.SetActive(false);   
        }

       
    }

    void TranssformIntoAnt()
    {
        foreach(var hand in humanHands)
        {
            hand.SetActive(false);   
        }
        foreach(var hand in birdWings)
        {
            hand.SetActive(false);   
        }
        foreach(var hand in antHands)
        {
            hand.SetActive(true);   
        }
    }

    void TransformIntoBird()
    {
        foreach(var hand in humanHands)
        {
            hand.SetActive(false);   
        }
        foreach(var hand in birdWings)
        {
            hand.SetActive(true);   
        }
        foreach(var hand in antHands)
        {
            hand.SetActive(false);   
        }
    }

}
