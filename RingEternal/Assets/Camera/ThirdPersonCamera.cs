using com.ootii.Cameras;
using com.ootii.Geometry;
using System.Collections;
using UnityEngine;

namespace Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        private static ThirdPersonCamera _instance;
      

        public static ThirdPersonCamera instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<ThirdPersonCamera>();

                    //Tell unity not to destroy this object when loading a new scene!
                    //DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        [Header("References")]
        [SerializeField] PlayerBlackboard blackboard;

        public Transform target;

        [Tooltip("Lerp speed between Camera States")]
        public float smoothBetweenState = 6f;

        public float smoothCameraRotation = 6f;
        public float scrollSpeed = 10f;

        [Tooltip("What layer will be culled")]
        public LayerMask cullingLayer = 1 << 0;

        [Tooltip("Change this value If the camera pass through the wall")]
        public float clipPlaneMargin;

        public float checkHeightRadius;
        public bool showGizmos;
        public bool startUsingTargetRotation = true;
        public bool startSmooth = false;

        [Tooltip("Debug purposes, lock the camera behind the character for better align the states")]
        public bool lockCamera;

        public Vector2 offsetMouse;


        CameraTransform camTrans;

        #region hide properties

        //[HideInInspector]
        public int indexList, indexLookPoint;

        //[HideInInspector]
        [Range(0, 10f)] public float offSetPlayerPivot;

        //[HideInInspector]
        public float distance = 5f;

        //[HideInInspector]
        public string currentStateName;

        //[HideInInspector]
        public Transform currentTarget;

        //[HideInInspector]
        public ThirdPersonCameraState currentState;

        //[HideInInspector]
        public ThirdPersonCameraListData CameraStateList;

        //[HideInInspector]
        public Transform lockTarget;

        //[HideInInspector]
        public Vector2 movementSpeed;

        //[HideInInspector]
        public ThirdPersonCameraState transitionState;

        private Transform targetLookAt;
        private Vector3 currentTargetPos;
        private Vector3 lookPoint;
        private Vector3 current_cPos;
        private Vector3 desired_cPos;
        private Vector3 lookTargetAdjust;
        private Camera _camera;
        private float mouseY = 0f;
        private float mouseX = 0f;
        private float currentHeight;
        private float currentZoom;
        private float cullingHeight;
        private float switchRight, currentSwitchRight;
        private float heightOffset;
        private bool isInit;
        private bool useSmooth;
        private bool isNewTarget;
        private bool firstStateIsInit;
        private Quaternion fixedRotation;
        public bool lockCursor = true; // If true, the mouse will be locked to screen center and hidden
        public LayerMask ignoreLayers;

        public float CullingDistance { get; private set; }

        #endregion hide properties
        private bool hitSomething = false;
        [SerializeField] float collisionRecoverSpeed = 5f;
        private void OnDrawGizmos()
        {
            if (showGizmos)
            {
                if (currentTarget)
                {
                    var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot, currentTarget.position.z);
                    Gizmos.DrawWireSphere(targetPos + Vector3.up * cullingHeight, checkHeightRadius);
                    Gizmos.DrawLine(targetPos, targetPos + Vector3.up * cullingHeight);
                }
            }
        }
        
        private void Start()
        {
            Init();
        }

        public void Init()
        {
            if (target == null)
                return;
            _camera = GetComponentInChildren<Camera>();
            currentTarget = target;
            switchRight = 1f;
            currentSwitchRight = 1f;
            distance = Vector3.Distance(transform.position, currentTarget.position);
            ChangeState("Default", startSmooth);
            currentZoom = currentState.defaultDistance;
            currentHeight = currentState.height;

            // Create a gameObject, hide it in the heirachy, and set it's position and rotation to the currenttarget, i.e. the player
            if (!targetLookAt)
                targetLookAt = new GameObject("targetLookAt").transform;
            currentTargetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot, currentTarget.position.z) + currentTarget.transform.up * transitionState.height;
            targetLookAt.position = currentTargetPos;
            targetLookAt.rotation = transform.rotation;
            //targetLookAt.hideFlags = HideFlags.HideInHierarchy;

            if (startUsingTargetRotation)
            {
                mouseY = currentTarget.root.eulerAngles.x;
                mouseX = currentTarget.root.eulerAngles.y;
            }
            else
            {
                mouseY = transform.root.eulerAngles.x;
                mouseX = transform.root.eulerAngles.y;
            }
           
            isInit = true;
        }

        // Keep checking what camera mode we are in (freedirection, fixed angle, fixed point
        protected virtual void FixedUpdate()
        {
            if (target == null || targetLookAt == null || currentState == null || transitionState == null || !isInit) return;

            // Cursors
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = lockCursor ? false : true;

            switch (currentState.cameraMode)
            {
                case TPCameraMode.FreeDirectional:
                    CameraMovement();
                    break;

                case TPCameraMode.FixedAngle:
                    CameraMovement();
                    break;

                case TPCameraMode.FixedPoint:
                    CameraFixed();
                    break;
            }
        }

        public void SetLockTarget(Transform _lockTarget, float heightOffset)
        {
            if (lockTarget != null && lockTarget == _lockTarget) return;
            isNewTarget = _lockTarget != lockTarget;
            lockTarget = _lockTarget;
            this.heightOffset = heightOffset;
        }

        public void SetLockTarget(Transform _lockTarget)
        {
            lockTarget = _lockTarget;
        }

        public void ClearLockOnTarget()
        {
            lockTarget = null;
        }
        public void RemoveLockTarget()
        {
            lockTarget = null;
        }

        /// <summary>
        /// Set the target for the camera
        /// </summary>
        /// <param name="New cursorObject"></param>
        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget ? newTarget : target;
        }

        public void SetMainTarget(Transform newTarget)
        {
            target = newTarget;
            currentTarget = newTarget;
            if (!isInit) Init();
        }

        /// <summary>
        /// Convert a point in the screen in a Ray for the world
        /// </summary>
        /// <param name="Point"></param>
        /// <returns></returns>
        public Ray ScreenPointToRay(Vector3 Point)
        {
            return this.GetComponent<Camera>().ScreenPointToRay(Point);
        }

        /// <summary>
        /// Change CameraState
        /// </summary>
        /// <param name="desiredStateName"></param>
        /// <param name="Use smooth"></param>
        public void ChangeState(string desiredStateName, bool hasSmooth)
        {
            useSmooth = (!firstStateIsInit || currentState != null && !currentState.Name.Equals(desiredStateName) || isInit) ? startSmooth : hasSmooth;
            // search for the camera state string name
            ThirdPersonCameraState state = CameraStateList != null ? CameraStateList.tpCameraStates.Find(obj => obj.Name.Equals(desiredStateName)) : new ThirdPersonCameraState("Default");

            //If we find the state in the list, then determine if we need to smooth and then set the current state to it.
            if (state != null)
            {
                currentStateName = desiredStateName;
                currentState.cameraMode = state.cameraMode;
                transitionState = state; // set the state of transition (lerpstate) to the state found on the list
                if (!firstStateIsInit)
                {
                    currentState.defaultDistance = Vector3.Distance(targetLookAt.position, transform.position);
                    currentState.height = state.height;
                    currentState.fov = state.fov;
                    StartCoroutine(ResetFirstState()); // sets firstStateIsInit to true at the end of the frame
                }
                // in case there is no smooth, a copy will be made without the transition values
                if (currentState != null && !useSmooth)
                    currentState.CopyState(state);
            }
            else
            {
                // if we can't find the chosen state, the first state will be set up as default
                if (CameraStateList != null && CameraStateList.tpCameraStates.Count > 0)
                {
                    state = CameraStateList.tpCameraStates[0];
                    currentStateName = state.Name;
                    currentState.cameraMode = state.cameraMode;
                    transitionState = state;

                    if (currentState != null && !useSmooth)
                        currentState.CopyState(state);
                }
            }
            // in case a list of states does not exist, a default state will be created
            if (currentState == null)
            {
                currentState = new ThirdPersonCameraState("Null");
                currentStateName = currentState.Name;
            }
            if (CameraStateList != null)
                indexList = CameraStateList.tpCameraStates.IndexOf(state);
            currentZoom = state.defaultDistance;

            if (currentState.cameraMode == TPCameraMode.FixedAngle)
            {
                mouseX = currentState.fixedAngle.x;
                mouseY = currentState.fixedAngle.y;
            }

            currentState.fixedAngle = new Vector3(mouseX, mouseY);
            indexLookPoint = 0;
        }

        /// <summary>
        /// Change State using look at point if the cameraMode is FixedPoint
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="pointName"></param>
        /// <param name="hasSmooth"></param>
        public void ChangeState(string stateName, string pointName, bool hasSmooth)
        {
            useSmooth = hasSmooth;
            if (!currentState.Name.Equals(stateName))
            {
                // search for the camera state string name
                var state = CameraStateList.tpCameraStates.Find(obj => obj.Name.Equals(stateName));

                if (state != null)
                {
                    currentStateName = stateName;
                    currentState.cameraMode = state.cameraMode;
                    transitionState = state; // set the state of transition (lerpstate) to the state found on the list
                                             // in case there is no smooth, a copy will be make without the transition values
                    if (currentState != null && !hasSmooth)
                        currentState.CopyState(state);
                }
                else
                {
                    // if the state choosed if not real, the first state will be set up as default
                    if (CameraStateList.tpCameraStates.Count > 0)
                    {
                        state = CameraStateList.tpCameraStates[0];
                        currentStateName = state.Name;
                        currentState.cameraMode = state.cameraMode;
                        transitionState = state;
                        if (currentState != null && !hasSmooth)
                            currentState.CopyState(state);
                    }
                }
                // in case a list of states does not exist, a default state will be created
                if (currentState == null)
                {
                    currentState = new ThirdPersonCameraState("Null");
                    currentStateName = currentState.Name;
                }

                indexList = CameraStateList.tpCameraStates.IndexOf(state);
                currentZoom = state.defaultDistance;
                currentState.fixedAngle = new Vector3(mouseX, mouseY);
                indexLookPoint = 0;
            }

            if (currentState.cameraMode == TPCameraMode.FixedPoint)
            {
                var point = currentState.lookPoints.Find(obj => obj.pointName.Equals(pointName));
                if (point != null)
                {
                    indexLookPoint = currentState.lookPoints.IndexOf(point);
                }
                else
                {
                    indexLookPoint = 0;
                }
            }
        }

        private IEnumerator ResetFirstState()
        {
            yield return new WaitForEndOfFrame();
            firstStateIsInit = true;
        }

        /// <summary>
        /// Change the lookAtPoint of current state if cameraMode is FixedPoint
        /// </summary>
        /// <param name="pointName"></param>
        public void ChangePoint(string pointName)
        {
            if (currentState == null || currentState.cameraMode != TPCameraMode.FixedPoint || currentState.lookPoints == null) return;
            var point = currentState.lookPoints.Find(delegate (LookPoint obj) { return obj.pointName.Equals(pointName); });
            if (point != null) indexLookPoint = currentState.lookPoints.IndexOf(point); else indexLookPoint = 0;
        }

        /// <summary>
        /// Zoom baheviour
        /// </summary>
        /// <param name="scrollValue"></param>
        /// <param name="zoomSpeed"></param>
        public void Zoom(float scrollValue)
        {
            currentZoom -= scrollValue * scrollSpeed;
        }

        /// <summary>
        /// Camera Rotation behaviour
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RotateCamera(float x, float y)
        {
            if(Mathf.Abs(x) > 0 || Mathf.Abs(y) > 0)
            {
                Debug.Log("Look in camera direction being called from Rotate Camera");
                //blackboard.lookInCameraDirection = true;
            }
            else
            {
                //blackboard.lookInCameraDirection = false;
            }
            if (currentState.cameraMode.Equals(TPCameraMode.FixedPoint)) return;
            if (!currentState.cameraMode.Equals(TPCameraMode.FixedAngle))
            {
                // lock into a target
                if (!lockTarget)
                {
                    // free rotation
                    mouseX += x * currentState.xMouseSensitivity;
                    mouseY -= y * currentState.yMouseSensitivity;

                    movementSpeed.x = x;
                    movementSpeed.y = -y;
                    if (!lockCamera)
                    {
                        // Ensure angle is between 360 and -360
                        mouseY = Utility.ClampAngle(mouseY, transitionState.yMinLimit, transitionState.yMaxLimit);
                        mouseX = Utility.ClampAngle(mouseX, transitionState.xMinLimit, transitionState.xMaxLimit);
                    }
                    else
                    {
                        /// Normalized the angle. between -180 and 180 degrees
                        mouseY = currentTarget.root.eulerAngles.NormalizeAngle().x;
                        mouseX = currentTarget.root.eulerAngles.NormalizeAngle().y;
                    }
                }
            }
            else
            {
                // free directional
                var _x = transitionState.fixedAngle.x;
                var _y = transitionState.fixedAngle.y;
                mouseX = useSmooth ? Mathf.LerpAngle(mouseX, _x, smoothBetweenState * Time.deltaTime * scrollSpeed) : _x;
                mouseY = useSmooth ? Mathf.LerpAngle(mouseY, _y, smoothBetweenState * Time.deltaTime * scrollSpeed) : _y;
            }
        }

        /// <summary>
        /// Switch Camera Right
        /// </summary>
        /// <param name="value"></param>
        public void SwitchRight(bool value = false)
        {
            switchRight = value ? -1 : 1;
        }

        private void CalculateLockOnPoint()
        {
            if (currentState.cameraMode.Equals(TPCameraMode.FixedAngle) && lockTarget) return;   // check if angle of camera is fixed
            var collider = lockTarget.GetComponent<Collider>();                                  // collider to get center of bounds

            if (collider == null)
            {
                return;
            }

            var _point = collider.bounds.center;
            Vector3 relativePos = _point - (desired_cPos);                      // get position relative to transform
            Quaternion rotation = Quaternion.LookRotation(relativePos);         // convert to rotation

            //convert angle (360 to 180)
            var y = 0f;
            var x = rotation.eulerAngles.y;
            if (rotation.eulerAngles.x < -180)
                y = rotation.eulerAngles.x + 360;
            else if (rotation.eulerAngles.x > 180)
                y = rotation.eulerAngles.x - 360;
            else
                y = rotation.eulerAngles.x;

            mouseY = Utility.ClampAngle(y, currentState.yMinLimit, currentState.yMaxLimit);
            mouseX = Utility.ClampAngle(x, currentState.xMinLimit, currentState.xMaxLimit);
        }


        protected virtual void CameraCollisions(float targetZ, ref float actualZ)
        {
            float step = Mathf.Abs(targetZ);
            int stepCount = 2;
            float stepIncremental = step / stepCount;
            Transform camTrans = transform;
            RaycastHit hit;
            Vector3 origin = currentTargetPos;
            Vector3 direction = -currentTargetPos;
            Debug.DrawRay(origin, direction * step, Color.blue);

            if (Physics.Raycast(origin, direction, out hit, step, ignoreLayers))
            {
                float distance = Vector3.Distance(hit.point, origin);
                actualZ = -(distance / 2);
            }
            else
            {
                for (int s = 0; s < stepCount + 1; s++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 dir = Vector3.zero;
                        Vector3 secondOrigin = origin + (direction * s) * stepIncremental;

                        switch (i)
                        {
                            case 0:
                                dir = camTrans.right;
                                break;
                            case 1:
                                dir = -camTrans.right;
                                break;
                            case 2:
                                dir = camTrans.up;
                                break;
                            case 3:
                                dir = -camTrans.up;
                                break;
                            default:
                                break;
                        }

                        Debug.DrawRay(secondOrigin, dir * 1, Color.red);
                        if (Physics.Raycast(secondOrigin, dir, out hit, 1, ignoreLayers))
                        {
                            float distance = Vector3.Distance(secondOrigin, origin);
                            actualZ = -(distance / 2);
                        }
                    }
                }
            }
        }

        

       
        private void CameraMovement()
        {
            if (currentTarget == null || _camera == null)
                return;

            if (useSmooth)
            {
                currentState.Slerp(transitionState, smoothBetweenState * Time.deltaTime);
            }
            else
                currentState.CopyState(transitionState);

            if (currentState.useZoom)
            {
                currentZoom = Mathf.Clamp(currentZoom, currentState.minDistance, currentState.maxDistance);
                distance = useSmooth ? Mathf.Lerp(distance, currentZoom, transitionState.smoothFollow * Time.deltaTime) : currentZoom;
            }
            else
            {
                distance = useSmooth ? Mathf.Lerp(distance, currentState.defaultDistance, transitionState.smoothFollow * Time.deltaTime) : currentState.defaultDistance;
                currentZoom = currentState.defaultDistance;
            }

            _camera.fieldOfView = currentState.fov;
            //cullingDistance = Mathf.Lerp(cullingDistance, currentZoom, smoothBetweenState * Time.deltaTime);
            currentSwitchRight = Mathf.Lerp(currentSwitchRight, switchRight, smoothBetweenState * Time.deltaTime);
            var camDir = (currentState.forward * -targetLookAt.forward) + ((currentState.right * currentSwitchRight) * targetLookAt.right);

           

            camDir = camDir.normalized;
            var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y, currentTarget.position.z) + currentTarget.transform.up * offSetPlayerPivot;
            currentTargetPos = targetPos;
            desired_cPos = targetPos + currentTarget.transform.up * currentState.height;
            current_cPos = targetPos + currentTarget.transform.up * currentHeight;
            #region Clipping
            RaycastHit hitInfo;

            Utility.ClipPlanePoints planePoints = _camera.NearClipPlanePoints(current_cPos + (camDir * (distance)), clipPlaneMargin);
            Utility.ClipPlanePoints oldPoints = _camera.NearClipPlanePoints(desired_cPos + (camDir * currentZoom), clipPlaneMargin);
            //Check if Height is not blocked
            if (Physics.SphereCast(targetPos, checkHeightRadius, currentTarget.transform.up, out hitInfo, currentState.cullingHeight + 0.2f, cullingLayer))
            {
                hitSomething = true;
                var t = hitInfo.distance - 0.2f;
                t -= currentState.height;
                t /= (currentState.cullingHeight - currentState.height);
                cullingHeight = Mathf.Lerp(currentState.height, currentState.cullingHeight, Mathf.Clamp(t, 0.0f, 1.0f));
            }
            else
            {
                cullingHeight = useSmooth ? Mathf.Lerp(cullingHeight, currentState.cullingHeight, smoothBetweenState * Time.deltaTime) : currentState.cullingHeight;
            }
            //Check if desired target position is not blocked
            if (CullingRayCast(desired_cPos, oldPoints, out hitInfo, currentZoom + 0.2f, cullingLayer, Color.blue))
            {
                distance = hitInfo.distance - 0.2f;
                if (distance < currentState.defaultDistance)
                {
                    var t = hitInfo.distance;
                    t -= currentState.cullingMinDist;
                    t /= (currentZoom - currentState.cullingMinDist);
                    currentHeight = Mathf.Lerp(cullingHeight, currentState.height, Mathf.Clamp(t, 0.0f, 1.0f));
                    current_cPos = targetPos + currentTarget.transform.up * currentHeight ;
                    hitSomething = true;
                }
            }
            else
            {
                currentHeight = useSmooth ? Mathf.Lerp(currentHeight, currentState.height, smoothBetweenState * Time.deltaTime) : currentState.height;
            }
            //Check if target position with culling height applied is not blocked
            if (CullingRayCast(current_cPos, planePoints, out hitInfo, distance, cullingLayer, Color.cyan)) distance = Mathf.Clamp(CullingDistance, 0.0f, currentState.defaultDistance);

            #endregion
            var lookPoint = current_cPos + targetLookAt.forward * 2f;
            lookPoint += (targetLookAt.right * Vector3.Dot(camDir * (distance), targetLookAt.right));
           
            targetLookAt.position = current_cPos;
       
            Quaternion newRot = Quaternion.Euler(mouseY + offsetMouse.y, mouseX + offsetMouse.x, 0);
            targetLookAt.rotation = useSmooth ? Quaternion.Lerp(targetLookAt.rotation, newRot, smoothCameraRotation * Time.deltaTime) : newRot;

            if (hitSomething)
            {

                transform.position = Vector3.Lerp(transform.position, current_cPos + (camDir * (distance)), Time.deltaTime * collisionRecoverSpeed);
                hitSomething = false;
            }
            else
            {

                transform.position = current_cPos + (camDir * (distance));
            }
            var rotation = Quaternion.LookRotation((lookPoint) - transform.position);

            if (blackboard.LockOnPressed)
                SetLockTarget(blackboard.LockTarget);
            else
                ClearLockOnTarget();

            if (lockTarget)
            {
                CalculateLockOnPoint();

                if (!(currentState.cameraMode.Equals(TPCameraMode.FixedAngle)))
                {
                    var collider = lockTarget.GetComponent<Collider>();
                    if (collider != null)
                    {
                        var point = (collider.bounds.center + Vector3.up * heightOffset) - transform.position;
                        var euler = Quaternion.LookRotation(point).eulerAngles - rotation.eulerAngles;
                        if (isNewTarget)
                        {
                            lookTargetAdjust.x = Mathf.LerpAngle(lookTargetAdjust.x, euler.x, currentState.smoothFollow * Time.deltaTime);
                            lookTargetAdjust.y = Mathf.LerpAngle(lookTargetAdjust.y, euler.y, currentState.smoothFollow * Time.deltaTime);
                            lookTargetAdjust.z = Mathf.LerpAngle(lookTargetAdjust.z, euler.z, currentState.smoothFollow * Time.deltaTime);
                            // Quaternion.LerpUnclamped(lookTargetAdjust, Quaternion.Euler(euler), currentState.smoothFollow * Time.deltaTime);
                            if (Vector3.Distance(lookTargetAdjust, euler) < .5f)
                                isNewTarget = false;
                        }
                        else
                            lookTargetAdjust = euler;
                    }
                }
            }
            else
            {
                lookTargetAdjust.x = Mathf.LerpAngle(lookTargetAdjust.x, 0, currentState.smoothFollow * Time.deltaTime);
                lookTargetAdjust.y = Mathf.LerpAngle(lookTargetAdjust.y, 0, currentState.smoothFollow * Time.deltaTime);
                lookTargetAdjust.z = Mathf.LerpAngle(lookTargetAdjust.z, 0, currentState.smoothFollow * Time.deltaTime);
                //lookTargetAdjust = Quaternion.LerpUnclamped(lookTargetAdjust, Quaternion.Euler(Vector3.zero), 1 * Time.deltaTime);
            }
            var _euler = rotation.eulerAngles + lookTargetAdjust;
            _euler.z = 0;
            var _rot = Quaternion.Euler(_euler + currentState.rotationOffSet);
            transform.rotation = _rot;
            movementSpeed = Vector2.zero;
        }
        

        private void CameraFixed()
        {
            if (useSmooth) currentState.Slerp(transitionState, smoothBetweenState);
            else currentState.CopyState(transitionState);

            var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot + currentState.height, currentTarget.position.z);
            currentTargetPos = useSmooth ? Vector3.MoveTowards(currentTargetPos, targetPos, currentState.smoothFollow * Time.deltaTime) : targetPos;
            current_cPos = currentTargetPos;
            var pos = isValidFixedPoint ? currentState.lookPoints[indexLookPoint].positionPoint : transform.position;
            transform.position = useSmooth ? Vector3.Lerp(transform.position, pos, currentState.smoothFollow * Time.deltaTime) : pos;
            targetLookAt.position = current_cPos;
            if (isValidFixedPoint && currentState.lookPoints[indexLookPoint].freeRotation)
            {
                var rot = Quaternion.Euler(currentState.lookPoints[indexLookPoint].eulerAngle);
                transform.rotation = useSmooth ? Quaternion.Slerp(transform.rotation, rot, (currentState.smoothFollow * 0.5f) * Time.deltaTime) : rot;
            }
            else if (isValidFixedPoint)
            {
                var rot = Quaternion.LookRotation(currentTargetPos - transform.position);
                transform.rotation = useSmooth ? Quaternion.Slerp(transform.rotation, rot, (currentState.smoothFollow) * Time.deltaTime) : rot;
            }
            _camera.fieldOfView = currentState.fov;
        }

        private bool isValidFixedPoint
        {
            get
            {
                return (currentState.lookPoints != null && currentState.cameraMode.Equals(TPCameraMode.FixedPoint) && (indexLookPoint < currentState.lookPoints.Count || currentState.lookPoints.Count > 0));
            }
        }

        private bool CullingRayCast(Vector3 from, Utility.ClipPlanePoints _to, out RaycastHit hitInfo, float distance, LayerMask cullingLayer, Color color)
        {
            bool value = false;
            if (showGizmos)
            {
                Debug.DrawRay(from, _to.LowerLeft - from, color);
                Debug.DrawLine(_to.LowerLeft, _to.LowerRight, color);
                Debug.DrawLine(_to.UpperLeft, _to.UpperRight, color);
                Debug.DrawLine(_to.UpperLeft, _to.LowerLeft, color);
                Debug.DrawLine(_to.UpperRight, _to.LowerRight, color);
                Debug.DrawRay(from, _to.LowerRight - from, color);
                Debug.DrawRay(from, _to.UpperLeft - from, color);
                Debug.DrawRay(from, _to.UpperRight - from, color);
            }
            if (Physics.Raycast(from, _to.LowerLeft - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                CullingDistance = hitInfo.distance;
            }

            if (Physics.Raycast(from, _to.LowerRight - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                if (CullingDistance > hitInfo.distance) CullingDistance = hitInfo.distance;
            }

            if (Physics.Raycast(from, _to.UpperLeft - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                if (CullingDistance > hitInfo.distance) CullingDistance = hitInfo.distance;
            }

            if (Physics.Raycast(from, _to.UpperRight - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                if (CullingDistance > hitInfo.distance) CullingDistance = hitInfo.distance;
            }

            return value;
        }
    }
}