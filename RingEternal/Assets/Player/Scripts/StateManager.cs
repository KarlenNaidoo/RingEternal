using UnityEngine;
using System.Collections;
using Player.PlayerController;
using Player;

public class StateManager : PlayerMotor, IHitboxResponder
{
    
    [SerializeField] float maxSprintStamina = 10f;
    [Header("References")]
    [SerializeField] HitBox _hitbox;
    [SerializeField] PlayerHitboxController _hitboxController;
    public HitboxProfile[] hitboxProfile;

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void Start()
    {
        base.Start();
        _blackboard.maxSprintStamina = maxSprintStamina;
        _blackboard.currentSprintStamina = maxSprintStamina;
    }
    

    


    public override void PlayHurtAnimation(bool value)
    {
        blackboard.animator.Play("Idle_Hit_Strong_Right");
    }
    

    public void CollidedWith(Collider collider)
    {
        Debug.Log("Player collided with " + collider.gameObject.name);
        Hurtbox hurtbox = collider.GetComponent<Hurtbox>();
        IHealthController hurtBoxController = hurtbox.GetComponentInParent<IHealthController>(); // the parent gameobject will implement the health and damage
        Damage attackDamage = new Damage(15);
        hurtBoxController?.ReceiveDamage(attackDamage);
    }


    private void SetResponderToHitbox()
    {
        Debug.Log("Setting player as responder to hitbox");
        _hitbox.SetResponder(this);
    }
}
