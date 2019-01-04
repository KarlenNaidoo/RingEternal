using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Player.Utility;

public class MeleeAttackControl : StateMachineBehaviour {


    private IBlackboard _blackboard;
    public List<HitBoxArea> hitboxes;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {    
        _blackboard = animator.GetComponent<IBlackboard>();
        if (_blackboard != null)
        {
            _blackboard.hitboxes = new List<HitBoxArea>();
            _blackboard.hitboxes = hitboxes; // Updates the blackboard with the new list according to the current animation playing
       
        }
        
	}
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_blackboard != null)
            _blackboard.SetAttackParameters(false);
    }
    

}
