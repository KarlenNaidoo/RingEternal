using UnityEngine;
using RingEternal.MyTools;
/* Actually moves the player
 * It accepts input from PlayerInput, it gets the values from the MovementController about its transitions, and how to move from the PlayerMotor
 */

namespace RingEternal.MyThirdPersonController
{
    public class PlayerAnimator : MonoBehaviour
    {
        


       
      
        private AnimState _animState;
       
        private float deltaAngle;

        protected bool _animatePhysics;

     

        [Header("References")]
        [SerializeField] PlayerBlackboard blackboard;
        [SerializeField] ControllerActionManager controllerActionManager;
        [SerializeField] float turnSpeed = 5f; // Animator turning interpolation speed
        [SerializeField] float runCycleLegOffset = 0.2f; // The offset of leg positions in the running cycle
        [Range(0.1f, 3f)] [SerializeField] float animSpeedMultiplier = 1; // How much the animation of the character will be multiplied by

        // get Layers from the Animator Controller

        private AnimatorStateInfo _baseLayerInfo, _rightArmInfo, _leftArmInfo, _fullBodyInfo, _upperBodyInfo;
        private int _baseLayer
        { get { return blackboard.Animator.GetLayerIndex("Base Layer"); } }
        private int _rightArmLayer
        { get { return blackboard.Animator.GetLayerIndex("RightArm"); } }
        private int _leftArmLayer
        { get { return blackboard.Animator.GetLayerIndex("LeftArm"); } }
        private int _upperBodyLayer
        { get { return blackboard.Animator.GetLayerIndex("UpperBody"); } }
        private int _fullbodyLayer
        { get { return blackboard.Animator.GetLayerIndex("FullBody"); } }

        private const string groundedDirectional = "Grounded Directional", groundedStrafe = "Grounded Strafe";
             
        public void LayerControl()
        {
            _baseLayerInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(_baseLayer);
            _rightArmInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(_rightArmLayer);
            _leftArmInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(_leftArmLayer);
            _upperBodyInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(_upperBodyLayer);
            _fullBodyInfo = blackboard.Animator.GetCurrentAnimatorStateInfo(_fullbodyLayer);
        }
        

        // Is the Animator playing the grounded animations?
        public bool AnimationGrounded
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
            UpdateAnimatorParams(blackboard.Angle);
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





        #region Combat Animations
        protected virtual void PlayTargetAnimation()
        {

            string targetAnim;
            if (blackboard.ActionSlot != null && _fullBodyInfo.IsName("ResetState")) // we need to be in the empty state in order to transition
            {
                targetAnim = blackboard.ActionSlot.targetAnim;
                blackboard.Animator.Play(targetAnim);
            }

            else if (blackboard.ActionSlot == null && _fullBodyInfo.IsName("ResetState"))
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

        // This should be replaced by FinalIK
        private void EquipWeapon()
        {
            if (blackboard.CurrentWeapon == WeaponStatus.OneHanded && !blackboard.WeaponEquipped)
            {
                blackboard.Animator.CrossFade("Sword1h_Equip", 0.4f);
                blackboard.WeaponEquipped = true;
            }
                
            blackboard.Animator.SetFloat("IsTwoHanded", Mathf.Lerp(blackboard.Animator.GetFloat("IsTwoHanded"), (float)blackboard.CurrentWeapon, Time.deltaTime));
        }
        

        public virtual void PlayHurtAnimation(bool value)
        {
            blackboard.Animator.Play("Idle_Hit_Strong_Right");
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