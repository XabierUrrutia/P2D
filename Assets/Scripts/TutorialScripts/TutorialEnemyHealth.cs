using UnityEngine;

public class TutorialEnemyHealth : MonoBehaviour
{
    public int maxHp = 10;
    int hp;

    void Awake() => hp = maxHp;

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
            Die();
    }

    void Die()
    {
        var listener = GetComponent<EnemyDeathListener>();
        if (listener != null)
        {
            listener.InvokeDeath(); // chama evento ANTES de destruir
            Debug.Log($"[EnemyHealth] '{gameObject.name}' morreu — evento disparado");
        }
        else
        {
            Debug.LogWarning($"[EnemyHealth] '{gameObject.name}' não tem EnemyDeathListener");
        }

        // Destrói depois de invocar o evento
        Destroy(gameObject);
    }
}