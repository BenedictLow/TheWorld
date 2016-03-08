using UnityEngine;
using System.Collections;

public class villagerTravel : ClassesStateBase {

	public villagerTravel(VillagerFSM fsm, VillagerFSM.State state)
	{
		m_VillagerFSM = fsm;
		m_State = state;
	}
	public virtual void Enter()
	{
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
		//m_VillageFSM.nav.speed = speed;
		m_VillagerFSM.nav.speed = m_VillagerFSM.speed;
		Vector3 travelFrom;
		Vector3 travelTo;

		travelFrom = m_VillagerFSM.transform.position;
		travelFrom.y = 0.0f;

		travelTo = m_VillagerFSM.waypoints [m_VillagerFSM.pointer].position;
		if (Vector3.Distance (travelTo, travelFrom) < m_VillagerFSM.minWaypointDistance) 
		{
			if (m_VillagerFSM.pointer == m_VillagerFSM.maxWaypoint) {
				m_VillagerFSM.pointer = 0;
			} 
			else
			{
				m_VillagerFSM.pointer++;
			}
		}

		m_VillagerFSM.nav.SetDestination (travelTo);
	}

}
