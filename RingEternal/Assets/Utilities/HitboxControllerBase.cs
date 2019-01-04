using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Player.Utility;

public abstract class HitboxControllerBase : MonoBehaviour
{

    protected IBlackboard _blackboard;
    List<Collider> _hitBoxTriggers;
    Hurtbox[] _hurtboxes;
    private ColliderState _state;
    private HitBox[] _hitboxes;


    protected virtual void Awake()
    {
        _hitboxes = GetComponentsInChildren<HitBox>(true);

        _blackboard = GetComponent<IBlackboard>();
        _hurtboxes = GetComponentsInChildren<Hurtbox>(true);
    }

    protected virtual void Start()
    {
        IgnoreOwnHurtboxes();
    }

    protected virtual void IgnoreOwnHurtboxes()
    {
        foreach (Hurtbox hurtbox in _hurtboxes)
        {
            foreach (HitBox hitbox in _hitboxes)
            {
                //Debug.Log("Setting ignore collision for " + hitbox.gameObject.name);
                Physics.IgnoreCollision(hurtbox.gameObject.GetComponent<Collider>(), hitbox.gameObject.GetComponent<Collider>());
            }
        }
    }

    protected abstract List<HitBox> GetActiveHitboxComponents();


    protected abstract List<Collider> GetActiveHitboxTriggers();

    public void EnableHitbox(bool value)
    {
        List<Collider> hitboxTriggers = new List<Collider>();
        hitboxTriggers = GetActiveHitboxTriggers();
        foreach (var hitbox in hitboxTriggers)
        {
            hitbox.enabled = value;
        }
    }


    protected virtual bool OpenHitBox()
    {
        _state = ColliderState.Open;
        //Debug.Log("OPEN HITBOX");
        EnableHitbox(true);
        _blackboard.activeHitboxComponents = GetActiveHitboxComponents();
        return true;
    }


    protected virtual bool CloseHitBox()
    {
        //Debug.Log("CLOSE HITBOX");
        EnableHitbox(false);
        return true;
    }

    private void checkGizmoColor()
    {
        switch (_state)
        {

            case ColliderState.Closed:

                Gizmos.color = Color.blue;

                break;

            case ColliderState.Open:

                Gizmos.color = Color.green;

                break;

            case ColliderState.Colliding:

                Gizmos.color = Color.yellow;

                break;

        }

    }

    private void OnDrawGizmos()
    {

        if (!Application.isPlaying) return;
        checkGizmoColor();

        _hitBoxTriggers = GetActiveHitboxTriggers(); // TODO: Causes a null pointer error upon exiting play mode

        if (_hitBoxTriggers != null)
        {
            foreach (var _hitBoxTrigger in _hitBoxTriggers)
            {

                if (_hitBoxTrigger && _hitBoxTrigger.enabled)
                {
                    if (_hitBoxTrigger as BoxCollider)
                    {

                        BoxCollider box = _hitBoxTrigger as BoxCollider;
                        var sizeX = transform.lossyScale.x * box.size.x;
                        var sizeY = transform.lossyScale.y * box.size.y;
                        var sizeZ = transform.lossyScale.z * box.size.z;
                        Matrix4x4 rotationMatrix = Matrix4x4.TRS(box.bounds.center, transform.rotation, new Vector3(sizeX, sizeY, sizeZ));
                        Gizmos.matrix = rotationMatrix;
                        Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    }
                }
            }
        }
    }
}


[System.Serializable]
public class HitboxProfile
{
    public HitBoxArea hitboxArea;
    public Collider hitBox;

}
