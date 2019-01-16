using UnityEngine;
using System.Collections;
using Player.Input;
/* This class is responsible for setting up the player input.
 * It will recive input and store it in a 2D vector.
 * It will move the character and tell the animator to move based on user input
 * Any extension class to deal with player input will be added to this class
 */

namespace RingEternal.MyThirdPersonController
{
    public class PlayerInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected Rigidbody _rigidbody;
        [SerializeField] protected PlayerBlackboard blackboard;


        // Input state
        public struct State
        {
            public Vector3 move;
            public Vector3 lookPos;
            public bool crouch;
            public bool jump;
            public int actionIndex;
        }

        public State state = new State();           // The current state of the user input
        [SerializeField] Transform cam;             // Reference to camera.main

        #region Setting up InControl

        protected PlayerActions playerActions;
        string saveData;
        
        void OnEnable()
        {
            // See PlayerActions.cs for this setup.
            playerActions = PlayerActions.CreateWithDefaultBindings();
            //playerActions.Move.OnLastInputTypeChanged += ( lastInputType ) => Debug.Log( lastInputType );

            LoadBindings();
        }

        void OnDisable()
        {
            // This properly disposes of the action set and unsubscribes it from
            // update events so that it doesn't do additional processing unnecessarily.
            playerActions.Destroy();
        }

        

        void SaveBindings()
        {
            saveData = playerActions.Save();
            PlayerPrefs.SetString("Bindings", saveData);
        }


        void LoadBindings()
        {
            if (PlayerPrefs.HasKey("Bindings"))
            {
                saveData = PlayerPrefs.GetString("Bindings");
                playerActions.Load(saveData);
            }
        }


        void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }


        #endregion

        protected virtual void Update()
        {
            HandleInput();

        }



        protected virtual void HandleInput()
        {
            StoreMovement();
            CheckForSprint();
        }

        protected virtual void StoreMovement()
        {


            // calculate move direction
            Vector3 move = cam.rotation * new Vector3(playerActions.Move.X, 0f, playerActions.Move.Y).normalized;

            // Flatten move vector to the character.up plane
            if (move != Vector3.zero)
            {
                Vector3 normal = transform.up;
                Vector3.OrthoNormalize(ref normal, ref move);
                state.move = move;
            }
            else state.move = Vector3.zero;

            float speedMultiplier = (blackboard.RunByDefault) ? 1.5f : 1f; // Set the correct speed value to pass to animator

            state.move *= speedMultiplier;

            // calculate the head look target position
            state.lookPos = transform.position + cam.forward * 100f;
        }
        
        
        protected virtual void CheckForSprint()
        {
            if (playerActions.Sprint.IsPressed)
            {
                blackboard.IsSprinting = true;
                DecreaseSprintStamina(blackboard.CurrentSprintStamina);
            }
            else
            {
                blackboard.IsSprinting = false;
                IncreaseSprintStamina(blackboard.CurrentSprintStamina);
            }
        }

        protected virtual void DecreaseSprintStamina(float currentSprintStamina)
        {
            currentSprintStamina -= Time.deltaTime;
            if(currentSprintStamina <= 0)
            {
                currentSprintStamina = 0;
            }
            blackboard.CurrentSprintStamina = currentSprintStamina;
        }

        protected virtual void IncreaseSprintStamina(float currentSprintStamina)
        {
            currentSprintStamina += Time.deltaTime;
            if(currentSprintStamina >= blackboard.MaxSprintStamina)
            {
                currentSprintStamina = blackboard.MaxSprintStamina;
            }
            blackboard.CurrentSprintStamina = currentSprintStamina;
        }
        

    }
}