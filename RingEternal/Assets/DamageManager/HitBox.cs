using UnityEngine;
using System.Collections;

namespace RingEternal.MyCharacter
{

    public class HitBox : MonoBehaviour
    {
        private IHitboxResponder _responder = null;
        private HitboxControllerBase _hitbox;
        private Animator _anim;

        private void Awake()
        {
            _hitbox = GetComponentInParent<HitboxControllerBase>();
            _anim = GetComponentInParent<Animator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log(gameObject.name + " collided with " + other.gameObject.name);
            _responder?.CollidedWith(other); // ? means it wont throw a Null Reference if _responder is null
        }

        public void SetResponder(IHitboxResponder responder)
        {
            _responder = responder;

        }
    }

}