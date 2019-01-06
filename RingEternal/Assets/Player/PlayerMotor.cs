
using RootMotion;
using UnityEngine;
using static Player.Utility;

/* Calculates the movement around the world and passes this onto the player animator
 */

namespace Player.PlayerController
{
    public class PlayerMotor : Character
    {



        [Header("References")]
        public PlayerBlackboard _blackboard;

        // Is the character always rotating to face the move direction or is he strafing?
        [System.Serializable]
        public enum MoveMode
        {
            Directional,
            Strafe
        }


        [Header("Movement")]
        public MoveMode moveMode; // Is the character always rotating to face the move direction or is he strafing?
        public bool runByDefault = true;
        public bool smoothPhysics = true; // If true, will use interpolation to smooth out the fixed time step.
        public float smoothAccelerationTime = 0.2f; // The smooth acceleration of the speed of the character (using Vector3.SmoothDamp)
        public float linearAccelerationSpeed = 3f; // The linear acceleration of the speed of the character (using Vector3.MoveTowards)
        public float platformFriction = 7f;                 // the acceleration of adapting the velocities of moving platforms
        public float groundStickyEffect = 4f;               // power of 'stick to ground' effect - prevents bumping down slopes.
        public float maxVerticalVelocityOnGround = 3f;      // the maximum y velocity while the character is grounded
        public float velocityToGroundTangentWeight = 0f;    // the weight of rotating character velocity vector to the ground tangent
        [Range(0,5)] public float wallDistance = 1f;
        public Transform headHeight;
        public Transform waistHeight;

        [Header("Rotation")]
        public bool lookInCameraDirection; // should the character be looking in the same direction that the camera is facing
        public float turnSpeed = 5f;                    // additional turn speed added when the player is moving (added to animation root rotation)
        public float stationaryTurnSpeedMlp = 1f;           // additional turn speed added when the player is stationary (added to animation root rotation)

        [Header("Falling")]
        public float airSpeed = 6f; // determines the max speed of the character while airborne
        public float airControl = 2f; // determines the response speed of controlling the character while airborne
      
        [Header("Wall Running")]

        [SerializeField] LayerMask wallRunLayers; // walkable vertical surfaces
        [SerializeField] LayerMask obstacleLayers;
        public float wallRunMaxLength = 1f;                 // max duration of a wallrun
        public float wallRunMinMoveMag = 0.6f;              // the minumum magnitude of the user control input move vector
        public float wallRunMinVelocityY = -1f;             // the minimum vertical velocity of doing a wall run
        public float wallRunRotationSpeed = 1.5f;           // the speed of rotating the character to the wall normal
        public float wallRunMaxRotationAngle = 70f;         // max angle of character rotation
        public float wallRunWeightSpeed = 5f;               // the speed of blending in/out the wall running effect

        [Header("Crouching")]
        public float crouchCapsuleScaleMlp = 0.6f;          // the capsule collider scale multiplier while crouching

        public bool onGround { get; private set; }
        public AnimState animState = new AnimState();

        protected Vector3 moveDirection; // The current move direction of the character in Strafe move mode
    

        private Vector3 normal, platformVelocity, platformAngularVelocity;
        private RaycastHit hit;
        private float jumpLeg, jumpEndTime, forwardMlp, groundDistance, lastAirTime, stickyForce;
        private Vector3 wallNormal = Vector3.up;
        private Vector3 moveDirectionVelocity;
        private float wallRunWeight;
        private float lastWallRunWeight;
        private bool fixedFrame;
        private float wallRunEndTime;
        private Vector3 gravity;
        private Vector3 verticalVelocity;
        private float velocityY;


        protected override void Awake()
        {
            base.Awake();
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            
            wallNormal = -gravity.normalized;
            onGround = true;
            
            animState.onGround = true;
            _blackboard.runByDefault = runByDefault;
        }
        
        void FixedUpdate()
        {
            gravity = GetGravity();

            verticalVelocity = V3Tools.ExtractVertical(_rigidbody.velocity, gravity, 1f);
            velocityY = verticalVelocity.magnitude;
            if (Vector3.Dot(verticalVelocity, gravity) > 0f) velocityY = -velocityY;

            // Smoothing out the fixed time step
            _rigidbody.interpolation = smoothPhysics ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
       
            MoveFixed(_blackboard.fixedDeltaPosition);

            //blackboard.fixedDeltaPosition = Vector3.zero;

            //_rigidbody.MoveRotation(transform.rotation * blackboard.fixedDeltaRotation);
            //blackboard.fixedDeltaRotation = Quaternion.identity;

            Rotate();

            GroundCheck(); // detect and stick to ground

            // Friction
            //if (_blackboard.input == Vector2.zero && groundDistance < airborneThreshold * 0.5f) HighFriction();
            //else ZeroFriction();

            bool stopSlide = onGround && groundDistance < airborneThreshold * 0.5f;

            // Individual gravity
            if (gravityTarget != null)
            {
                _rigidbody.useGravity = false;

                if (!stopSlide) _rigidbody.AddForce(gravity);
            }

            if (stopSlide)
            {
                //_rigidbody.useGravity = false;
                _rigidbody.velocity = Vector3.zero;
            }
            else if (gravityTarget == null) _rigidbody.useGravity = true;

            if (!onGround)
            {
                _rigidbody.AddForce(gravity * gravityMultiplier);
            }
            // Scale the capsule colllider while crouching
            //ScaleCapsule(blackboard.isCrouching ? crouchCapsuleScaleMlp : 1f);

            fixedFrame = true;

        }

        protected virtual void Update()
        {
            // Fill in animState
            animState.onGround = onGround;
            animState.moveDirection = GetMoveDirection();
            animState.crouch = _blackboard.isCrouching;
            animState.isStrafing = moveMode == MoveMode.Strafe;
            animState.vertical = CheckForObstacle() ? 0 : _blackboard.input.y;
            animState.horizontal = CheckForObstacle() ? 0 : _blackboard.input.x;
            _blackboard.animState = animState;

        }

        protected virtual void LateUpdate()
        {

            if (!fixedFrame && _rigidbody.interpolation == RigidbodyInterpolation.None) return;

            fixedFrame = false;
        }

        public virtual void RotateToTarget(Transform target)
        {
            if (target)
            {
                Quaternion rot = Quaternion.LookRotation(target.position - transform.position);
                var newPos = new Vector3(transform.eulerAngles.x, rot.eulerAngles.y, transform.eulerAngles.z);
                _blackboard.targetRotation = Quaternion.Euler(newPos);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(newPos), Time.deltaTime * turnSpeed);
            }
        }

        public bool CheckForObstacle()
        {
            RaycastHit hit;
            if (Physics.Raycast(headHeight.position, transform.forward, out hit, wallDistance, obstacleLayers))
            {
                Debug.Log("Hit " + hit.collider.name);
                return true;

            }
            // Can be later used for vaulting if legs are being stopped but the head has clearance
            if (Physics.Raycast(waistHeight.position, transform.forward, out hit, wallDistance, obstacleLayers))
            {
                Debug.Log("Hit " + hit.collider.name);
                return true;

            }
            return false;
        }
        public virtual void RotateWithAnotherTransform(Transform referenceTransform)
        {
            var newRotation = new Vector3(transform.eulerAngles.x, referenceTransform.eulerAngles.y, transform.eulerAngles.z);
            _blackboard.targetRotation = transform.rotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(newRotation), Time.deltaTime * turnSpeed); //TODO: Check if this is too slow
            Debug.Log("Using method within motor");
        }

        private void MoveFixed(Vector3 deltaPosition)
        {
            // Process horizontal wall-running
            WallRun();

            Vector3 velocity = deltaPosition / Time.deltaTime;

            // Add velocity of the rigidbody the character is standing on
            velocity += V3Tools.ExtractHorizontal(platformVelocity, gravity, 1f);

            if (onGround)
            {
                // Rotate velocity to ground tangent
                if (velocityToGroundTangentWeight > 0f)
                {
                    Quaternion rotation = Quaternion.FromToRotation(transform.up, normal);
                    velocity = Quaternion.Lerp(Quaternion.identity, rotation, velocityToGroundTangentWeight) * velocity;
                }
            }
            else
            {
                // Air move
                Vector3 airMove = V3Tools.ExtractHorizontal(_blackboard.input * airSpeed, gravity, 1f);
                velocity = Vector3.Lerp(_rigidbody.velocity, airMove, Time.deltaTime * airControl);
            }

            if (onGround && Time.time > jumpEndTime)
            {
                _rigidbody.velocity = _rigidbody.velocity - transform.up * stickyForce * Time.deltaTime;
            }

            // Vertical velocity
            Vector3 verticalVelocity = V3Tools.ExtractVertical(_rigidbody.velocity, gravity, 1f);
            Vector3 horizontalVelocity = V3Tools.ExtractHorizontal(velocity, gravity, 1f);

            if (onGround)
            {
                if (Vector3.Dot(verticalVelocity, gravity) < 0f)
                {
                    verticalVelocity = Vector3.ClampMagnitude(verticalVelocity, maxVerticalVelocityOnGround);
                }
            }

            //_rigidbody.velocity = horizontalVelocity + verticalVelocity;

            // Dampering forward speed on the slopes
            float slopeDamper = !onGround ? 1f : GetSlopeDamper(-deltaPosition / Time.deltaTime, normal);
            forwardMlp = Mathf.Lerp(forwardMlp, slopeDamper, Time.deltaTime * 5f);
        }

        // Processing horizontal wall running
        private void WallRun()
        {
            bool canWallRun = CanWallRun();

            // Remove flickering in and out of wall-running
            if (wallRunWeight > 0f && !canWallRun) wallRunEndTime = Time.time;
            if (Time.time < wallRunEndTime + 0.5f) canWallRun = false;

            wallRunWeight = Mathf.MoveTowards(wallRunWeight, (canWallRun ? 1f : 0f), Time.deltaTime * wallRunWeightSpeed);

            if (wallRunWeight <= 0f)
            {
                // Reset
                if (lastWallRunWeight > 0f)
                {
                    Vector3 frw = V3Tools.ExtractHorizontal(transform.forward, gravity, 1f);
                    transform.rotation = Quaternion.LookRotation(frw, -gravity);
                    wallNormal = -gravity.normalized;
                }
            }

            lastWallRunWeight = wallRunWeight;

            if (wallRunWeight <= 0f) return;

            // Make sure the character won't fall down
            if (onGround && velocityY < 0f) _rigidbody.velocity = V3Tools.ExtractHorizontal(_rigidbody.velocity, gravity, 1f);

            // transform.forward flattened
            Vector3 f = V3Tools.ExtractHorizontal(transform.forward, gravity, 1f);

            // Raycasting to find a walkable wall
            RaycastHit velocityHit = new RaycastHit();
            velocityHit.normal = -gravity.normalized;
            //Physics.Raycast(onGround ? transform.position : collider.bounds.center, f, out velocityHit, 3f, wallRunLayers);

            // Finding the normal to rotate to
            wallNormal = Vector3.Lerp(wallNormal, velocityHit.normal, Time.deltaTime * wallRunRotationSpeed);

            // Clamping wall normal to max rotation angle
            wallNormal = Vector3.RotateTowards(-gravity.normalized, wallNormal, wallRunMaxRotationAngle * Mathf.Deg2Rad, 0f);

            // Get transform.forward ortho-normalized to the wall normal
            Vector3 fW = transform.forward;
            Vector3 nW = wallNormal;
            Vector3.OrthoNormalize(ref nW, ref fW);

            // Rotate from upright to wall normal
            transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(f, -gravity), Quaternion.LookRotation(fW, wallNormal), wallRunWeight);
        }

        // Should the character be enabled to do a wall run?
        private bool CanWallRun()
        {
            if (Time.time < jumpEndTime - 0.1f) return false;
            if (Time.time > jumpEndTime - 0.1f + wallRunMaxLength) return false;
            if (velocityY < wallRunMinVelocityY) return false;
            if (_blackboard.input.magnitude < wallRunMinMoveMag) return false;
            return true;
        }

        // Get the move direction of the character relative to the character rotation
        private Vector3 GetMoveDirection()
        {
            switch (moveMode)
            {
                case MoveMode.Directional:
                    moveDirection = Vector3.SmoothDamp(moveDirection, new Vector3(0f, 0f, _blackboard.input.magnitude), ref moveDirectionVelocity, smoothAccelerationTime);
                    moveDirection = Vector3.MoveTowards(moveDirection, new Vector3(0f, 0f, _blackboard.input.magnitude), Time.deltaTime * linearAccelerationSpeed);
                    return moveDirection * forwardMlp;
                case MoveMode.Strafe:
                    moveDirection = Vector3.SmoothDamp(moveDirection, _blackboard.input, ref moveDirectionVelocity, smoothAccelerationTime);
                    moveDirection = Vector3.MoveTowards(moveDirection, _blackboard.input, Time.deltaTime * linearAccelerationSpeed);
                    return transform.InverseTransformDirection(moveDirection);
            }
            return Vector3.zero;
        }

        // Rotate the character
        protected virtual void Rotate()
        {
                if (gravityTarget != null)
                {
                    Debug.Log("Gravity target != null");
                    _rigidbody.MoveRotation(Quaternion.FromToRotation(transform.up, transform.position - gravityTarget.position) * transform.rotation);

                }
                if (platformAngularVelocity != Vector3.zero) {
                Debug.Log("Platform angular velocity != zero");
                    _rigidbody.MoveRotation(Quaternion.Euler(platformAngularVelocity) * transform.rotation);

            }

            if (_blackboard.lookInCameraDirection && _blackboard.lookPos && _blackboard.input.sqrMagnitude > 0.1f)
                {
                    Debug.Log("Rotate with camera");
                    RotateWithAnotherTransform(_blackboard.lookPos);
                }
                else
                {
                Debug.Log("Moving");
                    float angle = GetAngleFromForward(GetForwardDirection());

                    //if (blackboard.input == Vector2.zero)
                    //    angle *= (1.01f - (Mathf.Abs(angle) / 180f)) * stationaryTurnSpeedMlp;

                    // Rotating the character
                     _rigidbody.MoveRotation(Quaternion.AngleAxis(angle * Time.deltaTime * turnSpeed, transform.up) * _rigidbody.rotation);
                
            }
            
        }

        // Which way to look at?
        private Vector3 GetForwardDirection()
        {
            bool isMoving = _blackboard.input != Vector2.zero;

            switch (moveMode)
            {
                case MoveMode.Directional:
                    Vector3 forwardVector;
                    forwardVector = new Vector3(_blackboard.input.x, 0, _blackboard.input.y);
                    return forwardVector;
                      
                case MoveMode.Strafe:
                    if (isMoving)
                        return _blackboard.lookPos.position - _rigidbody.position;
                    return lookInCameraDirection ? _blackboard.lookPos.position - _rigidbody.position : transform.forward;
            }

            return Vector3.zero;
        }
        

        // Is the character grounded?
        private void GroundCheck()
        {
            Vector3 platformVelocityTarget = Vector3.zero;
            platformAngularVelocity = Vector3.zero;
            float stickyForceTarget = 0f;

            // Spherecasting
            hit = GetSpherecastHit();
            //normal = hit.normal;
            normal = transform.up;
            //groundDistance = r.position.y - hit.point.y;
            groundDistance = Vector3.Project(_rigidbody.position - hit.point, transform.up).magnitude;

            // if not jumping...
            bool findGround = true; //  Time.time > jumpEndTime;

            if (findGround)
            {
                bool g = onGround;
                onGround = false;

                // The distance of considering the character grounded
                float groundHeight = !g ? airborneThreshold * 0.5f : airborneThreshold;
             
                //Vector3 horizontalVelocity = r.velocity;
                Vector3 horizontalVelocity = V3Tools.ExtractHorizontal(_rigidbody.velocity, gravity, 1f);

                float velocityF = horizontalVelocity.magnitude;
                
                if (groundDistance < groundHeight)
                {
                    // Force the character on the ground
                    stickyForceTarget = groundStickyEffect * velocityF * groundHeight;

                    // On moving platforms
                    if (hit.rigidbody != null)
                    {
                        platformVelocityTarget = hit.rigidbody.GetPointVelocity(hit.point);
                        platformAngularVelocity = Vector3.Project(hit.rigidbody.angularVelocity, transform.up);
                    }

                    // Flag the character grounded
                    onGround = true;
                }
            }

            // Interpolate the additive velocity of the platform the character might be standing on
            platformVelocity = Vector3.Lerp(platformVelocity, platformVelocityTarget, Time.deltaTime * platformFriction);

            stickyForce = Mathf.Lerp(stickyForce, stickyForceTarget, Time.deltaTime * 5f);

            //// remember when we were last in air, for jump delay
            if (!onGround) lastAirTime = Time.time;
        }


        private void OnDrawGizmos()
        {
            Gizmos.DrawRay(headHeight.position, transform.forward * wallDistance);
            Gizmos.DrawRay(waistHeight.position, transform.forward * wallDistance);

        }
    }



}


