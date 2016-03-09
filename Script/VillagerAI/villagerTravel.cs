using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class villagerTravel : ClassesStateBase {

	private float m_speed;
	private int m_pointer;
	private int m_maxWaypoint;
	public villagerTravel(VillagerFSM fsm, VillagerFSM.State state)
	{
		m_VillagerFSM = fsm;
		m_State = state;
		m_AIPerception = fsm.m_Perception;
		m_AIMovement = fsm.m_Movement;
	}

	public override void Enter()
	{
		m_speed = m_VillagerFSM.speed;
		m_pointer = m_VillagerFSM.pointer;
		m_maxWaypoint = m_VillagerFSM.maxWaypoint;
		CheckNearestSafeZone ();
	}

	public override void Execute()
	{
		if (m_VillagerFSM.nav.isActiveAndEnabled == false)
			m_VillagerFSM.nav.enabled = true;

		CheckSurr ();
		Travel ();
	}

	public override void FixedExecute()
	{
		
	}

	public override void End()
	{}

	void Travel()
	{
		Debug.Log (m_speed);
		m_VillagerFSM.nav.speed = m_speed;
		Vector3 travelFrom;
		Vector3 travelTo;

		travelFrom = m_VillagerFSM.transform.position;
		travelFrom.y = 0.0f;

		travelTo = m_VillagerFSM.waypoints [m_pointer].position;
		Debug.Log (m_VillagerFSM.maxWaypoint);
		if (Vector3.Distance (travelTo, travelFrom) < m_VillagerFSM.minWaypointDistance) 
		{
			if (m_pointer == m_maxWaypoint) {
				m_pointer = 0;
			} 
			else
			{
				m_pointer++;
			}
		}
		Debug.Log (m_pointer);
		m_VillagerFSM.nav.SetDestination (travelTo);
	}

	void CheckNearestSafeZone()
	{
		int nearest = 0;
		for (int i = 0; i < m_VillagerFSM.waypoints.Length; i++) 
		{
			if (Vector3.Distance (m_VillagerFSM.transform.position, m_VillagerFSM.waypoints [nearest].position) 
				> Vector3.Distance (m_VillagerFSM.transform.position, m_VillagerFSM.waypoints [i].position)) 
			{
				nearest = i;
			}
		}
		m_pointer = nearest;
		Debug.Log (m_pointer);
	}

	void CheckSurr()
	{
		List<GameObject> DetectedObjects = m_AIPerception.CheckPerception ();
	}
}
