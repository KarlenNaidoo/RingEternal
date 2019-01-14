using UnityEngine;

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
        
        private Vector3 fixedDeltaPosition;
        private Quaternion fixedDeltaRotation = Quaternion.identity;

        protected bool animatePhysics;

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
        { get { return blackboard.animator.GetLayerIndex("Base Layer"); } }
        private int rightArmLayer
        { get { return blackboard.animator.GetLayerIndex("RightArm"); } }
        private int leftArmLayer
        { get { return blackboard.animator.GetLayerIndex("LeftArm"); } }
        private int upperBodyLayer
        { get { return blackboard.animator.GetLayerIndex("UpperBody"); } }
        private int fullbodyLayer
        { get { return blackboard.animator.GetLayerIndex("FullBody"); } }

        private Vector3 lastForward;
        private const string groundedDirectional = "Grounded Directional", groundedStrafe = "Grounded Strafe";
        private float deltaAngle;


        protected virtual void Start()
        {

            lastForward = transform.forward;

        }


        public void LayerControl()
        {
            baseLayerInfo = blackboard.animator.GetCurrentAnimatorStateInfo(baseLayer);
            rightArmInfo = blackboard.animator.GetCurrentAnimatorStateInfo(rightArmLayer);
            leftArmInfo = blackboard.animator.GetCurrentAnimatorStateInfo(leftArmLayer);
            upperBodyInfo = blackboard.animator.GetCurrentAnimatorStateInfo(upperBodyLayer);
            fullBodyInfo = blackboard.animator.GetCurrentAnimatorStateInfo(fullbodyLayer);
        }

        public Vector3 GetPivotPoint()
        {
            return blackboard.animator.pivotPosition;
        }

        // Is the Animator playing the grounded animations?
        public bool animationGrounded
        {
            get
            {
                return blackboard.animator.GetCurrentAnimatorStateInfo(0).IsName(groundedDirectional) || blackboard.animator.GetCurrentAnimatorStateInfo(0).IsName(groundedStrafe);
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
            animatePhysics = blackboard.animator.updateMode == AnimatorUpdateMode.AnimatePhysics;

            // Jumping
            if (blackboard.animState.jump)
            {
                float runCycle = Mathf.Repeat(blackboard.animator.GetCurrentAnimatorStateInfo(0).normalizedTime + runCycleLegOffset, 1);
                float jumpLeg = (runCycle < 0 ? 1 : -1) * blackboard.animState.moveDirection.z;

                blackboard.animator.SetFloat("JumpLeg", jumpLeg);
            }

            // Update Animator params
            blackboard.animator.SetFloat("Turn", Mathf.Lerp(blackboard.animator.GetFloat("Turn"), angle, Time.deltaTime * turnSpeed));

            blackboard.animator.SetFloat("Forward", Mathf.Clamp(blackboard.animState.moveDirection.z,-1f,1f));
            blackboard.animator.SetFloat("Right", blackboard.animState.moveDirection.x);
            blackboard.animator.SetBool("Crouch", blackboard.animState.crouch);
            blackboard.animator.SetBool("OnGround", blackboard.animState.onGround);
            blackboard.animator.SetBool("IsStrafing", blackboard.animState.isStrafing);

            if (!blackboard.animState.onGround)
            {
                blackboard.animator.SetFloat("Jump", blackboard.animState.yVelocity);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector
            if (blackboard.animState.onGround && blackboard.animState.moveDirection.z > 0f)
            {
                blackboard.animator.speed = animSpeedMultiplier;
            }
            else
            {
                // but we don't want to use that while airborne
                blackboard.animator.speed = 1;
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
            if (blackboard.useRootMotion)
            {
                parentTransform.position = blackboard.animator.rootPosition;
            }
            
            // For not using root rotation in Turn value calculation 
            Vector3 f = blackboard.animator.deltaRotation * Vector3.forward;
            deltaAngle += Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            Move(blackboard.animator.deltaPosition, blackboard.animator.deltaRotation);
        }


        // When the Animator moves
        public virtual void Move(Vector3 deltaPosition, Quaternion deltaRotation)
        {

            // Accumulate delta position, update in FixedUpdate to maintain consitency
            fixedDeltaPosition += deltaPosition;
            fixedDeltaRotation *= deltaRotation;

            blackboard.deltaPosition = deltaPosition;
        }
        

        #region Combat Animations
        protected virtual void PlayTargetAnimation()
        {

            string targetAnim;
            if (blackboard.actionSlot != null && fullBodyInfo.IsName("ResetState")) // we need to be in the empty state in order to transition
            {
                targetAnim = blackboard.actionSlot.targetAnim;
                blackboard.animator.Play(targetAnim);
            }

            else if (blackboard.actionSlot == null && fullBodyInfo.IsName("ResetState"))
            {
                blackboard.canAttack = false;
                blackboard.doOnce = false;
            }
        }

        protected void CheckForCombo()
        {
            if (blackboard.canAttack)
            {
                ControllerActionInput a_input = controllerActionManager.GetActionInput();
                if (a_input == ControllerActionInput.Square && !blackboard.doOnce)
                {
                    blackboard.animator.SetTrigger("LightAttack");
                    blackboard.doOnce = true;
                    return;
                }
                if (a_input == ControllerActionInput.Triangle && !blackboard.doOnce)
                {
                    blackboard.animator.SetTrigger("HeavyAttack");
                    blackboard.doOnce = true;
                    return;
                }
            }

        }
        #endregion


        private void EquipWeapon()
        {
            if (blackboard.currentWeapon == WeaponStatus.OneHanded && !blackboard.weaponEquipped)
            {
                blackboard.animator.CrossFade("Sword1h_Equip", 0.4f);
                blackboard.weaponEquipped = true;
                weapon.transform.parent = weaponParentDestination.transform;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localPosition = Vector3.zero;
            }
                
            blackboard.animator.SetFloat("IsTwoHanded", Mathf.Lerp(blackboard.animator.GetFloat("IsTwoHanded"), (float)blackboard.currentWeapon, Time.deltaTime));
        }
        
        public virtual void PlayHurtAnimation(bool value)
        {
            blackboard.animator.Play("Idle_Hit_Strong_Right");
        }


        public virtual void Crouch()
        {

            blackboard.isCrouching = true;

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
            blackboard.canAttack = true;
            blackboard.doOnce = false;
        }


        // This function is called from an Animation Event
        public void CannotAttack()
        {

            //Debug.Log("Closing can attack");
            blackboard.canAttack = false;
        }


    }
    
}