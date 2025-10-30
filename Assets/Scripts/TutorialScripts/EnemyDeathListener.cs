using UnityEngine;
using UnityEngine.Events;

// Anexa a este componente no prefab/enemy na cena.
// Quando o GameObject for destru�do (morre), invoke OnDeath.
[RequireComponent(typeof(Collider2D))]
public class EnemyDeathListener : MonoBehaviour
{
    [Header("Evento chamado quando este inimigo morre/destr�i")]
    public UnityEvent OnDeath;

    void OnDestroy()
    {
        // Invoca o evento quando o inimigo � destru�do
        if (Application.isPlaying)
            OnDeath?.Invoke();
    }

    // M�todo auxiliar para invocar manualmente (�til em editor/Debug)
    public void InvokeDeath()
    {
        OnDeath?.Invoke();
    }
}