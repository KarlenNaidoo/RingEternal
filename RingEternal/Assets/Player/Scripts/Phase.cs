using UnityEngine;
using System.Collections;

public class Phase : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] PlayerBlackboard _blackboard;
    Rigidbody _rb;
    float offsetY;
    [SerializeField] float phaseDistance = 10f;
    int layerMask = 1 << 16; // ground
    Vector3 _desiredPosition;
    float _groundDistance;
    // Use this for initialization
  
    void Start()
    {
        _rb = _blackboard.primaryRigidbody;
        
        // But instead we want to collide against everything except layermask. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;


    }

    // Update is called once per frame
    protected void FixedUpdate()
    {
        float contextualPhaseDistance = phaseDistance;
        if (_blackboard.actionSlot != null && _blackboard.actionSlot.inputButton == ControllerActionInput.L1 && _blackboard.input.sqrMagnitude > 0.3f)
        {
            contextualPhaseDistance = CheckForObjectCollision();
            Debug.Log("Phase: " + contextualPhaseDistance);
            _desiredPosition = transform.position + transform.forward * contextualPhaseDistance ;
            //_groundDistance = GroundDistance();
            //_desiredPosition.y -= 0;
            _rb.position = _desiredPosition;

           // _blackboard.animator.Play("Phase");
        }
    }

    float GroundDistance()
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, -transform.up, out hit, 100f, layerMask))
        {
            Debug.Log("Ground distance: " + hit.distance);
            return hit.distance;
        }

        return 0;
    }

    float CheckForObjectCollision()
    {

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.forward, out hit, phaseDistance, layerMask))
        {
            offsetY = hit.point.y;
            return hit.distance > 0.5 ? hit.distance - 0.5f : 0;
        }
        else
        {
            return phaseDistance;
        }
    }
}
