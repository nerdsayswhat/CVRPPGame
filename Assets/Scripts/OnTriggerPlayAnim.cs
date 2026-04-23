using UnityEngine;

public class OnTriggerPlayAnim : MonoBehaviour
{

    public string KeyTag;
    public string AnimBool;
    public Animator animlock;
    public Animator animdoor;


    void OnTriggerEnter(Collider other)
    {

        Debug.Log("key check");
        if(other.gameObject.tag == KeyTag)

        {
            Debug.Log("Found Key");
            animlock.SetBool(AnimBool, true);
            animdoor.SetBool(AnimBool, true);
        }
    }
}
