using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class VillagerFSM : MonoBehaviour {

	public ClassesStateBase TravelState = null;
	public ClassesStateBase CurrentState;
	private List<ClassesStateBase> m_States = new List<ClassesStateBase> ();
	public enum State{Travel};

	public NavMeshAgent nav;
	public float speed;
	public Transform[] waypoints;
	public int pointer = 0;
	public int maxWaypoint;
	public float minWaypointDistance = 0.1f;

	// Use this for initialization
	void Start () 
	{
		nav = GetComponent<NavMeshAgent> ();
		maxWaypoint = waypoints.Length - 1;

		m_States = new List<ClassesStateBase> ();
		TravelState = new villagerTravel (this, State.Travel);
		m_States.Add (TravelState);
		ChangeState (State.Travel);
	}
	
	// Update is called once per frame
	void Update () 
	{
		CurrentState.Execute ();
	}

	public void ChangeState(State changeState)
	{
		if (changeState == State.Travel) 
		{
			CurrentState = TravelState;
		}
		CurrentState.Enter ();
	}
}
