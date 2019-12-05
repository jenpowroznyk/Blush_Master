using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderHandler : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject antCol;
    public GameObject antObj;


    void Start()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        antCol.SetActive(true);
        antObj.SetActive(true);
        // Destroy(this.gameObject);

        Instantiate(antObj, new Vector3(63, -14, 38), Quaternion.Euler(0, 90, 0));
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
