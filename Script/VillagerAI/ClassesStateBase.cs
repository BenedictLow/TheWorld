using UnityEngine;
using System.Collections;

public class ClassesStateBase : MonoBehaviour {

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
