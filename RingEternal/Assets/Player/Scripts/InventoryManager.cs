using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerBlackboard _blackboard;
    [SerializeField] WeaponList _weaponList;
    
    private void Awake()
    {
        _blackboard.weaponList = _weaponList;
    }
}


[System.Serializable]
public class WeaponList
{
     public List<WeaponAction> oneHandedSwordActions;
     public List<WeaponAction> twoHandedSwordActions;
     public List<WeaponAction> meleeActions;
}