using UnityEngine;
using System.Collections;
using static Player.Utility;
using System.Collections.Generic;
using System;
using RingEternal.MyThirdPersonController;
using RingEternal.MyCamera;
using RingEternal.MyCharacter;
using RingEternal.MyTools;

public class PlayerBlackboard : MonoBehaviour, IBlackboard
{

    [Header("References")]
    [SerializeField] Rigidbody _playerRigidbody;
    [SerializeField] Animator _animator;

    public Animator Animator => _animator;

    public AnimState AnimState {get; set;}

    public WeaponAction ActionSlot { get; set; }
    public WeaponStatus CurrentWeapon { get; set; }
    public WeaponList WeaponList { get; set; }
    public bool WeaponEquipped { get; set; } = false;
    public bool DoOnce { get; set; }
    public bool CanAttack { get; set; }

    bool _shouldAttack;

    public const int SPRINT_SPEED = 3;
    public const int RUN_SPEED = 2;
    public bool IsCrouching { get; set; }
    public bool IsSprinting { get; set; }
    public float MaxSprintStamina { get; set; }
    public float CurrentSprintStamina { get; set; }
    public float Speed { get; set; }
    public bool RunByDefault { get; set; }
    
    public List<HitBoxArea> hitboxes { get; set; }
    public List<HitBox> activeHitboxComponents { get; set; }


    public Transform LockTarget { get; set; }
    private bool _lockOnPressed = false;
    public bool LockOnPressed { get { return _lockOnPressed; } set { _lockOnPressed = !_lockOnPressed; } }

    
    public bool SmoothFollow { get; set; }

    public Vector3 DeltaPosition { get; set; }

    
    //TODO: Create singleton pattern
    private void Awake()
    {
        SmoothFollow = true;

    }

    public void SetAttackParameters(bool shouldAttack)
    {
        this._shouldAttack = shouldAttack;
    }

}
