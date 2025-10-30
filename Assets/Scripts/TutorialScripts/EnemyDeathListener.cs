using UnityEngine;
using UnityEngine.Events;

// Anexa a este componente no prefab/enemy na cena.
// Quando o GameObject for destruído (morre), invoke OnDeath.
[RequireComponent(typeof(Collider2D))]
public class EnemyDeathListener : MonoBehaviour
{
    [Header("Evento chamado quando este inimigo morre/destrói")]
    public UnityEvent OnDeath;

    void OnDestroy()
    {
        // Invoca o evento quando o inimigo é destruído
        if (Application.isPlaying)
            OnDeath?.Invoke();
    }

    // Método auxiliar para invocar manualmente (útil em editor/Debug)
    public void InvokeDeath()
    {
        OnDeath?.Invoke();
    }
}