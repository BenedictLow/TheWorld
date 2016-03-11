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
	float m_fTimer;
	GameObject closestEnemy;
	float closestDistance;
	public override void Enter()
	{
		closestEnemy = null;
		closestDistance = 0.0f;
		m_fTimer = 0.0f;
	}

	public override void Execute()
	{
		CheckSurr ();
		m_fTimer += Time.deltaTime;
		if (m_fTimer > 20) 
		{
			m_VillagerFSM.ChangeState (VillagerFSM.State.Travel);
		}
	}

	public override void FixedExecute()
	{
		Flee ();
		Debug.Log (m_fTimer);
	}

	public override void End()
	{
		
	}
	void CheckSurr()
	{
		List<GameObject> DetectedObjects = m_AIPerception.CheckPerception ();
		foreach (GameObject Detected in DetectedObjects) 
		{
			if (Detected.tag == "Monster") 
			{
				float EnemyDistance = Vector3.Distance (m_VillagerFSM.gameObject.transform.position, Detected.transform.position);
				if (closestEnemy == null || EnemyDistance < closestDistance) 
				{
					closestEnemy = Detected;
					closestDistance = EnemyDistance;
				}
			}
		}
	}

	void Flee()
	{
		if (closestEnemy != null) 
		{
			m_AIMovement.Flee (closestEnemy.transform.position);
			m_AIMovement.Flee (closestEnemy.transform.position);
		}
	}
}
