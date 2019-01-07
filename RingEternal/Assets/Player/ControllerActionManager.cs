using UnityEngine;
using System.Collections.Generic;
using static Player.Utility;
using RingEternal.MyThirdPersonController;

public class ControllerActionManager : PlayerInput
{ 
    [SerializeField] List<WeaponAction> actionSlots = new List<WeaponAction>();
    const int CONTROLLER_INPUT_BUTTONS = 4;

    protected ControllerActionManager()
    {
        for (int i = 0; i < CONTROLLER_INPUT_BUTTONS; i++) 
        {
            WeaponAction a = new WeaponAction();
            a.inputButton = (ControllerActionInput)i;
            actionSlots.Add(a);
        }
    }

    protected override void HandleInput()
    {
        base.HandleInput();
        blackboard.actionSlot = GetActionSlot();
        IsOneHandedOrTwoHanded();
        MapControllerAtkActions();
    }

    public void MapControllerAtkActions()
    {
        WeaponList weaponList = blackboard.weaponList;
        //Debug.Log("Current weapon status: " + blackboard.currentWeapon);
        switch (blackboard.currentWeapon)
        {
            case WeaponStatus.OneHanded:
                EmptyAllSlots();
                for (int i = 0; i < weaponList.oneHandedSwordActions.Count; i++)
                {
                    WeaponAction a = GetAction(weaponList.oneHandedSwordActions[i].inputButton);
                    a.targetAnim = weaponList.oneHandedSwordActions[i].targetAnim;
                }
                break;
            case WeaponStatus.TwoHanded:
                EmptyAllSlots();
                for (int i = 0; i < weaponList.twoHandedSwordActions.Count; i++)
                {
                    WeaponAction a = GetAction(weaponList.twoHandedSwordActions[i].inputButton);
                    a.targetAnim = weaponList.twoHandedSwordActions[i].targetAnim;
                }
                break;
            case WeaponStatus.None:
                EmptyAllSlots();
                for (int i = 0; i < weaponList.meleeActions.Count; i++)
                {
                    WeaponAction a = GetAction(weaponList.meleeActions[i].inputButton);
                    a.targetAnim = weaponList.meleeActions[i].targetAnim;
                }
                break;
            default:
                EmptyAllSlots();
                for (int i = 0; i < weaponList.meleeActions.Count; i++)
                {
                    WeaponAction a = GetAction(weaponList.meleeActions[i].inputButton);
                    a.targetAnim = weaponList.meleeActions[i].targetAnim;
                }
                break;
        }
    }

    void EmptyAllSlots()
    {
        for (int i = 0; i < CONTROLLER_INPUT_BUTTONS; i++)
        {
            WeaponAction a = GetAction((ControllerActionInput)i);
            a.targetAnim = null;

        }
    }
    
    public WeaponAction GetActionSlot ()
    {
        ControllerActionInput a_input = GetActionInput();
        return GetAction(a_input);
    }

    public ControllerActionInput GetActionInput ()
    {
        if (playerActions.LightAttack.WasPressed)
        {
            return ControllerActionInput.Square;
        }
        if (playerActions.HeavyAttack.WasPressed)
            return ControllerActionInput.Triangle;
        if (playerActions.Phase.WasPressed)
            return ControllerActionInput.L1;
        if (playerActions.Slide.WasPressed)
        {
            HasCrouched();
            return ControllerActionInput.X;

        }

        return ControllerActionInput.None;
    }

    // Monitors whether player pressed the equip left or right and updates the blackboard's currentweapon
    public void IsOneHandedOrTwoHanded()
    {
        if (!blackboard.weaponEquipped)
            blackboard.currentWeapon = WeaponStatus.None;
        if (playerActions.OneHanded.IsPressed)
            blackboard.currentWeapon = WeaponStatus.OneHanded;
        if (playerActions.TwoHanded.IsPressed)
            blackboard.currentWeapon = WeaponStatus.TwoHanded;
    }

    public void HasCrouched()
    {
        blackboard.speed = 3;
    }

    WeaponAction GetAction(ControllerActionInput input)
    {
        for (int i = 0; i < actionSlots.Count; i++)
        {
            if (actionSlots[i].inputButton == input)
            {
                return actionSlots[i];
            }
        }

        return null;
    }
}


[System.Serializable]
public class WeaponAction
{
    public ControllerActionInput inputButton;
    public string targetAnim;
}

[System.Serializable]
public class ItemAction
{
    public string targetAnim;
    public string itemID;
}


