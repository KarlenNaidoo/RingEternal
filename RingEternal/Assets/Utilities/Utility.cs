using UnityEngine;

namespace Player
{

    public static class Utility
    {
        public class AnimatorParameter
        {
            private readonly AnimatorControllerParameter _parameter;

            // Returns a valid parameter within the animation controller
            // Valid is defined as a param that is found within the controller
            public static implicit operator int(AnimatorParameter a)
            {
                if (a.isValid) return a._parameter.nameHash;
                else
                    return -1;
            }

            public readonly bool isValid;

            public AnimatorParameter(Animator animator, string parameter)
            {
                if (animator && animator.ContainsParam(parameter))
                {
                    _parameter = animator.GetValidParameter(parameter);
                    this.isValid = true;
                }
                else this.isValid = false;
            }
        }
        
        public static class Constants
        {
            public static string Horizontal = "Horizontal";
            public static string Vertical = "Vertical";
            public static string Crouch = "Crouch";
            public static string IdleRandomTrigger = "IdleRandomTrigger";
            public static string IdleRandom = "IdleRandom";
            public static string Sprint = "Run";
            public static string Strafe = "Strafe";
            public static string InputMagnitude = "InputMagnitude";
            public static float DeadZone = 0.1f;
            public static string LightAttack = "LightAttack";
            public static string HeavyAttack = "HeavyAttack";
        }

        public static AnimatorControllerParameter GetValidParameter(this Animator _Anim, string _ParamName)
        {
            foreach (AnimatorControllerParameter param in _Anim.parameters)
            {
                if (param.name == _ParamName) return param;
            }
            return null;
        }

        public static bool ContainsParam(this Animator _Anim, string _ParamName)
        {
            foreach (AnimatorControllerParameter param in _Anim.parameters)
            {
                if (param.name == _ParamName) return true;
            }
            return false;
        }

        // Returns the signed angle in degress
        public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector)
        {
            if (fromVector == toVector)
                return 0.0f;
            float angle = Vector3.Angle(fromVector, toVector);
            Vector3 cross = Vector3.Cross(fromVector, toVector);
            angle *= Mathf.Sign(cross.y);
            return angle;
        }



        /// <summary>
        /// Lerp between CameraStates
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="time"></param>
        public static void Slerp(this ThirdPersonCameraState to, ThirdPersonCameraState from, float time)
        {
            to.Name = from.Name;
            to.forward = Mathf.Lerp(to.forward, from.forward, time);
            to.right = Mathf.Lerp(to.right, from.right, time);
            to.defaultDistance = Mathf.Lerp(to.defaultDistance, from.defaultDistance, time);
            to.maxDistance = Mathf.Lerp(to.maxDistance, from.maxDistance, time);
            to.minDistance = Mathf.Lerp(to.minDistance, from.minDistance, time);
            to.height = Mathf.Lerp(to.height, from.height, time);
            to.fixedAngle = Vector2.Lerp(to.fixedAngle, from.fixedAngle, time);
            to.smoothFollow = Mathf.Lerp(to.smoothFollow, from.smoothFollow, time);
            to.xMouseSensitivity = Mathf.Lerp(to.xMouseSensitivity, from.xMouseSensitivity, time);
            to.yMouseSensitivity = Mathf.Lerp(to.yMouseSensitivity, from.yMouseSensitivity, time);
            to.yMinLimit = Mathf.Lerp(to.yMinLimit, from.yMinLimit, time);
            to.yMaxLimit = Mathf.Lerp(to.yMaxLimit, from.yMaxLimit, time);
            to.xMinLimit = Mathf.Lerp(to.xMinLimit, from.xMinLimit, time);
            to.xMaxLimit = Mathf.Lerp(to.xMaxLimit, from.xMaxLimit, time);
            to.rotationOffSet = Vector3.Lerp(to.rotationOffSet, from.rotationOffSet, time);
            to.cullingHeight = Mathf.Lerp(to.cullingHeight, from.cullingHeight, time);
            to.cullingMinDist = Mathf.Lerp(to.cullingMinDist, from.cullingMinDist, time);
            to.cameraMode = from.cameraMode;
            to.useZoom = from.useZoom;
            to.lookPoints = from.lookPoints;
            to.fov = Mathf.Lerp(to.fov, from.fov, time);

            if (to.fov <= 0) to.fov = 1f;
        }

        public static ClipPlanePoints NearClipPlanePoints(this Camera camera, Vector3 pos, float clipPlaneMargin)
        {
            var clipPlanePoints = new ClipPlanePoints();

            var transform = camera.transform;
            var halfFOV = (camera.fieldOfView / 2) * Mathf.Deg2Rad;
            var aspect = camera.aspect;
            var distance = camera.nearClipPlane;
            var height = distance * Mathf.Tan(halfFOV);
            var width = height * aspect;
            height *= 1 + clipPlaneMargin;
            width *= 1 + clipPlaneMargin;
            clipPlanePoints.LowerRight = pos + transform.right * width;
            clipPlanePoints.LowerRight -= transform.up * height;
            clipPlanePoints.LowerRight += transform.forward * distance;

            clipPlanePoints.LowerLeft = pos - transform.right * width;
            clipPlanePoints.LowerLeft -= transform.up * height;
            clipPlanePoints.LowerLeft += transform.forward * distance;

            clipPlanePoints.UpperRight = pos + transform.right * width;
            clipPlanePoints.UpperRight += transform.up * height;
            clipPlanePoints.UpperRight += transform.forward * distance;

            clipPlanePoints.UpperLeft = pos - transform.right * width;
            clipPlanePoints.UpperLeft += transform.up * height;
            clipPlanePoints.UpperLeft += transform.forward * distance;

            return clipPlanePoints;
        }


        /// <summary>
        /// Normalized the angle. between -180 and 180 degrees
        /// </summary>
        /// <param Name="eulerAngle">Euler angle.</param>
        public static Vector3 NormalizeAngle(this Vector3 eulerAngle)
        {
            var delta = eulerAngle;

            if (delta.x > 180) delta.x -= 360;
            else if (delta.x < -180) delta.x += 360;

            if (delta.y > 180) delta.y -= 360;
            else if (delta.y < -180) delta.y += 360;

            if (delta.z > 180) delta.z -= 360;
            else if (delta.z < -180) delta.z += 360;

            return new Vector3(delta.x, delta.y, delta.z);//round values to angle;
        }


        public static float ClampAngle(float angle, float min, float max)
        {
            do
            {
                if (angle < -360)
                    angle += 360;
                if (angle > 360)
                    angle -= 360;
            } while (angle < -360 || angle > 360);

            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// Copy of CameraStates
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        public static void CopyState(this ThirdPersonCameraState to, ThirdPersonCameraState from)
        {
            to.Name = from.Name;
            to.forward = from.forward;
            to.right = from.right;
            to.defaultDistance = from.defaultDistance;
            to.maxDistance = from.maxDistance;
            to.minDistance = from.minDistance;
            to.height = from.height;
            to.fixedAngle = from.fixedAngle;
            to.lookPoints = from.lookPoints;
            to.smoothFollow = from.smoothFollow;
            to.xMouseSensitivity = from.xMouseSensitivity;
            to.yMouseSensitivity = from.yMouseSensitivity;
            to.yMinLimit = from.yMinLimit;
            to.yMaxLimit = from.yMaxLimit;
            to.xMinLimit = from.xMinLimit;
            to.xMaxLimit = from.xMaxLimit;
            to.rotationOffSet = from.rotationOffSet;
            to.cullingHeight = from.cullingHeight;
            to.cullingMinDist = from.cullingMinDist;
            to.cameraMode = from.cameraMode;
            to.useZoom = from.useZoom;
            to.fov = from.fov;

            if (to.fov <= 0) to.fov = 1f;
        }


        public struct ClipPlanePoints
        {
            public Vector3 UpperLeft;
            public Vector3 UpperRight;
            public Vector3 LowerLeft;
            public Vector3 LowerRight;
        }
    }
}



public static class DamageHelper
{
    public static float HitAngle(this Transform transform, Vector3 hitpoint, bool normalized = true)
    {
        var localTarget = transform.InverseTransformPoint(hitpoint);
        var _angle = (int)(Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg);

        if (!normalized) return _angle;

        if (_angle <= 45 && _angle >= -45)
            _angle = 0;
        else if (_angle > 45 && _angle < 135)
            _angle = 90;
        else if (_angle >= 135 || _angle <= -135)
            _angle = 180;
        else if (_angle < -45 && _angle > -135)
            _angle = -90;

        return _angle;
    }
}


public enum WeaponStatus { None, OneHanded, TwoHanded }

public enum ControllerActionInput { Square, Triangle, L1, L2, None , X}

public enum LocomotionType { Free, Strafe }

public enum AttackCategory { none, light, heavy, running_heavy, running_light }

public enum HitBoxArea { LeftArm, RightArm, BothArms, LeftLeg, RightLeg, BothLegs, Weapon }

public enum ColliderState { Closed, Open, Colliding }


// Animation state
public struct AnimState
{
    public Vector3 moveDirection; // the forward speed
    public bool crouch; // should the character be crouching?
    public bool onGround; // is the character grounded
    public bool isStrafing; // should the character always rotate to face the move direction or strafe?
    public float yVelocity; // y velocity of the character
    public float vertical;
    public float horizontal;
}

