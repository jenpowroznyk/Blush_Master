using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidBehavior : MonoBehaviour
{
	public Rigidbody rigidBody;
	public Transform boidTransform;

	private GameObject[] boidObjects;
	private List<Rigidbody> rigidbodies = new List<Rigidbody>();
	public GameObject watcher;
	public Rigidbody watcherRb;
	public float maxSpeed;

	public Vector3 averageBoidPos;
	public Vector3 totaledBoidPos;
	public Vector3 boidPos;
	public Vector3 watcherPos;
	public Vector3 difference;
	public Vector3 boidAveraging;
	public Vector3 keepDistance;



	void Start()
	{
		BoidWatcher boidWatcher = watcher.GetComponent<BoidWatcher>();
		boidObjects = GameObject.FindGameObjectsWithTag("Boid");
		watcherRb = watcher.GetComponent<Rigidbody>();

		foreach (GameObject boid in boidObjects)
		{
			rigidbodies.Add(boid.GetComponent<Rigidbody>());
		}

	}

	Vector3 CalculateAlignment(Rigidbody currentBoid)
	{
		Vector3 alignment = Vector3.zero;
		foreach (Rigidbody rb in rigidbodies)
		{
			if (rb != currentBoid)
			{
				alignment += rb.velocity;
			}
		}

		alignment /= rigidbodies.Count;
		alignment.Normalize();
		return alignment;
	}

	Vector3 CalculateCohesion(Rigidbody currentBoid)
	{
		Vector3 cohesion = Vector3.zero;
		foreach (Rigidbody rb in rigidbodies)
		{
			if (rb != currentBoid)
			{
				cohesion += rb.position;
			}
		}

		cohesion /= rigidbodies.Count;
		Vector3 directionToAverageCenterOfMass = (cohesion - currentBoid.position);
		directionToAverageCenterOfMass.Normalize();
		return directionToAverageCenterOfMass;
	}

	Vector3 CalculateSeperation(Rigidbody currentBoid)
	{
		Vector3 seperation = Vector3.zero;
		foreach (Rigidbody rb in rigidbodies)
		{
			if (rb != currentBoid)
			{
				seperation += (rb.position - currentBoid.position);
			}
		}

		seperation *= -1;
		seperation /= rigidbodies.Count;
		return seperation;
	}


	void Update()
	{
		Vector3 alignment = Vector3.zero;
		Vector3 cohesion = Vector3.zero;
		Vector3 seperation = Vector3.zero;

		foreach (Rigidbody rb in rigidbodies)
		{
			alignment = CalculateAlignment(rb);
			cohesion = CalculateCohesion(rb);
			seperation = CalculateSeperation(rb);

			Vector3 directionToTarget = watcherRb.position - rb.position;
			float speed = 15.0f;
			Vector3 finalVelocity = alignment + cohesion + seperation + directionToTarget;
			finalVelocity.Normalize();
			finalVelocity *= speed;
			rb.velocity = finalVelocity;
		}
	}
}


