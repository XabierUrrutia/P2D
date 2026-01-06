using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitHUDManager : MonoBehaviour
{
    public static UnitHUDManager Instance;

    [Header("Referencias")]
    public GameObject panelCompleto;
    public Slider xpSlider;
    public Slider hpSlider;
    public TextMeshProUGUI nivelTexto;
    public TextMeshProUGUI hpTexto;

    private UnitVeterancy veteraniaSeleccionada;
    private IHealth saludSeleccionada;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Comprobación de seguridad al iniciar
        if (xpSlider == null) Debug.LogError("ERROR CRÍTICO: ¡Falta asignar el XP Slider en el Inspector de UnitHUDManager!");
        if (hpSlider == null) Debug.LogError("ERROR CRÍTICO: ¡Falta asignar el HP Slider en el Inspector de UnitHUDManager!");

        SeleccionarUnidad(null);
    }

    void Update()
    {
        // Actualizar vida constantemente
        if (saludSeleccionada != null && hpSlider != null)
        {
            ActualizarBarraVida();
        }
    }

    public void SeleccionarUnidad(UnitVeterancy unidad)
    {
        // 1. Limpieza anterior
        if (veteraniaSeleccionada != null)
        {
            veteraniaSeleccionada.OnStatsChanged -= ActualizarBarraXP;
        }

        veteraniaSeleccionada = unidad;

        // 2. Nueva conexión
        if (veteraniaSeleccionada != null)
        {
            Debug.Log($"[HUD] Conectado con unidad: {unidad.name}"); // <--- ¡MIRA SI SALE ESTO!

            // Conectar eventos y componentes
            veteraniaSeleccionada.OnStatsChanged += ActualizarBarraXP;
            saludSeleccionada = veteraniaSeleccionada.GetComponent<IHealth>();

            if (saludSeleccionada == null) Debug.LogWarning($"[HUD] La unidad {unidad.name} tiene Veteranía pero NO tiene script de vida (IHealth).");

            // Mostrar Panel
            if (panelCompleto != null) panelCompleto.SetActive(true);

            // Forzar actualización inicial
            ActualizarBarraXP();
            ActualizarBarraVida();
        }
        else
        {
            // Ocultar si es null
            Debug.Log("[HUD] Deseleccionado (Ocultando panel)");
            saludSeleccionada = null;
            if (panelCompleto != null) panelCompleto.SetActive(false);
        }
    }

    void ActualizarBarraXP()
    {
        if (veteraniaSeleccionada == null || xpSlider == null) return;

        float maxXP = (float)veteraniaSeleccionada.xpParaSiguienteNivel;
        if (maxXP <= 0) maxXP = 1;

        float valor = (float)veteraniaSeleccionada.xpActual / maxXP;
        xpSlider.value = valor;

        // Debug.Log($"[HUD] XP Actualizada: {valor * 100}%");
    }

    void ActualizarBarraVida()
    {
        if (saludSeleccionada == null || hpSlider == null) return;

        float vida = saludSeleccionada.GetCurrentHealth();
        float max = saludSeleccionada.GetMaxHealth();

        hpSlider.value = vida / max;

        if (hpTexto != null) hpTexto.text = $"{vida}/{max}";
    }
}