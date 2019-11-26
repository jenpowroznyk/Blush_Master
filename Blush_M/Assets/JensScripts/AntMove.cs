using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntMove : MonoBehaviour
{

    public static int movespeed = 1;
    public Vector3 userDirection = Vector3.right;

    void Start()
    {
    }

    public void Update()
    {
        transform.Translate(userDirection * movespeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {

        Debug.Log("Hit and object named " + collision.gameObject.name);

        if (collision.gameObject.tag == "AntTrigRight")
        {
            userDirection = new Vector3(0, 0, 5);

           
        }

        if (collision.gameObject.tag == "AntTrigLeft")
        {
            userDirection = new Vector3(0, 0, -5);

           
        }

    }
}
