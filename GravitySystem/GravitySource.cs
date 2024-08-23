using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class GravitySource : MonoBehaviour
{
    [SerializeField] private bool debug_View = false;
    [SerializeField] protected Transform sourceObject;
    [SerializeField] protected CustomTrigger customTrigger;

    [Tooltip("Higher values will be prioritzed when applying gravity, use a value of 1 unless the source is intended to overpower other sources")]
    [SerializeField] private int gravitySourcePriority = 1;
    [SerializeField] protected float gravityStrength = -9.8f;

    [SerializeField] private bool invertGravity = false;
    private Vector3 gravity;

    private List<GameObject> affectedObjects = new List<GameObject>();

    protected bool requireGravityUpdates = false;

    public int GravitySourcePriority { get { return gravitySourcePriority; } }

    void Awake()
    {
        if (sourceObject == null)
        {
            sourceObject = this.transform;
        }
    }

    protected void SubscribeToCustomTrigger()
    {
        if (customTrigger != null)
        {
            // Subscribe to custom trigger events
            customTrigger.OnEnterBounds += OnCustomTriggerEnter;
            customTrigger.OnExitBounds += OnCustomTriggerExit;
        }
    }

    protected void UnsubscribeToCustomTrigger()
    {
        if (customTrigger != null)
        {
            // Unsubscribe to custom trigger events
            customTrigger.OnEnterBounds -= OnCustomTriggerEnter;
            customTrigger.OnExitBounds -= OnCustomTriggerExit;
        }
    }
    public Vector3 GetGravity(Transform transform)
    {
        if (requireGravityUpdates) // Certain gravity sources do not need to update their gravity direction and strength dynamically.
        {
            
        }
        gravity = CalculateGravity(transform);
        if (invertGravity)
        {
            gravity *= -1f;
        }

        if (debug_View)
        {
            Debug.DrawLine(transform.position, transform.position + gravity, Color.red);
        }

        return gravity;
    }

    public Quaternion GetTargetRotation(Transform transform)
    {
        return CalculateRotation(transform.transform);
    }

    protected virtual Vector3 CalculateGravity(Transform targetObject)
    {
        return Vector3.zero;
    }

    protected virtual Quaternion CalculateRotation(Transform targetObject)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(targetObject.up, -gravity.normalized) * targetObject.rotation;
        return targetRotation;
    }

    private float DistanceScale(Transform targetObject)
    {
        float scale = 1;
        float distace = Vector3.Distance(this.transform.position, targetObject.transform.position);


        return scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (customTrigger != null) return;
        HandleEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (customTrigger != null) return;
        HandleExit(other);
    }

    protected void OnCustomTriggerEnter(Collider other)
    {
        HandleEnter(other);
    }

    protected void OnCustomTriggerExit(Collider other)
    {
        HandleExit(other);
    }    

    private void HandleEnter(Collider other)
    {
        GravityHandler gravityBoundObject = other.gameObject.GetComponent<GravityHandler>();
        if (gravityBoundObject != null)
        {
            gravityBoundObject.AddGravitySource(this);
            affectedObjects.Add(other.gameObject);
        }
    }

    private void HandleExit(Collider other)
    {
        GravityHandler gravityBoundObject = other.gameObject.GetComponent<GravityHandler>();
        if (gravityBoundObject == null) { return; }
        if (affectedObjects.Contains(other.gameObject))
        {
            gravityBoundObject.RemoveGravitySource(this);
            affectedObjects.Remove(other.gameObject);
        }
    }

    public void InvertGravityDir()
    {
        invertGravity = !invertGravity;
    }

    public void SetGravityInversionState(bool boolean)
    {
        invertGravity = boolean;
    }
}
