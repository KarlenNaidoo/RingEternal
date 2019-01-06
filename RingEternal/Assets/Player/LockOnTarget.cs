using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnTarget : MonoBehaviour {

    PlayerBlackboard blackboard;

	// Use this for initialization
	void Start () {
        blackboard = GetComponentInParent<PlayerBlackboard>();	
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit an enemy: " + other.gameObject.name);
        blackboard.lockTarget = other.transform;
    }
}
