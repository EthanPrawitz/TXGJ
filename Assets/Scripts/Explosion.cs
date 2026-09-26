using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField]
    private float lifetime = 1.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ExplosionBlast(float radius)
    {
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D col in objectsInRange)
        {
            IActivatable activatableTarget = col.GetComponent<IActivatable>();

            if (activatableTarget != null)
            {
                activatableTarget.Activate();
            }
        }
    }
}
