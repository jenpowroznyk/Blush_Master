using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{


    Rigidbody rb;
   

    public int speed;
    public int originalSpeed;
    public int jumpImpulse;
    public int maxSpeed;
    private int originalJumpImpulse;

    //private Vector3 mirrorVel;
           

    public int lilyForce;


    bool canJump = true;
    

    void Start()
    {        
        rb = this.GetComponent<Rigidbody>();
        originalSpeed = speed;
        originalJumpImpulse = jumpImpulse;


    }

    // Update is called once per frame
    void Update()
    {
        int xValue = 0;
        int zValue = 0;
        bool keyDown = false;
       

        float rbMag = rb.velocity.magnitude;

        if (Input.GetKey(KeyCode.W))
        {
            zValue = 1;
            keyDown = true;

        }


        if (Input.GetKey(KeyCode.A))
        {
            xValue = -1;
            keyDown = true;

        }


        if (Input.GetKey(KeyCode.S))
        {
            zValue = -1;
            keyDown = true;

        }


        if (Input.GetKey(KeyCode.D))
        {
            xValue = 1;
            keyDown = true;


        }

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            Vector3 jumpVector = new Vector3(0, jumpImpulse, 0);
            rb.velocity += jumpVector;
            canJump = false;

        }

        // if (Input.anyKey == false)
        //{
        // rb.velocity = new Vector3(0, 0, 0);

        //}


      

    

        if (!Input.anyKey)
        {
            
            xValue = 0;
            zValue = 0;


             

        }




        Vector3 velocity = new Vector3(xValue * speed * Time.deltaTime, 0, zValue * speed * Time.deltaTime);
         
            rb.velocity += velocity;

        if (Input.GetKeyDown("escape")) Application.Quit();

    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            Debug.Log("grounded");
            canJump = true;
        }

        if (collision.gameObject.tag == "Lilly")
        {
            jumpImpulse = jumpImpulse + lilyForce / 4;
            canJump = true;   
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Lilly")
        {
            jumpImpulse = originalJumpImpulse;
            canJump = true;
        }
    }
}
