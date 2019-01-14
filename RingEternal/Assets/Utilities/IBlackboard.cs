using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RingEternal.MyCharacter
{

    // This lists shared variables by any object wanting to have it's own blackboard
    public interface IBlackboard
    {
        Animator Animator { get; }
        bool IsCrouching { get; set; }
        List<HitBoxArea> hitboxes { get; set; }
        List<HitBox> activeHitboxComponents { get; set; }
        void SetAttackParameters(bool shouldAttack);
    }

}