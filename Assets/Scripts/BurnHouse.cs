using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurnHouse : MonoBehaviour
{
    [SerializeField] private ParticleSystem effectPrefab;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private float particleLifetime = 5f; // How long particles remain after object destruction
    private ParticleSystem activeEffect;

    public void BurnTheHouse()
    {
        if (effectPrefab != null)
        {
            activeEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            var renderer = activeEffect.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 5; // Adjust as needed
            }
            activeEffect.Play();

            // Destroy particles after their lifetime
            Destroy(activeEffect.gameObject, particleLifetime);
        }

        if (destroyDelay > 0)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
