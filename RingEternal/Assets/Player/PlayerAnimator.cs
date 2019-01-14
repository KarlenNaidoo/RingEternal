using UnityEngine;
using RingEternal.MyTools;
/* Actually moves the player
 * It accepts input from PlayerInput, it gets the values from the MovementController about its transitions, and how to move from the PlayerMotor
 */

namespace RingEternal.MyThirdPersonController
{
    public class PlayerAnimator : MonoBehaviour
    {
        


        [SerializeField] float turnSensitivity = 0.2f; // Animator turning sensitivity
        [SerializeField] float turnSpeed = 5f; // Animator turning interpolation speed
        [SerializeField] float runCycleLegOffset = 0.2f; // The offset of leg positions in the running cycle
        [Range(0.1f, 3f)] [SerializeField] float animSpeedMultiplier = 1; // How much the animation of the character will be multiplied by
        [SerializeField] bool _useRootPosition = true;
        private AnimState _animState;
        private Vector3 _fixedDeltaPosition;
        private Quaternion _fixedDeltaRotation = Quaternion.identity;

        protected bool _animatePhysics;

        [SerializeField] GameObject weapon;
        [SerializeField] GameObject weaponParentDestination;
        [SerializeField] GameObject weaponParentOrigin;

        [Header("References")]
        [SerializeField] PlayerBlackboard blackboard;
        [SerializeField] Transform parentTransform;
        [SerializeField] ControllerActionManager controllerActionManager;

        // get Layers from the Animator Controller
        [HideInInspector]
        public AnimatorStateInfo baseLayerInfo, rightArmInfo, leftArmInfo, fullBodyInfo, upperBodyInfo;
        private int baseLayer
        { get { return blackboard.Animator.GetLayerIndex("Base Layer"); } }
        private int rightArmLayer
        { get { return blackboard.Animator.GetLayerIndex("RightArm"); } }
        private int leftArmLayer
        { get { return blackboard.Animator.GetLayerIndex("LeftArm"); } }
        private int upperBodyLayer
        { get { return blackboard.Animator.GetLayerIndex("UpperBody"); } }
        private int fullbodyLayer
        { get { return blackboard.Animator.GetLayerIndex("FullBody"); } }

        private Vector3 lastForward;
        private const string groundedDirectional = "Grounded Directional", groundedStrafe = "Grounded Strafe";
        private float deltaAngle;


        protected virtual void Start()
        {

            lastForward = transform.forward;

        }

        
        public void LayerControl()
        {
            baseLayerInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(baseLayer);
            rightArmInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(rightArmLayer);
            leftArmInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(leftArmLayer);
            upperBodyInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(upperBodyLayer);
            fullBodyInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(fullbodyLayer);
        }

        public Vector3 GetPivotPoint()
        {
            return blackboard.Animator.pivotPosition;
        }

        // Is the Animator playing the grounded animations?
        public bool animationGrounded
        {
            get
            {
                return blackboard.Animator.GetCurrentAnimatorStateInfo(0).IsName(groundedDirectional) || blackboard.Animator.GetCurrentAnimatorStateInfo(0).IsName(groundedStrafe);
            }
        }


        // Update the Animator with the current state of the character controller
        protected virtual void Update()
        {
            if (Time.deltaTime == 0f) return;


            LayerControl();
            EquipWeapon();


            // Calculate the angular delta in character rotation
            float angle = -GetAngleFromForward(lastForward) - deltaAngle;
            deltaAngle = 0f;
            lastForward = transform.forward;
            angle *= turnSensitivity * 0.01f;
            angle = Mathf.Clamp(angle / Time.deltaTime, -1f, 1f);
            Debug.Log("Angle " + Mathf.Clamp(angle / Time.deltaTime, -1, 1));

            UpdateAnimatorParams(angle);
            PlayTargetAnimation();
            CheckForCombo();
        }

        private void UpdateAnimatorParams(float angle)
        {
            _animatePhysics = blackboard.Animator.updateMode == AnimatorUpdateMode.AnimatePhysics;
            _animState = blackboard.AnimState;

            // Jumping
            if (_animState.jump)
            {
                float runCycle = Mathf.Repeat(blackboard.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + runCycleLegOffset, 1);
                float jumpLeg = (runCycle < 0 ? 1 : -1) * _animState.moveDirection.z;

                blackboard.Animator.SetFloat("JumpLeg", jumpLeg);
            }

            // Update Animator params
            blackboard.Animator.SetFloat("Turn", Mathf.Lerp(blackboard.Animator.GetFloat("Turn"), angle, Time.deltaTime * turnSpeed));

            blackboard.Animator.SetFloat("Forward", Mathf.Clamp(_animState.moveDirection.z,-1f,1f));
            blackboard.Animator.SetFloat("Right", _animState.moveDirection.x);
            blackboard.Animator.SetBool("Crouch", _animState.crouch);
            blackboard.Animator.SetBool("OnGround", _animState.onGround);
            blackboard.Animator.SetBool("IsStrafing", _animState.isStrafing);

            if (!_animState.onGround)
            {
                blackboard.Animator.SetFloat("Jump", _animState.yVelocity);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector
            if (_animState.onGround && _animState.moveDirection.z > 0f)
            {
                blackboard.Animator.speed = animSpeedMultiplier;
            }
            else
            {
                // but we don't want to use that while airborne
                blackboard.Animator.speed = 1;
            }
        }

        private float CalculateAngularDelta()
        {
            // Calculate the angular delta in character rotation
            float angle = -GetAngleFromForward(lastForward) - deltaAngle;
            deltaAngle = 0f;
            lastForward = transform.forward;
            angle *= turnSensitivity * 0.01f;
            angle = Mathf.Clamp(angle / Time.deltaTime, -1f, 1f);
            Debug.Log("Angle " + Mathf.Clamp(angle / Time.deltaTime, -1, 1));
            return angle;
        }

        public void OnAnimatorMove()
        {
            if (_useRootPosition)
            {
                parentTransform.position = blackboard.Animator.rootPosition;
            }
            
            // For not using root rotation in Turn value calculation 
            Vector3 f = blackboard.Animator.deltaRotation * Vector3.forward;
            deltaAngle += Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            Move(blackboard.Animator.deltaPosition, blackboard.Animator.deltaRotation);
        }


        // When the Animator moves
        public virtual void Move(Vector3 deltaPosition, Quaternion deltaRotation)
        {

            // Accumulate delta position, update in FixedUpdate to maintain consitency
            _fixedDeltaPosition += deltaPosition;
            _fixedDeltaRotation *= deltaRotation;

            blackboard.DeltaPosition = deltaPosition;
        }
        

        #region Combat Animations
        protected virtual void PlayTargetAnimation()
        {

            string targetAnim;
            if (blackboard.ActionSlot != null && fullBodyInfo.IsName("ResetState")) // we need to be in the empty state in order to transition
            {
                targetAnim = blackboard.ActionSlot.targetAnim;
                blackboard.Animator.Play(targetAnim);
            }

            else if (blackboard.ActionSlot == null && fullBodyInfo.IsName("ResetState"))
            {
                blackboard.CanAttack = false;
                blackboard.DoOnce = false;
            }
        }

        protected void CheckForCombo()
        {
            if (blackboard.CanAttack)
            {
                ControllerActionInput a_input = controllerActionManager.GetActionInput();
                if (a_input == ControllerActionInput.Square && !blackboard.DoOnce)
                {
                    blackboard.Animator.SetTrigger("LightAttack");
                    blackboard.DoOnce = true;
                    return;
                }
                if (a_input == ControllerActionInput.Triangle && !blackboard.DoOnce)
                {
                    blackboard.Animator.SetTrigger("HeavyAttack");
                    blackboard.DoOnce = true;
                    return;
                }
            }

        }
        #endregion


        private void EquipWeapon()
        {
            if (blackboard.CurrentWeapon == WeaponStatus.OneHanded && !blackboard.WeaponEquipped)
            {
                blackboard.Animator.CrossFade("Sword1h_Equip", 0.4f);
                blackboard.WeaponEquipped = true;
                weapon.transform.parent = weaponParentDestination.transform;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localPosition = Vector3.zero;
            }
                
            blackboard.Animator.SetFloat("IsTwoHanded", Mathf.Lerp(blackboard.Animator.GetFloat("IsTwoHanded"), (float)blackboard.CurrentWeapon, Time.deltaTime));
        }
        
        public virtual void PlayHurtAnimation(bool value)
        {
            blackboard.Animator.Play("Idle_Hit_Strong_Right");
        }


        public virtual void Crouch()
        {

            blackboard.IsCrouching = true;

        }

        // Gets angle around y axis from a world space direction
        public float GetAngleFromForward(Vector3 worldDirection)
        {
            Vector3 local = parentTransform.InverseTransformDirection(worldDirection);
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        // This function is called from an Animation Event
        public void CanAttack()
        {
            blackboard.CanAttack = true;
            blackboard.DoOnce = false;
        }


        // This function is called from an Animation Event
        public void CannotAttack()
        {

            //Debug.Log("Closing can attack");
            blackboard.CanAttack = false;
        }


    }


   
}