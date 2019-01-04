using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Player.Utility;
using Player.PlayerController;

public class PlayerHitboxController : HitboxControllerBase
{
    [Header("References")]
    [SerializeField] StateManager stateManager;

    protected override void Awake()
    {
        base.Awake();
    }


    protected override List<HitBox> GetActiveHitboxComponents()
    {
        List<HitBox> activeHitboxes = new List<HitBox>();
        if (_blackboard.hitboxes != null)
        {

            for (int i = 0; i < _blackboard.hitboxes.Count; i++)
            {
                for (int j = 0; j < stateManager.hitboxProfile.Length; j++)
                {
                    if (stateManager.hitboxProfile[j].hitboxArea == _blackboard.hitboxes[i])
                    {
                        activeHitboxes.Add(stateManager.hitboxProfile[j].hitBox.GetComponent<HitBox>());
                    }
                }
            }
        }
        return activeHitboxes;
    }


    protected override List<Collider> GetActiveHitboxTriggers()
    {
        List<Collider> activeHitboxTriggers = new List<Collider>();
        if (_blackboard.hitboxes != null && _blackboard.hitboxes.Count > 0)
        {
            for (int i = 0; i < _blackboard.hitboxes.Count; i++)
            {
                for (int j = 0; j < stateManager.hitboxProfile.Length; j++)
                {
                    if (stateManager.hitboxProfile[j].hitboxArea == _blackboard.hitboxes[i])
                    {
                        activeHitboxTriggers.Add(stateManager.hitboxProfile[j].hitBox);
                    }
                }
            }
        }
        return activeHitboxTriggers;
    }


}
