using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Usable : MonoBehaviour , IUseable
{
    [Header("Base Settings")]
    [SerializeField] protected Item usableItem;
    protected GameObject player;

    public Item UsableItem { get { return usableItem; } set { usableItem = value; } }

    // Start is called before the first frame update

    public void Equip() 
    {
        OnEquip();
    }
    public void UnEquip()
    {
        OnUnequip();
    }
    public void ConfigureReferences()
    {
        player = Utility.FindParentWithTag(this.gameObject, "Player");
        ConfigureExtendedReferences();
    }

    protected virtual void OnEquip()
    { 
    }
    protected virtual void OnUnequip()
    {
    }
    protected virtual void ConfigureExtendedReferences() // if any inheriting class has special references to assign it will be in their override method of this.
    {
    }

    public virtual void HandleLeftClick()
    {
    }

    public virtual void HandleLeftClickHold()
    {
    }

    public virtual void HandleLeftClickUp()
    {
    }

    public virtual void HandleRightClick()
    {
    }

    public virtual void HandleRightClickHold()
    {
    }

    public virtual void HandleRightClickUp()
    {
    }

    public virtual void HandleUseButton()
    {
    }

    public virtual void HandleReloadButton()
    {
    }
}
