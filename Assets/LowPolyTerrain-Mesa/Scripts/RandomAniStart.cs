using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAniStart : MonoBehaviour {

    float frameLangth;
    Animator anim;
    public string clipString;
    

    void Start () {
        anim = transform.GetComponent<Animator>();
        
        RuntimeAnimatorController ac = anim.runtimeAnimatorController;    //Get Animator controller
        for (int i = 0; i < ac.animationClips.Length; i++)                 //For all animations
        {
            if (clipString != null && ac.animationClips[i].name == clipString)        //If it has the same name as your clip
            {
                frameLangth = ac.animationClips[i].length;
                float randomFrame = Random.Range(0, frameLangth);
                anim.Play(clipString, 0, (1f / frameLangth) * randomFrame);
            }
            else
            {
                string objName = transform.gameObject.name;
                print("There is no clip name or no matching clips : " + objName);
            }
        }
    }
}
