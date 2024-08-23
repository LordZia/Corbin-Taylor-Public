using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_EnableObject : MonoBehaviour
{
    [SerializeField] private GameObject _gameObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.CompareTag("Player"))
        {
            _gameObject.SetActive(true);
        }
    }
}
