using UnityEngine;
using System.Collections;

public abstract class ClassesStateBase  {

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
