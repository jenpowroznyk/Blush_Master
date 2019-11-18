using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimController : MonoBehaviour
{
    private Animator anim;

    private float w;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        w = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Space))
        {
            w = 1;
        }
        else
        {
            w = 0;
        }

        anim.SetFloat("Walk", w);
    }
}
