using UnityEngine;
using System; // Necesario para los Actions

public class UnitVeterancy : MonoBehaviour
{
    // EVENTO: Avisa al mundo (y al HUD) que mis stats cambiaron
    public event Action OnStatsChanged;

    [Header("Identidad de la Unidad")]
    public Sprite retratoCara;

    [Header("Estado")]
    public int nivel = 1;
    public int xpActual = 0;
    public int xpParaSiguienteNivel = 100;

    [Header("Configuración")]
    public int maxNivel = 3;
    public int bonusSalud = 2;
    public int bonusDaño = 1;

    // Referencias internas
    private PlayerShooting shootingScript;
    private PlayerHealth healthScript;

    // (Opcional) Referencia a SelectableUnit para saber si soy yo el elegido
    private SelectableUnit selectableScript;

    void Start()
    {
        shootingScript = GetComponent<PlayerShooting>();
        healthScript = GetComponent<PlayerHealth>();
        selectableScript = GetComponent<SelectableUnit>();
    }

    public void GanarXP(int cantidad)
    {
        if (nivel >= maxNivel) return;

        xpActual += cantidad;

        if (xpActual >= xpParaSiguienteNivel)
        {
            SubirNivel();
        }

        // ¡AVISO IMPORTANTE! Mis datos cambiaron. 
        // Si el HUD me está mirando, se actualizará solo.
        OnStatsChanged?.Invoke();
    }

    void SubirNivel()
    {
        xpActual -= xpParaSiguienteNivel;
        nivel++;
        xpParaSiguienteNivel = Mathf.RoundToInt(xpParaSiguienteNivel * 1.5f);

        if (shootingScript != null) shootingScript.bulletDamage += bonusDaño;
        if (healthScript != null) { healthScript.maxHealth += bonusSalud; healthScript.Revive(); }

        Debug.Log($"{name} subió a nivel {nivel}");
    }
}