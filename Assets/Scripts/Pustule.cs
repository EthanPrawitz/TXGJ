using System.Collections;
using UnityEngine;

public class Pustule : MonoBehaviour, IActivatable
{

    [SerializeField]
    private GameObject explosionPrefab;
    [SerializeField]
    private float radius;

    public bool activated = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("Test Detonation")]
    public void Activate()
    {
        if (activated) return;
        activated = true;

        StartCoroutine(DelayedExplosion());
    }

    IEnumerator DelayedExplosion()
    {
        yield return new WaitForSeconds(0.2f);

        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.GetComponent<Explosion>().ExplosionBlast(radius);

        Destroy(gameObject);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
