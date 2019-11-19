using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{


    Rigidbody rb;
   

    public int speed;
    public int originalSpeed;
    public int jumpImpulse;


    bool canJump = true;
    

    void Start()
    {        
        rb = this.GetComponent<Rigidbody>();
        originalSpeed = speed;
       
    }

    // Update is called once per frame
    void Update()
    {
        int xValue = 0;
        int zValue = 0;
        bool keyDown = false;
        

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


        if (keyDown)
        {
            //Vector3 velocity = new Vector3(xValue * speed * Time.deltaTime, 0, zValue * speed * Time.deltaTime);

            //rb.AddForce(velocity, ForceMode.VelocityChange);


            //Vector3 velocity = new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0, Input.GetAxis("Vertical") * speed * Time.deltaTime);
            //rb.velocity += velocity;

        }



        Vector3 velocity = new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0, Input.GetAxis("Vertical") * speed * Time.deltaTime);
        rb.velocity += velocity;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            canJump = true;
        }
    }
}
