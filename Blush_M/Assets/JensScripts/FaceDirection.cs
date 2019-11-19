using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceDirection : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() { 

        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

    Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
    
        if (movement != Vector3.zero)
        {
            
            
            transform.rotation = Quaternion.LookRotation(movement);

        }
    }
}
