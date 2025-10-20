using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float damage = 10f;
    public float lifeTime = 5f;

    private void Start() => Destroy(gameObject, lifeTime);

    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
