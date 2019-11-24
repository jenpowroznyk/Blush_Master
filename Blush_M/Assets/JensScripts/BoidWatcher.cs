using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidWatcher : MonoBehaviour
{
	public Rigidbody rigidBody;
	public Transform watcherTransform;


	public float force;


	void Start()
	{

	}

	void Update()
	{

		if (Input.GetKey(KeyCode.A))
		{

			rigidBody.AddForce(new Vector3(-1, 0, 0) * force, ForceMode.Acceleration);

		}
		else if (Input.GetKey(KeyCode.S))
		{
			rigidBody.AddForce(new Vector3(0, 0, -1) * force, ForceMode.Acceleration);


		}
		else if (Input.GetKey(KeyCode.W))
		{
			rigidBody.AddForce(new Vector3(0, 0, 1) * force, ForceMode.Acceleration);


		}
		else if (Input.GetKey(KeyCode.D))
		{
			rigidBody.AddForce(new Vector3(1, 0, 0) * force, ForceMode.Acceleration);

		}


	}

}
