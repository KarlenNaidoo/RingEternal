using UnityEngine;
using System.Collections;
using RingEternal.MyCharacter;

namespace RingEternal.MyThirdPersonController
{

    public class StateManager : PlayerMotor, IHitboxResponder
    {

        [SerializeField] float maxSprintStamina = 10f;
        [Header("References")]
        [SerializeField] HitBox _hitbox;
        [SerializeField] PlayerHitboxController _hitboxController;
        public HitboxProfile[] hitboxProfile;


        protected override void Start()
        {
            base.Start();
            _blackboard.MaxSprintStamina = maxSprintStamina;
            _blackboard.CurrentSprintStamina = maxSprintStamina;
        }





        public void PlayHurtAnimation(bool value)
        {
            _blackboard.Animator.Play("Idle_Hit_Strong_Right");
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
            //_hitbox.SetResponder(this);
        }
    }

}