using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceAnimation : MonoBehaviour
{
    
    public Animator animator;
    
    // Start is called before the first frame update
    void Start()
    {

    }
   

    // Update is called once per frame
     public void OnInteractableSelected()
     {
        animator.SetBool("EyesOpen", true);
    }
    public void Update()
    {

    }
}
