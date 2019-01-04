using UnityEngine;
using System.Collections;
using Player;
using Player.Input;
using Player.PlayerController;
using com.ootii.Cameras;

public class CameraManager: PlayerInput
{

    [Header("References")]

    public ThirdPersonCamera cam;              // acess camera info


    public bool ignoreCameraRotation;
    [HideInInspector]
    public string desiredCameraState;                    // generic string to change the CameraState        
    [HideInInspector]
    public string customlookAtPoint;                    // generic string to change the CameraPoint of the Fixed Point Mode        
    [HideInInspector]
    public bool changeCameraState;                      // generic bool to change the CameraState        
    [HideInInspector]
    public bool smoothCameraState;                      // generic bool to know if the state will change with or without lerp  
    [HideInInspector]
    public bool keepDirection;                          // keep the current direction in case you change the cameraState

    public virtual void CameraInput()
    {
        if (!Camera.main) Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
        if (!ignoreCameraRotation)
        {
            RotateWithCamera(Camera.main.transform);
        }

        if (cam == null)
            return;
       
        var Y = playerActions.MoveMouse.Y;
        var X = playerActions.MoveMouse.X;
        //var zoom = Input.GetAxis("Mouse ScrollWheel");

        cam.RotateCamera(X, Y);
        //tpCamera.Zoom(zoom);

        // change keepDirection from input difference
        if (keepDirection && Vector2.Distance(blackboard.input, blackboard.oldInput) > 0.2f) keepDirection = false;

        if (playerActions.LockOn.WasPressed)
        {
            blackboard.lockOnPressed = true;
        }
            
    }

    protected virtual void UpdateCameraStates()
    {
        // CAMERA STATE - you can change the CameraState here, the bool means if you want lerp of not, make sure to use the same CameraState String that you named on TPCameraListData

        if (cam == null)
        {
             return;
            
        }
        //if (cam)
        //{
        //    cam.SetMainTarget(this.transform);
        //    cam.Init();
        //}
        if (changeCameraState)
            cam.ChangeState(desiredCameraState, customlookAtPoint, smoothCameraState);
        else if (blackboard.isCrouching)
            cam.ChangeState("Crouch", true);
        else
            cam.ChangeState("MovementState", true);

    }

    public void ChangeCameraState(string cameraState)
    {
        changeCameraState = true;
        desiredCameraState = cameraState;
    }

    public void ResetCameraState()
    {
        changeCameraState = false;
        desiredCameraState = string.Empty;
    }

    protected virtual void RotateWithCamera(Transform cameraTransform)
    {
        // smooth align character with aim position               
        if (cam != null && cam.lockTarget)
        {
            blackboard.RotateToTarget(cam.lockTarget);
        }
        // rotate the camera around the character and align with when the char move
        else if (blackboard.input != Vector2.zero)
        {
            blackboard.lookPos = cameraTransform;
            //blackboard.RotateWithAnotherTransform(cameraTransform);
        }

    }

    protected virtual void LateUpdate()
    {
        CameraInput();                      // update camera input
        UpdateCameraStates();               // update camera states
    }
}
