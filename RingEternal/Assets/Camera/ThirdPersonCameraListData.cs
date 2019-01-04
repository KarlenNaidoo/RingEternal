using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "New State", menuName = "Camera/State", order = 1)]
    public class ThirdPersonCameraListData : ScriptableObject
    {
        [SerializeField] public string Name;
        [SerializeField] public List<ThirdPersonCameraState> tpCameraStates;

        public ThirdPersonCameraListData()
        {
            tpCameraStates = new List<ThirdPersonCameraState>();
            tpCameraStates.Add(new ThirdPersonCameraState("Default"));
        }
    }
}