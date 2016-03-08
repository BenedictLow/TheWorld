using UnityEngine;
using System.Collections;

public class Villager : MonoBehaviour {

	private NavMeshAgent nav;
	public float speed;
	public Transform[] waypoints;
	private int pointer = 0;
	private int maxWaypoint;
	public float minWaypointDistance = 0.1f;
	// Use this for initialization
	void Awake () 
	{
		nav = GetComponent<NavMeshAgent> ();

		maxWaypoint = waypoints.Length - 1;
	}
	
	// Update is called once per frame
	void Update () {
		Travel ();
	}
	void Travel()
	{
		nav.speed = speed;

		Vector3 travelFrom;
		Vector3 travelTo;

		travelFrom = transform.position;
		travelFrom.y = 0.0f;

		travelTo = waypoints [pointer].position;
		if (Vector3.Distance (travelTo, travelFrom) < minWaypointDistance) 
		{
			if (pointer == maxWaypoint) {
				pointer = 0;
			} 
			else
			{
				pointer++;
			}
		}

		nav.SetDestination (travelTo);
	}
}
