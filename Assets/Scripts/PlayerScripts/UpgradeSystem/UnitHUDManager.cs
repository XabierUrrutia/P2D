using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitHUDManager : MonoBehaviour
{
    public static UnitHUDManager Instance;

    [Header("Referencias")]
    public GameObject panelCompleto;
    public Image imagenRetratoUI;
    public Sprite spritePorDefecto;
    public Slider xpSlider;
    public Slider hpSlider;
    public Slider shieldSlider;
    public TextMeshProUGUI nivelTexto;
    public TextMeshProUGUI hpTexto;

    [Header("Munición")]
    public TextMeshProUGUI municionTexto;

    // Referencia a TU script de disparo
    private PlayerShooting armaSeleccionada;

    private UnitVeterancy veteraniaSeleccionada;
    private IHealth saludSeleccionada;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Corrección de rangos 0-1
        if (xpSlider != null) { xpSlider.minValue = 0; xpSlider.maxValue = 1; }
        if (hpSlider != null) { hpSlider.minValue = 0; hpSlider.maxValue = 1; }
        if (shieldSlider != null) { shieldSlider.minValue = 0; shieldSlider.maxValue = 1; }

        SeleccionarUnidad(null);
    }

    void Update()
    {
        // Esto hace que las barras del HUD se muevan en tiempo real
        if (saludSeleccionada != null)
        {
            ActualizarStatsCombate();
        }
    }

    public void SeleccionarUnidad(UnitVeterancy unidad)
    {
        // Limpieza previa
        if (veteraniaSeleccionada != null)
            veteraniaSeleccionada.OnStatsChanged -= ActualizarBarraXP;

        veteraniaSeleccionada = unidad;

        if (veteraniaSeleccionada != null)
        {
            // 1. Conexiones básicas
            veteraniaSeleccionada.OnStatsChanged += ActualizarBarraXP;
            saludSeleccionada = veteraniaSeleccionada.GetComponent<IHealth>();

            // 2. BUSQUEDA DEL ARMA CON CHIVATO (DEBUG)
            armaSeleccionada = veteraniaSeleccionada.GetComponent<PlayerShooting>();

            if (armaSeleccionada == null)
            {
                Debug.LogError("¡ERROR! La unidad seleccionada NO tiene el script 'PlayerShooting' puesto.");
            }
            else
            {
                Debug.Log("¡EXITO! Arma encontrada. Balas: " + armaSeleccionada.currentAmmo);
            }

            // 3. Activar Panel
            if (panelCompleto != null) panelCompleto.SetActive(true);

            // 4. Activar Texto Balas
            if (municionTexto != null)
            {
                // Solo activamos el texto si encontramos el arma
                municionTexto.gameObject.SetActive(armaSeleccionada != null);

                // Forzamos el texto inmediatamente para ver si funciona
                if (armaSeleccionada != null)
                {
                    municionTexto.text = "TEST " + armaSeleccionada.currentAmmo;
                }
            }
            else
            {
                Debug.LogError("¡ERROR! No has asignado el 'Municion Texto' en el inspector del HUD.");
            }

            // Resto de configuraciones
            if (imagenRetratoUI != null)
            {
                if (unidad.retratoCara != null) imagenRetratoUI.sprite = unidad.retratoCara;
                else if (spritePorDefecto != null) imagenRetratoUI.sprite = spritePorDefecto;
                imagenRetratoUI.color = Color.white;
            }

            if (shieldSlider != null)
            {
                bool tieneEscudo = saludSeleccionada != null && saludSeleccionada.GetMaxShield() > 0;
                shieldSlider.gameObject.SetActive(tieneEscudo);
            }

            ActualizarBarraXP();
            ActualizarStatsCombate();
        }
        else
        {
            if (panelCompleto != null) panelCompleto.SetActive(false);
            saludSeleccionada = null;
            armaSeleccionada = null;
        }
    }

    void ActualizarStatsCombate()
    {
        if (saludSeleccionada == null) return;

        // --- 1. VIDA ---
        if (hpSlider != null)
        {
            float vida = (float)saludSeleccionada.GetCurrentHealth();
            float maxVida = (float)saludSeleccionada.GetMaxHealth();
            hpSlider.value = (maxVida > 0) ? vida / maxVida : 0;

            if (hpTexto != null) hpTexto.text = $"{vida}/{maxVida}";
        }

        // --- 2. ESCUDO ---
        if (shieldSlider != null && shieldSlider.gameObject.activeSelf)
        {
            float escudo = (float)saludSeleccionada.GetCurrentShield();
            float maxEscudo = (float)saludSeleccionada.GetMaxShield();
            shieldSlider.value = (maxEscudo > 0) ? escudo / maxEscudo : 0;
        }

        // --- 3. MUNICIÓN (Aquí leemos tu PlayerShooting) ---
        if (armaSeleccionada != null && municionTexto != null)
        {
            // Leemos las variables 'currentAmmo' y 'maxAmmo' de tu script
            municionTexto.text = $"{armaSeleccionada.currentAmmo} / {armaSeleccionada.maxAmmo}";

            // Color rojo si quedan pocas balas
            if (armaSeleccionada.currentAmmo == 0) municionTexto.color = Color.grey;
            else if (armaSeleccionada.currentAmmo <= 3) municionTexto.color = Color.red;
            else municionTexto.color = Color.white;
        }
    }

    void ActualizarBarraXP()
    {
        if (veteraniaSeleccionada == null) return;

        // 1. Actualizar Slider de XP
        if (xpSlider != null)
        {
            float actual = veteraniaSeleccionada.xpActual;
            float necesario = veteraniaSeleccionada.xpParaSiguienteNivel;
            // Evitar división por cero
            xpSlider.value = (necesario > 0) ? actual / necesario : 0;
        }

        // 2. Actualizar TEXTO DE NIVEL (NUEVO)
        if (nivelTexto != null)
        {
            // Escribe el número. Puedes poner "Lvl " + ... si quieres texto antes
            nivelTexto.text = veteraniaSeleccionada.nivel.ToString();
        }
    }
}