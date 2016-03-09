using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class AIPerception : MonoBehaviour {

	//private variables that are not accessible through other classes
	//SerializeField allows these variables to be accessed through the Unity Inspector
	//Header allows different variables to be grouped under different names in the Unity Inspector
	//Serialize Field is pretty cool huh
	[Header ("Perception Settings")]
	[SerializeField] private float 	m_fFieldOfView = 70.0f; //FOV for agent sight
	[SerializeField] private float 	m_fViewingDistance = 50.0f; //the distance that the agent is able to see
	[SerializeField] private float 	m_fViewingRotation = 0.0f; //the rotation of the agent's sight (eg. AI turns head)
	[SerializeField] private float 	m_fHearingRadius = 30.0f; //the radius that the agent is able to hear
	[SerializeField] private bool 	m_bSenseThroughObjects = false; //boolean to allow the agent to sense through objects

	//private variables used to change gizmo colours
	private bool m_bSightDetected = false;
	private bool m_bHearingDetected = false;

	//The list used to store the gameobjects that are detected by sight and sound
	private List<GameObject> m_SightList = null;
	private List<GameObject> m_HearList = null;

	//timers for sight and sound
	private float m_fSightTimer = 0.0f;
	private float m_fHearTimer = 0.0f;
	private float m_fPerceptionInterval;

	//getter and setter properties that allow other classes to access variables in a controlled way
	public float FieldOfView 		{ get { return m_fFieldOfView; } set { m_fFieldOfView = Mathf.Clamp(value, 0.0f, 360.0f); } }
	public float ViewingDistance 	{ get { return m_fViewingDistance; } set { m_fViewingDistance = Mathf.Abs (value); } }
	public float ViewingRotation 	{ get { return m_fViewingRotation; } set { m_fViewingRotation = value; } }
	public float HearingRadius 		{ get { return m_fHearingRadius; } set { m_fHearingRadius = Mathf.Abs (value); } }
	public bool SenseThroughObjects { get { return m_bSenseThroughObjects; } set { m_bSenseThroughObjects = value; } }

	// Use this for initialization
	void Start () 
	{
		//prevents all the ai from rechecking perception at the same time and causing lag
		m_fPerceptionInterval = Random.Range (0.3f, 0.4f);
	}
	
	// Update is called once per frame
	void Update () 
	{
		//update timers
		m_fSightTimer += Time.deltaTime;
		m_fHearTimer += Time.deltaTime;
	}

	//public function that allows agents to sense their surroundings through sight and hearing
	//returns List<T> of GameObjects that have been sensed by the agent
	public List<GameObject> CheckPerception(bool _bUseCache = true)
	{
		//check sight and hearing and combine returned lists into one
		List<GameObject> GameObjectList = CheckSight (_bUseCache);
		GameObjectList.AddRange(CheckHearing (_bUseCache));

		//use LINQ Distinct to remove duplicate entries in the list
		GameObjectList = GameObjectList.Distinct().ToList ();

		return GameObjectList;
	}

	//First Check Sight
	public List<GameObject> CheckSight(bool _bUseList = true)
	{
		//check sight if enough time has passed or if the caller does not want to use the existing list, or if sight list does not exist
		if (_bUseList == false || m_SightList == null || m_fSightTimer > m_fPerceptionInterval) 
		{
			//reset gizmos for sight to default
			m_bSightDetected = false;
			List<GameObject> GameObjectList = new List<GameObject> (); //create new List<T> for GameObjects

			//calculate the direction the agent is looking in by rotating the forward direction Vector by a Quaternion
			Vector3 vViewingDir = Quaternion.Euler(0.0f, m_fViewingRotation, 0.0f) * transform.forward;

			//detect all gameobjects with collider in a circle radius around the agent
			//this checks for gameobjects that would be within the agent's range of sight
			Collider[] DetectedObjects = Physics.OverlapSphere (transform.position, m_fViewingDistance);

			//loop through all gameobjects detected
			foreach (Collider DetectedObject in DetectedObjects)
			{
				if(DetectedObject.gameObject != gameObject && DetectedObject.tag != "Land") //exclude the agent gameobject so that it does not see itself or ocean
				{
					//calculate the vector that points from the agent to the detected gameobject
					Vector3 vGameObjectDir = DetectedObject.transform.position - transform.position;

					//if the detected gameobject is not hidden behind another gameobject AND
					//if the detected gameobject is within the FOV of the agent (angle between viewing direction and the gameobjects direction < fov/2)
					if(CheckIfHidden(DetectedObject.gameObject) == false && Vector3.Angle(vViewingDir, vGameObjectDir) <= m_fFieldOfView / 2.0f)
					{
						//detected gameobject can be seen by the agent and is added to the list
						GameObjectList.Add (DetectedObject.gameObject);
						m_bSightDetected = true; //update sight gizmo colour
					}
				}
			}

			//reset the timer and update the cache to the new list of gameobjects detected by sight
			m_fSightTimer = 0.0f;
			m_SightList = GameObjectList;

			return GameObjectList;
		}
		//else if not enough time has passed yet, simply return the list stored in cache to reduce computation requirements
		else
		{
			//loop through the cache backwards and remove any gameobjects that have been destroyed
			//loop is done backwards so that objects removed won't affect the current index i
			for (int i = m_SightList.Count - 1; i >= 0; i--)
				if (m_SightList[i] == null)
					m_SightList.RemoveAt(i);

			return m_SightList;
		}
	}

	//public function that allows agents to sense surroundings through hearing
	//returns List<T> of GameObjects that have been heard by the agent
	public List<GameObject> CheckHearing(bool _bUseCache = true)
	{
		//only recheck hearing if enough time has passed or if the caller does not want to use the cache
		if (_bUseCache == false || m_HearList == null || m_fHearTimer > m_fPerceptionInterval)
		{
			m_bHearingDetected = false; //reset hearing gizmo colour to default
			List<GameObject> GameObjectList = new List<GameObject> (); //create new List<T> for GameObjects

			//detect all gameobjects with collider in a radius around the agent
			//this checks for gameobjects that are within the agent's hearing radius
			Collider[] DetectedObjects = Physics.OverlapSphere (transform.position, m_fHearingRadius);

			//loop through all gameobjects detected
			foreach (Collider DetectedObject in DetectedObjects)
			{
				//check to ensure that the gameobject is not the agent itself or the ocean
				if(DetectedObject.gameObject != gameObject && DetectedObject.tag != "Land")
				{
					//detected gameobject can be heard by the agent and is added to the list
					GameObjectList.Add (DetectedObject.gameObject);
					m_bHearingDetected = true; //update hearing gizmo colour
				}
			}

			//reset the timer and update the cache to the new list of gameobjects detected by hearing
			m_fHearTimer = 0.0f;
			m_HearList = GameObjectList;

			return GameObjectList;
		}
		//else if not enough time has passed yet, simply return the list stored in cache to reduce computation requirements
		else
		{
			//loop through the cache backwards and remove any gameobjects that have been destroyed
			//loop is done backwards so that objects removed won't affect the current index i
			for (int i = m_HearList.Count - 1; i >= 0; i--)
				if (m_HearList[i] == null)
					m_HearList.RemoveAt(i);

			return m_HearList;
		}
	}


	//private function that checks if a gameobject is hidden behind another object from the agent's point of view
	private bool CheckIfHidden(GameObject _GameObject)
	{
		//stop checking and return false as the agent can sense through objects and gameobjects cannot hide behind them
		if (m_bSenseThroughObjects == true)
			return false;

		//cast a line between the agent and the gameobject
		RaycastHit hit;
		Physics.Linecast (transform.position, _GameObject.transform.position, out hit);

		//check if there is anything between the agent and the gameobject
		if (hit.collider != null && hit.collider.gameObject != _GameObject)
			return true;
		else
			return false;
	}

	//draw gizmos to help visualise the viewing cone and hearing radius
	void OnDrawGizmos()
	{
		Vector3 vPosition = transform.position;

		//viewing cone will turn from green to red if the agent sees something
		if(m_bSightDetected == true)
			Gizmos.color = Color.red;
		else
			Gizmos.color = Color.green;

		//calculate the direction the agent is looking in by rotating the forward direction Vector by a Quaternion
		Vector3 vViewingDir = Quaternion.Euler(0.0f, m_fViewingRotation, 0.0f) * transform.forward;

		//draw the viewing cone
		Gizmos.DrawLine(vPosition, vPosition + Quaternion.Euler(0.0f, m_fFieldOfView / 2.0f, 0.0f) * vViewingDir * m_fViewingDistance);
		Gizmos.DrawLine(vPosition, vPosition + Quaternion.Euler(0.0f, m_fFieldOfView / -2.0f, 0.0f) * vViewingDir * m_fViewingDistance);
		Gizmos.DrawLine(vPosition, vPosition + vViewingDir * m_fViewingDistance);

		//hearing radius will turn from green to red if the agent hears something
		if(m_bHearingDetected == true)
			Gizmos.color = Color.red;
		else
			Gizmos.color = Color.green;

		//draw hearing radius
		Gizmos.DrawWireSphere(vPosition, m_fHearingRadius);
	}
}
