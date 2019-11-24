using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntMove : MonoBehaviour
{

	public GameObject ant;
	private Transform antBod;
	
    // Start is called before the first frame update
    void Start()
    {
		antBod = ant.GetComponent<Transform>();

    }

    // Update is called once per frame
    void Update()
    {
		
    }
}
