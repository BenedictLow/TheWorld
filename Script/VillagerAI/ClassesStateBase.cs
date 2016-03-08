using UnityEngine;
using System.Collections;

public abstract class ClassesStateBase  {

	protected VillagerFSM m_VillagerFSM = null;
	protected VillagerFSM.State m_State;
	public virtual void Enter()
	{
	}

	public virtual void Execute()
	{}

	public virtual void FixedExecute()
	{}

	public virtual void End()
	{}
}
