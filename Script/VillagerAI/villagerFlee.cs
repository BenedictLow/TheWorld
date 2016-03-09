using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class villagerFlee : ClassesStateBase {

	public villagerFlee(VillagerFSM fsm, VillagerFSM.State state)
	{
		m_VillagerFSM = fsm;
		m_State = state;
		m_AIPerception = fsm.m_Perception;
		m_AIMovement = fsm.m_Movement;
	}

	public override void Enter()
	{
		
	}

	public override void Execute()
	{
		
	}

	public override void FixedExecute()
	{
		
	}

	public override void End()
	{
		
	}
	void CheckSurr()
	{
		List<GameObject> DetectedObjects = m_AIPerception.CheckPerception ();
	}
}
