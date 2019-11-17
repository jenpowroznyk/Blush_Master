using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blending : MonoBehaviour
{

    private Animator anim;
    private float h;
    private float v;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        anim.SetFloat("Walk", h);
       // anim.SetFloat("Turn", v);
    }
}
