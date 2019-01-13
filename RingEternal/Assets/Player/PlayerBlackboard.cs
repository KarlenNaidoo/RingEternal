using UnityEngine;
using System.Collections;
using static Player.Utility;
using System.Collections.Generic;
using System;
using RingEternal.MyThirdPersonController;
using RingEternal.MyCamera;
using RingEternal.MyCharacter;

public class PlayerBlackboard : MonoBehaviour, IBlackboard
{
    InventoryManager _inventoryManager;
    ControllerActionManager _actionManager;
    PlayerMotor _motor;
    CameraController cam { get; set; } // If assigned, will only update the camera in LateUpdate only if character moves
    float _inputY;
    public float inputX { get; set; }
    public float inputY { set { _inputY = value; } }
    public Vector2 input { get; private set; }
    public Vector2 oldInput { get; set; }
    public bool runByDefault;
    public void SetPlayerInputParameters(float x, float y)
    {
        input = new Vector2(x, y);
    }

    [Header("References")]
    [SerializeField] Rigidbody _playerRigidbody;

    public Rigidbody primaryRigidbody
    {
        get
        {
            return _playerRigidbody;
        }
    }
    [SerializeField] Animator _animator;
    public Animator animator { get { return _animator; } }

    [SerializeField] ControllerActionManager _controllerActionManager;
    public ControllerActionManager controllerActionManager { get { return _controllerActionManager; } }


    // general variables to the locomotion
    protected Vector3 _targetDirection;
    protected Quaternion _targetRotation;

    public WeaponAction actionSlot { get; set; }


    public const int SPRINT_SPEED = 3;
    public const int RUN_SPEED = 2;

    public bool isCrouching { get; set; }
    public bool isSprinting { get; set; }
    public float maxSprintStamina { get; set; }
    public float currentSprintStamina { get; set; }
    public bool useRootMotion { get; set; } = true;
    public float speed { get; set; }

    public Transform lookPos { get; set; }
    public bool lookInCameraDirection { get; set; }

    bool shouldAttack;
    public Quaternion targetRotation { get; set; }

    public List<HitBoxArea> hitboxes { get; set; }
    public List<HitBox> activeHitboxComponents { get; set; }

    public WeaponStatus currentWeapon { get; set; }
    public WeaponList weaponList { get; set; }
    public bool weaponEquipped { get; set; } = false;
    public bool doOnce { get; set; }
    public bool canAttack { get; set; }
    public Transform lockTarget { get; set; }
    private bool _lockOnPressed = false;

    public bool lockOnPressed { get { return _lockOnPressed; } set { _lockOnPressed = !_lockOnPressed; } }


    // New
    public bool smoothFollow { get; set; }
    public AnimState animState { get; set; }
    public Vector3 deltaPosition {get; set; }

    // Animation state
    public struct AnimState
    {
        public Vector3 moveDirection; // the forward speed
        public bool jump; // should the character be jumping?
        public bool crouch; // should the character be crouching?
        public bool onGround; // is the character grounded
        public bool isStrafing; // should the character always rotate to face the move direction or strafe?
        public float yVelocity; // y velocity of the character
    }
    
    //TODO: Create singleton pattern
    private void Awake()
    {
        _actionManager = GetComponent<ControllerActionManager>();
        _motor = GetComponent<PlayerMotor>();
        _inventoryManager = GetComponent<InventoryManager>();
        animState = new AnimState();
        smoothFollow = true;

    }

    

    public void SetAttackParameters(bool shouldAttack)
    {
        this.shouldAttack = shouldAttack;
    }

}
