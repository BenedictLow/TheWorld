using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIMovement : MonoBehaviour {
	//private variables that are not accessible through other classes
	//SerializeField allows these variables to be accessed through the Unity Inspector
	//Header allows different variables to be grouped under different names in the Unity Inspector
	[Header ("Physical Properties")]
	[SerializeField] private float m_fMass = 1.0f;
	[SerializeField] private float m_fDrag = 1.0f;
	[SerializeField] private float m_fAngularDrag = 3.5f;

	[Header ("Force Settings")]
	[SerializeField] private float m_fMaxForce = 3.0f; //the max force that can be applied to the rigidbody
	[SerializeField] private float m_fMaxTorque = 2.0f; //the max torque that can be applied to the rigidbody

	[Header ("Wander Settings")]
	[SerializeField] private float m_fWanderRadius = 2.0f; //the radius of the circle projected in front of the agent
	[SerializeField] private float m_fWanderDistance = 4.0f; //the distance that the circle is projected in front of the agent
	[SerializeField] private float m_fWanderJitter = 20.0f; //the amount of jitter alowed every timestep in degrees

	[Header ("Object Avoidance Settings")]
	[SerializeField] private float m_fAvoidanceRadius = 2.0f; //the radius of the agent used when avoiding obstacles
	[SerializeField] private float m_fAvoidanceDistance = 3.0f; //the distance used when looking for objects to avoid
	[SerializeField] private float m_fBrakingMultiplier = 4.0f; //determines how much the agent should break when avoiding an obstacle

	[Header ("Flocking Settings")]
	[SerializeField] private float m_fSeparationWeight = 5.0f; //determines how much separation behaviour contributes to flocking
	[SerializeField] private float m_fAlignmentWeight = 3.0f; //determines how much alignment behaviour contributes to flocking
	[SerializeField] private float m_fCohesionWeight = 1.0f; //determines how much cohesion behaviour contributes to flocking

	private Vector3 m_vWanderTarget; //the current position of the wander target that the agent should seek to when wandering

	private List<Rigidbody> m_Neighbours = null; //list of all the neighbours in the agent's group
	private Rigidbody m_Leader = null; //leader of the agent's group
	private bool m_bIsLeader = false;

	private Rigidbody m_RigidBody = null; //rigidbody component

	private NavMeshPath m_NavPath = null; //navmeshpath
	private int m_nNavPathIndex = 0;

	//cache of obstacle to avoid
	private RaycastHit m_ObstacleCache;
	private float m_fAvoidObstacleTimer = 10.0f;
	private float m_fAvoidObstacleInterval;

	//getter and setter properties that allow other classes to access variables in a controlled way
	public float Mass { get { return m_fMass; } set { m_fMass = Mathf.Abs (value); } }
	public float Drag { get { return m_fDrag; } set { m_fDrag = Mathf.Abs (value); } }
	public float AngularDrag { get { return m_fAngularDrag; } set { m_fAngularDrag = Mathf.Abs (value); } }

	public float MaxForce { get { return m_fMaxForce; } set { m_fMaxForce = Mathf.Abs (value); } }
	public float MaxTorque { get { return m_fMaxTorque; } set { m_fMaxTorque = Mathf.Abs (value); } }

	public float WanderRadius { get { return m_fWanderRadius; } set { m_fWanderRadius = Mathf.Abs (value); } }
	public float WanderDistance { get { return m_fWanderDistance; } set { m_fWanderDistance = Mathf.Abs (value); } }
	public float WanderJitter { get { return m_fWanderJitter; } set { m_fWanderJitter = Mathf.Abs (value); } }
	public Vector3 WanderTarget { get { return m_vWanderTarget; } set { m_vWanderTarget = value.normalized; } }

	public float AvoidanceRadius { get { return m_fAvoidanceRadius; } set { m_fAvoidanceRadius = Mathf.Abs (value); } }
	public float AvoidanceDistance { get { return m_fAvoidanceDistance; } set { m_fAvoidanceDistance = Mathf.Abs (value); } }
	public float BrakingMultiplier { get { return m_fBrakingMultiplier; } set { m_fBrakingMultiplier = Mathf.Max (0.1f, value); } }

	public float SeparationWeight { get { return m_fSeparationWeight; } set { m_fSeparationWeight = Mathf.Abs (value); } }
	public float AlightmentWeight { get { return m_fAlignmentWeight; } set { m_fAlignmentWeight = Mathf.Abs (value); } }
	public float CohesionWeight { get { return m_fCohesionWeight; } set { m_fCohesionWeight = Mathf.Abs (value); } }

	public List<Rigidbody> Neighbours { get { return m_Neighbours; } set { m_Neighbours = value; } }
	public Rigidbody Leader { get { return m_Leader; } set { m_Leader = value; } }
	public bool IsLeader { get { return m_bIsLeader; } set { m_bIsLeader = true; } }


	void Start ()
	{
		//retrieve components
		m_RigidBody = GetComponent<Rigidbody> ();
		m_RigidBody.isKinematic = false; //give control of the agent to the physics engine

		//set rigidbody settings
		m_RigidBody.mass = m_fMass;
		m_RigidBody.drag = m_fDrag;
		m_RigidBody.angularDrag = m_fAngularDrag;

		m_vWanderTarget = transform.forward; //set initial wandertarget

		//set the interval between rechecking for obstavles to avoid
		//prevents all entities from rechecking at the exact same time and causing a lag spike
		m_fAvoidObstacleInterval = Random.Range(0.2f, 0.3f);
	}


	void FixedUpdate()
	{
		//update timer
		m_fAvoidObstacleTimer += Time.deltaTime;
	}


	//public function that gradualy rotates the agent to face the target direction
	public bool RotateTowards(Vector3 _vTargetDir, float _fTorque, bool _fSmooth = true)
	{
		Vector3 vTargetDir = _vTargetDir.normalized; //normalize the target direction

		//check to see if agent is already rotated to the target direction using quick dot product
		//0.999f is an estimated margin of 2.5 degrees
		if (Vector3.Dot (transform.forward, vTargetDir) < 0.999f) {
			//use cross product to determine which way the agent should turn.
			//positive y = turn left, negative y = turn right
			float RotDirection = Vector3.Cross (transform.forward, vTargetDir).y;

			//use dot product to determine if the target direction is behind the agent
			//if target direction is more than 90 degrees either way, set RotDirection to 1/-1
			//OR if smooth is false, prevent the agent from slowing down its rotation when close to the target direction
			if (Vector3.Dot (transform.forward, vTargetDir) < 0.0f || _fSmooth == false)
				RotDirection = Mathf.Sign (RotDirection) * 1.0f;

			//add torque to rotate the agent through unity physics system
			m_RigidBody.AddRelativeTorque (new Vector3 (0.0f, RotDirection * _fTorque, 0.0f));
			return false; // return false if agent is still rotating
		}
		else 
		{
			return true; //return true if agent has finished rotating to face target direction
		}
	}


	//public function that makes the agent rotate and move towards a target position
	//combines both seek and arrive steering behaviours
	public bool Seek(Vector3 _vTargetPos, bool _bArrive = true)
	{
		//check if the agent has reached the target position and gradualy stop if the arrive variable is set to true
		if (_bArrive == false || Vector3.Distance (m_RigidBody.position, _vTargetPos) >= m_fMaxForce)
		{
			//find target direction and rotate towards it
			Vector3 vTargetDir = _vTargetPos - m_RigidBody.position;
			RotateTowards (vTargetDir, m_fMaxTorque);

			//move forward using unity physics engine
			m_RigidBody.AddRelativeForce (Vector3.forward * m_fMaxForce);
			return false; //if target is not yet reached or arrive set to false, return false
		}
		else
		{
			return true; //returns true once the agent has arrived at the target position
		}
	}


	//public function that makes the agent flee from the  position
	public void Flee(Vector3 _vPos)
	{
		//find direction to position and rotate away from it
		Vector3 vTargetDir = _vPos - m_RigidBody.position;
		RotateTowards (-vTargetDir, m_fMaxTorque);

		//move forward using unity physics engine
		m_RigidBody.AddRelativeForce (Vector3.forward * m_fMaxForce);
	}


	//public function that makes the agent chase a target
	//estimates target's future position in order to pursue target better
	public bool Pursuit(Rigidbody _TargetRigidBody)
	{
		//find target direction
		Vector3 vTargetDir = _TargetRigidBody.position - m_RigidBody.position;

		//calculate lookahead time which is:
		//proportional to the distance between agent and target and inversly proportional to the velocity of both
		float LookAheadTime = vTargetDir.magnitude / (m_RigidBody.velocity.magnitude + _TargetRigidBody.velocity.magnitude);

		//seek towards the calculated future position of the target
		return Seek (_TargetRigidBody.position + _TargetRigidBody.velocity * LookAheadTime, false);
	}


	//public function that makes the agent run away from a pursuer
	//estimates pursuer's future position in order to evade better
	public void Evade(Rigidbody _PursuerRigidBody)
	{
		//find direction to pursuer
		Vector3 vTargetDir = _PursuerRigidBody.position - m_RigidBody.position;

		//calculate lookahead time which is:
		//proportional to the distance between agent and target and inversly proportional to the velocity of both
		float LookAheadTime = vTargetDir.magnitude / (m_RigidBody.velocity.magnitude + _PursuerRigidBody.velocity.magnitude);

		//flee from the calculated future position of the target
		Flee (_PursuerRigidBody.position + _PursuerRigidBody.velocity * LookAheadTime);
	}


	//public function that makes the agent wander around randomly
	//projects a circle in front of the agent and seeks to positions on its circumference to produce jitter free motion
	public void Wander()
	{
		//create a quaternion that rotates the wandertarget vector by a small random amount
		Quaternion qVecRotation = Quaternion.Euler(0.0f, Random.Range(-m_fWanderJitter, m_fWanderJitter), 0.0f);
		m_vWanderTarget = qVecRotation * m_vWanderTarget;

		//project circle in front of agent
		Vector3 vWanderCirclePos = m_RigidBody.position + (transform.forward * m_fWanderDistance);

		//seek to the wandertarget which is a position on the circumference of the projected circle
		Seek (vWanderCirclePos + (m_vWanderTarget * m_fWanderRadius), false);
	}


	//public function that makes the agent avoid obstacles
	public bool AvoidObstacles(bool _bIgnoreNeighbours = false, bool _bIgnoreIslands = false)
	{
		RaycastHit ClosestIntersection;

		//if enough time has passed, recheck for obstacles to avoid
		if (m_fAvoidObstacleTimer > m_fAvoidObstacleInterval)
		{
			//cast a sphere in front of the agent to detect objects that are ahead. length of sphere cast depends on velocity
			RaycastHit[] DetectedObstacles = Physics.SphereCastAll (m_RigidBody.position, m_fAvoidanceRadius, transform.forward, m_RigidBody.velocity.magnitude * m_fAvoidanceDistance);

			//for each detected obstacle, attempt to find the closest intersection point to the agent
			ClosestIntersection = new RaycastHit();
			foreach (RaycastHit DetectedObstacle in DetectedObstacles)
			{
				//check if the the detected object is a neighbour and ignore them if bool is set
				bool bIgnoreNeighbour;
				if(m_Neighbours != null)
					bIgnoreNeighbour = (_bIgnoreNeighbours == true && m_Neighbours.Contains(DetectedObstacle.collider.attachedRigidbody) == true);
				else
					bIgnoreNeighbour = false;

				//check if detected object is an island and ignore them if bool is set
				bool bIgnoreIsland = ((DetectedObstacle.collider.tag == "Island" || DetectedObstacle.collider.tag == "SmallIsland" || DetectedObstacle.collider.tag == "PirateIsland") && _bIgnoreIslands == true);

				//check to make sure that detected object is not the agent gameobject
				if(DetectedObstacle.collider.gameObject != gameObject && DetectedObstacle.collider.tag != "Ocean" && bIgnoreNeighbour == false && bIgnoreIsland == false)
				{
					//if the closest intersection point is null, set it to the current one
					if(ClosestIntersection.collider == null)
						ClosestIntersection = DetectedObstacle;
					else //else see if the current obstacle is closer than the set closest intersection
						if(DetectedObstacle.distance < ClosestIntersection.distance)
							ClosestIntersection = DetectedObstacle;
				}
			}

			//update cache
			m_fAvoidObstacleTimer = 0.0f;
			m_ObstacleCache = ClosestIntersection;
		}
		else
		{
			//if not enough time has passed, simply reuse from cache to save computation resources
			ClosestIntersection = m_ObstacleCache;
		}

		//check if any obstacles are in front of the agent
		if (ClosestIntersection.collider != null)
		{
			//calculate offset of the intersection point to the intersected gameobject's center
			Vector3 vIntersectOffset = ClosestIntersection.point - ClosestIntersection.transform.position;

			//fix a bug where if the origin of the other object was inside the raycast, the intersect point would be 0,0,0
			if (ClosestIntersection.point == Vector3.zero)
				vIntersectOffset = transform.position - ClosestIntersection.transform.position;

			//if intersection point is to the left of the center, make agent turn left. otherwise, turn right
			if(Vector3.Dot(vIntersectOffset, transform.right) >= 0.0f)
				m_RigidBody.AddRelativeTorque(new Vector3(0.0f, m_fMaxTorque * 1.5f, 0.0f));
			else
				m_RigidBody.AddRelativeTorque(new Vector3(0.0f, -m_fMaxTorque * 1.5f, 0.0f));

			//make object slow down the closer it gets to the obstacle
			m_RigidBody.AddRelativeForce(Vector3.back * m_fMaxForce / (Mathf.Max(ClosestIntersection.distance / m_fBrakingMultiplier, 1.5f)));

			return true; //return true if the agent is avoiding an obstacle
		}
		else
		{
			return false; //return false if the agent is not avoiding anything
		}
	}


	public void BecomeLeader()
	{
		m_Leader = m_RigidBody;
		m_bIsLeader = true;
		m_Neighbours = new List<Rigidbody> ();
	}


	//public function that makes the agent join a group
	public void JoinGroup(Rigidbody _Leader)
	{
		//find the AgentMovement of the leader
		AIMovement LeaderMovement = _Leader.GetComponent<AIMovement>();

		m_Leader = _Leader; //set the agent's leader
		m_Neighbours = new List<Rigidbody>(LeaderMovement.Neighbours); //add the neighbours
		m_Neighbours.Add (m_Leader); //add leader to the list of neighbours
		m_bIsLeader = false;

		//for every neighbour of the agent
		foreach (Rigidbody Neighbour in m_Neighbours)
		{
			//update their neighbour list to include the agent
			AIMovement NeighbourMovement = Neighbour.GetComponent<AIMovement>();
			NeighbourMovement.Neighbours.Add(m_RigidBody);
		}
	}


	//public function that makes agent leave their group
	public void LeaveGroup()
	{
		//only leave group if agent is part of a group
		if (m_Leader != null && m_Neighbours != null)
		{
			//loop through all neighbours
			foreach (Rigidbody Neighbour in m_Neighbours)
			{
				AIMovement NeighbourMovement = Neighbour.GetComponent<AIMovement>();

				//if agent is a leader, remove all neighbours from their group too
				if (m_bIsLeader == true)
				{
					NeighbourMovement.Leader = null;
					NeighbourMovement.Neighbours = null;
				}
				else
				{
					//otherwise, remove agent from their neighbour lists
					NeighbourMovement.m_Neighbours.Remove(m_RigidBody);
				}
			}
		}

		//remove leader and neighbours from agent
		m_bIsLeader = false;
		m_Leader = null;
		m_Neighbours = null;
	}


	//public function which makes agents move in flocks. combines separation, alignment, and cohesion to create adjustable flocks
	//in this implementation, agents follow around a leader who is the center of the flock
	public void Flocking()
	{
		//check to make sure neighbours and leader are set
		if (m_Leader != null && m_Neighbours != null)
		{
			//calculate total steering from a combination of separation, alignment, and cohesion according to their weights
			Vector3 vTotalSteering = Vector3.zero;
			vTotalSteering += Separation () * m_fSeparationWeight;
			vTotalSteering += Alignment () * m_fAlignmentWeight;
			vTotalSteering += Cohesion () * m_fCohesionWeight;

			//seek towards the calculated direction and stop if the agent has arrived at the target
			if(Seek (m_RigidBody.position + vTotalSteering, true) == true)
				RotateTowards(Alignment(), m_fMaxTorque); //further align agent after arriving
		}
	}


	//separation behaviour which makes the agents move away from its neighbours
	private Vector3 Separation()
	{
		//for each neighbour, calculate a force that move away from them and add them up
		Vector3 vTotalSteering = Vector3.zero;
		foreach (Rigidbody Neighbour in m_Neighbours)
		{
			Vector3 vNeighbourDir = m_RigidBody.position - Neighbour.position;
			vTotalSteering += vNeighbourDir / vNeighbourDir.magnitude;
		}
		return vTotalSteering;
	}


	//alignment behaviour which aligns the agent to the leader's forward direction
	private Vector3 Alignment()
	{
		//returns the leader's forward direction for the agent to align to
		return m_Leader.transform.forward;
	}


	//cohesion behaviour that makes agents move to the center of the flock, in this case the leader
	private Vector3 Cohesion()
	{
		//returns the direction that moves the agent towards the leader
		Vector3 vLeaderDir = m_Leader.position - m_RigidBody.position;
		return vLeaderDir;
	}


	//public function which navigates an agent towards a position using a path generated by Unity's navmesh system
	//return value of 0 meaning that the agent is still navigating, 1 meaning that the agent has finished navigating and -1 meaning that there was an error
	public int NavigateToPos(Vector3 _vTargetPos)
	{
		//if the agent is not currently following a NavMeshPath...
		if (m_NavPath == null || m_NavPath.corners.Length == 0)
		{
			//find the closest position on the navmesh to the target position
			NavMeshHit ClosestPos;
			if (NavMesh.SamplePosition(_vTargetPos, out ClosestPos, 2.0f, NavMesh.AllAreas) == false)
			{
				Debug.Log("Invalid target position for navigation");
				return -1; //if no closest position found, return error
			}

			//lower the target position to the ocean surface
			Vector3 vCurrentPos = m_RigidBody.position;
			vCurrentPos.y = -0.5f;

			//calculate a new NavMeshPath to the target position
			m_NavPath = new NavMeshPath();
			if (NavMesh.CalculatePath(vCurrentPos, ClosestPos.position, NavMesh.AllAreas, m_NavPath) == false)
			{
				ClearNavigation();
				return -1; //if could not plot path, return error
			}

			m_nNavPathIndex = 0;

			return 0;
		}
		else
		{
			//if agent has reached the end of the path return 1
			if (m_nNavPathIndex >= m_NavPath.corners.Length)
			{
				ClearNavigation();
				return 1;
			}
			else
			{
				//else seek to the next point on the path and return 0
				Seek(m_NavPath.corners[m_nNavPathIndex], false);
				AvoidObstacles(true, true);

				//increment the NavPathIndex once the next point on the path has been reached
				if (Vector3.Distance(m_RigidBody.position, m_NavPath.corners[m_nNavPathIndex]) < 2.0f)
					m_nNavPathIndex++;

				return 0;
			}
		}
	}


	//public function which clears the agent's navigation
	public void ClearNavigation()
	{
		m_NavPath = null;
		m_nNavPathIndex = 0;
	}

	void OnDrawGizmos()
	{
		if (m_NavPath != null && m_NavPath.corners.Length != 0)
		{
			Gizmos.color = Color.yellow;

			Gizmos.DrawLine(transform.position, m_NavPath.corners[m_nNavPathIndex]);

			for(int i = m_nNavPathIndex; i < m_NavPath.corners.Length - 1; i++)
				Gizmos.DrawLine(m_NavPath.corners[i], m_NavPath.corners[i + 1]);
		}
	}
}
