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
		CheckSurr ();
	}

	public override void FixedExecute()
	{
		DecideTravelDestination ();
		int travelStatus = TravelToArea (m_VillagerFSM.waypoints [m_pointer].position);
	}

	public override void End()
	{
		
	}

	void DecideTravelDestination()
	{
		Vector3 travelFrom;
		Vector3 travelTo;

		travelFrom = m_VillagerFSM.transform.position;
		travelFrom.y = 0.0f;

		travelTo = m_VillagerFSM.waypoints [m_pointer].position;
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

	private int TravelToArea(Vector3 TargetPos)
	{
		return m_AIMovement.NavigateToPos (TargetPos);
	}
}
