using UnityEngine;
using UnityEngine.Events;

public class EnemyDeathListener : MonoBehaviour
{
    public UnityEvent onEnemyDied;

    private bool _isInvoking = false;

    // Auto-regista o manager caso o inimigo seja instanciado em runtime
    void Awake()
    {
        var mgr = FindObjectOfType<TutorialStep2Manager>();
        if (mgr != null)
        {
            // evita duplicação
            onEnemyDied.RemoveListener(mgr.OnEnemyDeath);
            onEnemyDied.AddListener(mgr.OnEnemyDeath);
            Debug.Log($"[EnemyDeathListener] Auto-subscrito '{gameObject.name}' ao TutorialStep2Manager");
        }
    }

    // Método público que o sistema de morte chama
    public void InvokeDeath()
    {
        Debug.Log($"[EnemyDeathListener] InvokeDeath() chamado em '{gameObject.name}'");

        if (_isInvoking) return;
        _isInvoking = true;

        // Evita reentrada
        onEnemyDied?.Invoke();

        _isInvoking = false;
    }
}