using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death : MonoBehaviour
{
   

    public bool isDead;
    private Transform charPos;

    public Transform spawnP;

  
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

    }
}
