using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death : MonoBehaviour
{
   

    public bool isDead;
    private Transform charPos;

    public Transform spawnP;
    public Transform spawnP2;

   // public GameObject ant;


    // Start is called before the first frame update
    void Start()
    {
        isDead = false;
        charPos = this.GetComponent<Transform>();
        //charPos = this.GetComponent<Transform>();
     
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.tag == "Water")
        {

            isDead = true;
            charPos.position = spawnP.position;
            
        }

        if (collision.gameObject.tag == "Dead")
        {

            isDead = true;
            charPos.position = spawnP2.position;
           

            //Instantiate(ant, new Vector3(0, 0, 0), Quaternion.identity);

        }

    }
}
