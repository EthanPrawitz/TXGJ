using UnityEngine;

// Attach to any one-shot particle effect prefab (explosions, impacts, etc.)
// so it cleans itself up after playing instead of lingering in the scene forever.

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAutoDestruct : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // IsAlive checks both the system and any child particle systems
        if (!ps.IsAlive(true))
        {
            Destroy(gameObject);
        }
    }
}
