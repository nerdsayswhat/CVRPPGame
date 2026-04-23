using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class SwitchCharacter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    CharacterType currentCharacter;
    public GameObject [] humanHands;
    public GameObject []antHands;
    public GameObject [] birdWings;

    public GravityProvider gravityProvider;//control the Gravity of Game Object

    public LocomotionManager locomotionManager;// use to enable fly mode

    public DynamicMoveProvider moveProvider;
    public float humanMoveSpeed;
    public float antMoveSpeed;
    public float birdFlySpeed;
    // change the speed in three modes

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
    int toggleStep = 0;
    public void ToggleToNext()
{
    toggleStep++;

    if (toggleStep > 3) toggleStep = 0;

    switch (toggleStep)
    {
        case 0:
            Switch(CharacterType.Human);
            break;
        case 1:
            Switch(CharacterType.Ant);
            break;
        case 2:
            Switch(CharacterType.Human);
            break;
        case 3:
            Switch(CharacterType.Bird);
            break;
    }
}

    public void Switch(CharacterType newType)
    {

        if(currentCharacter == newType) return;

        if(newType == CharacterType.Human) TransformIntoHuman();
        if(newType == CharacterType.Ant) TranssformIntoAnt();
        if(newType == CharacterType.Bird) TransformIntoBird();

        if (gravityProvider != null)
        gravityProvider.useGravity = (newType != CharacterType.Bird);
        //control the Gravity of Game Object
        
        if (locomotionManager != null)
        locomotionManager.enableFly = (newType == CharacterType.Bird);
        // use to enable fly mode

        if(newType == CharacterType.Bird)
        {
            moveProvider.moveSpeed = birdFlySpeed;
        }
        else if(newType == CharacterType.Ant)
        {
            moveProvider.moveSpeed = antMoveSpeed;
        }
        else
        {
            moveProvider.moveSpeed = humanMoveSpeed;
        }

      
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
