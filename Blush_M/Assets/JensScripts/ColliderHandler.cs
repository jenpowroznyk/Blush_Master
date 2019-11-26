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
        Destroy(this.gameObject);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
