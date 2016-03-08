using UnityEngine;
using System.Collections;

public class villagerTravel : ClassesStateBase {

	private float m_speed;
	private int m_pointer;
	private int m_maxWaypoint;
	public villagerTravel(VillagerFSM fsm, VillagerFSM.State state)
	{
		m_VillagerFSM = fsm;
		m_State = state;
	}
	public virtual void Enter()
	{
		m_speed = m_VillagerFSM.speed;
		m_pointer = m_VillagerFSM.pointer;
		m_maxWaypoint = m_VillagerFSM.maxWaypoint;
	}

	public virtual void Execute()
	{
		Travel ();
	}

	public virtual void FixedExecute()
	{
	}

	public virtual void End()
	{}

	void Travel()
	{
		Debug.Log ("Travelling...");
		//m_VillageFSM.nav.speed = speed;
		m_VillagerFSM.nav.speed = m_speed;
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

		m_VillagerFSM.nav.SetDestination (travelTo);
	}

}
