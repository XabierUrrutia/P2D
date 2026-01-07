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
        // 1. Desconectar anterior
        if (veteraniaSeleccionada != null) veteraniaSeleccionada.OnStatsChanged -= ActualizarBarraXP;

        veteraniaSeleccionada = unidad;

        if (veteraniaSeleccionada != null)
        {
            // 2. Conectar nuevo
            veteraniaSeleccionada.OnStatsChanged += ActualizarBarraXP;
            saludSeleccionada = veteraniaSeleccionada.GetComponent<IHealth>();

            // 3. Gestionar la FOTO
            if (imagenRetratoUI != null)
            {
                if (unidad.retratoCara != null) imagenRetratoUI.sprite = unidad.retratoCara;
                else if (spritePorDefecto != null) imagenRetratoUI.sprite = spritePorDefecto;
                imagenRetratoUI.color = Color.white;
            }

            // 4. Activar Panel y Barras básicas
            if (panelCompleto != null) panelCompleto.SetActive(true);
            if (xpSlider != null) xpSlider.gameObject.SetActive(true);
            if (hpSlider != null) hpSlider.gameObject.SetActive(true);

            // 5. GESTIÓN INTELIGENTE DEL ESCUDO
            CheckEscudo();

            // 6. Actualizar valores iniciales
            ActualizarBarraXP();
            ActualizarStatsCombate();
        }
        else
        {
            if (panelCompleto != null) panelCompleto.SetActive(false);
            saludSeleccionada = null;
        }
    }

    void ActualizarBarraVida()
    {
        if (saludSeleccionada == null || hpSlider == null) return;

        float vida = saludSeleccionada.GetCurrentHealth();
        float max = saludSeleccionada.GetMaxHealth();

        hpSlider.value = vida / max;

        if (hpTexto != null) hpTexto.text = $"{vida}/{max}";
    }

    void CheckEscudo()
    {
        if (shieldSlider == null) return;

        // Preguntamos si la unidad tiene escudo máximo mayor que 0
        if (saludSeleccionada != null && saludSeleccionada.GetMaxShield() > 0)
        {
            shieldSlider.gameObject.SetActive(true); // Activar barra azul
        }
        else
        {
            shieldSlider.gameObject.SetActive(false); // Ocultar barra azul (es un soldado normal)
        }
    }

    void ActualizarStatsCombate()
    {
        if (saludSeleccionada == null) return;

        // --- VIDA (Barra Roja) ---
        if (hpSlider != null)
        {
            float vida = (float)saludSeleccionada.GetCurrentHealth();
            float maxVida = (float)saludSeleccionada.GetMaxHealth();
            // Si maxVida es 0, ponemos 0 para evitar error de división
            hpSlider.value = (maxVida > 0) ? vida / maxVida : 0;

            if (hpTexto != null) hpTexto.text = $"{vida}/{maxVida}";
        }

        // --- ESCUDO (Barra Azul) ---
        if (shieldSlider != null && shieldSlider.gameObject.activeSelf)
        {
            // ¡ESTO ES LO IMPORTANTE! El (float) obliga a usar decimales
            float escudo = (float)saludSeleccionada.GetCurrentShield();
            float maxEscudo = (float)saludSeleccionada.GetMaxShield();

            // Si tiene 25 de 50, el resultado será 0.5 (mitad de barra)
            shieldSlider.value = (maxEscudo > 0) ? escudo / maxEscudo : 0;
        }
    }

    void ActualizarBarraXP()
    {
        if (veteraniaSeleccionada == null || xpSlider == null) return;
        float actual = veteraniaSeleccionada.xpActual;
        float necesario = veteraniaSeleccionada.xpParaSiguienteNivel;
        xpSlider.value = (necesario > 0) ? actual / necesario : 0;
    }
}