using UnityEngine;

public class PoolableViewModel : MonoBehaviour, IPoolable
{
    [Tooltip("Where projectiles (or raycasts) should originate for this weapon.")]
    [SerializeField] private Transform _firePoint;
    public Transform FirePoint => _firePoint;

    [Tooltip("Where shell casings eject.")]
    [SerializeField] private Transform _ejectPoint;
    public Transform EjectPoint => _ejectPoint;

    [SerializeField, Tooltip("Must match the pool entry key in NetworkedPrefabPool")]
    private string poolKey = "PoolableViewModel";

    // IPoolable:
    public string PoolKey => poolKey;

    [Tooltip("Internal pivot marking where the weapon grips the hand")]
    public Transform holdPoint;

    // Called once, right after we Instantiate the prefab for the first time
    public void OnWarm()
    {
        gameObject.SetActive(false);
    }

    // Called each time we pull from the pool
    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    // Called each time we return to the pool
    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }

    public void RegisterToPool()
    {
    }
}

