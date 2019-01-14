using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerBlackboard))]
public class ResetAttackTriggers : StateMachineBehaviour {

    PlayerBlackboard blackboard;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        blackboard = animator.GetComponent<PlayerBlackboard>();
        if (blackboard)
        {
            blackboard.Animator.ResetTrigger("HeavyAttack");
            blackboard.Animator.ResetTrigger("LightAttack");
        }
    }
}
