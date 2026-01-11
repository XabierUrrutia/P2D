using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// BuildingManager completo e funcional:
/// - SelectBuilding(BuildingData) inicia modo one-shot de colocação.
/// - Segura o botão configurado (por defeito mouse direito) para ver preview; ao soltar tenta colocar.
/// - Valida: raio da base, FogOfWar (não permite colocar em fog preta), overlap com blockingLayers.
/// - Preview azul (válido) / vermelho (inválido). Suporta sprites e meshes.
/// - Custo e tempo de construção com coroutine; durante a construção scripts ficam desativados e animação do estado é aplicada.
/// </summary>
public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Base e área de construção")]
    public Transform militaryBase;
    [Tooltip("Raio onde é permitida a construção (unidades do mundo)")]
    public float buildRadius = 10f;

    [Header("Layers e validação")]
    [Tooltip("Layers que bloqueiam a colocação (ex.: Ground, Buildings)")]
    public LayerMask blockingLayers;
    public float placeZ = 0f;

    [Header("Preview")]
    public Color previewValidColor = new Color(0f, 0.55f, 1f, 0.6f);
    public Color previewInvalidColor = new Color(1f, 0f, 0f, 0.6f);
    public Color constructionColor = new Color(1f, 0.9f, 0.5f, 0.9f);

    [Header("Input / comportamento")]
    [Tooltip("0 = left, 1 = right, 2 = middle")]
    public int placementMouseButton = 1; // right click
    public bool cancelWithEsc = true;
    public bool ignoreWhenPointerOverUI = true;

    [Header("Grid snap")]
    public bool enableGridSnap = true;
    public Vector2 defaultGridSize = Vector2.one;

    [Header("Recursos")]
    public bool requireResources = true;
    // OBS: O saldo agora é gerido pelo MoneyManager central. Não usar playerResources local.

    [Header("Fog of War")]
    [Tooltip("Referencia ao FogOfWar (se vazio tenta encontrar na cena)")]
    public FogOfWar fogOfWar;

    [Header("Construção - Animator")]
    [Tooltip("Nome do parâmetro bool no Animator para marcar 'under construction' (se existir)")]
    public string animatorUnderConstructionBool = "UnderConstruction";
    [Tooltip("Pulso fallback (se não houver Animator)")]
    public float constructionPulseScale = 0.06f;
    public float constructionPulseSpeed = 3.5f;

    // runtime
    private BuildingData selectedBuildingData;
    private GameObject currentPrefab;
    private GameObject previewInstance;
    private bool isPlacing = false;
    private Camera mainCam;

    // caches
    private Dictionary<Renderer, Color> previewOriginalColors = new Dictionary<Renderer, Color>();
    private List<Material> previewInstantiatedMaterials = new List<Material>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogWarning($"BuildingManager: instância duplicada em '{name}', destruindo componente.");
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        mainCam = Camera.main;
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();
    }

    void Update()
    {
        if (!isPlacing || currentPrefab == null) return;

        // garante que o jogo não fica pausado enquanto colocas
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        if (cancelWithEsc && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacing();
            return;
        }

        if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (previewInstance != null) previewInstance.SetActive(false);
            return;
        }

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = placeZ;

        // mostrar preview ao pressionar o botão
        if (Input.GetMouseButtonDown(placementMouseButton))
        {
            if (previewInstance != null) previewInstance.SetActive(true);
        }

        // enquanto segura, move o preview e atualiza visual
        if (Input.GetMouseButton(placementMouseButton))
        {
            if (previewInstance == null) return;

            Vector3 targetPos = mouseWorld;
            if (enableGridSnap && selectedBuildingData != null)
            {
                Vector2 grid = selectedBuildingData.gridSize;
                if (grid == Vector2.zero) grid = defaultGridSize;
                targetPos = ApplyGridSnap(targetPos, grid);
            }

            previewInstance.transform.position = targetPos;

            bool valid = IsValidPlacement(targetPos, out Collider2D[] overlapping);
            UpdatePreviewVisual(valid);
        }

        // ao soltar: tenta colocar (one-shot)
        if (Input.GetMouseButtonUp(placementMouseButton))
        {
            if (previewInstance != null && previewInstance.activeSelf)
            {
                Vector3 placePos = previewInstance.transform.position;
                bool valid = IsValidPlacement(placePos, out Collider2D[] overlapping);
                if (valid)
                {
                    int cost = selectedBuildingData != null ? selectedBuildingData.cost : 0;
                    if (requireResources)
                    {
                        if (!TrySpendResources(cost))
                        {
                            Debug.Log("[BuildingManager] Recursos insuficientes.");
                        }
                        else
                        {
                            GameObject placed = Instantiate(currentPrefab, placePos, Quaternion.identity);
                            placed.name = currentPrefab.name;
                            StartCoroutine(ConstructBuildingRoutine(placed, selectedBuildingData.buildTime));
                        }
                    }
                    else
                    {
                        GameObject placed = Instantiate(currentPrefab, placePos, Quaternion.identity);
                        placed.name = currentPrefab.name;
                        StartCoroutine(ConstructBuildingRoutine(placed, selectedBuildingData.buildTime));
                    }
                }
                else
                {
                    Debug.Log("[BuildingManager] Local inválido para construir (raio, fog ou overlap).");
                }
            }

            // limpa seleção (one-shot)
            CancelPlacing();
        }
    }

    // UI chama isto
    public void SelectBuilding(BuildingData buildingData)
    {
        if (buildingData == null)
        {
            Debug.LogWarning("BuildingManager: buildingData nulo.");
            return;
        }

        if (buildingData.buildingPrefab == null)
        {
            Debug.LogWarning($"BuildingManager: prefab não definido no BuildingData '{buildingData.buildingName}'.");
            return;
        }

        StartPlacing(buildingData);
    }

    public void StartPlacing(BuildingData data)
    {
        CancelPlacing();

        selectedBuildingData = data;
        currentPrefab = data.buildingPrefab;
        isPlacing = true;

        previewInstance = Instantiate(currentPrefab);
        previewInstance.name = currentPrefab.name + "_Preview";
        DisableRuntimeComponents(previewInstance);
        CacheRendererColors(previewInstance, previewOriginalColors);
        ApplyColorToPreview(previewInvalidColor);
        previewInstance.SetActive(false);

        Debug.Log($"BuildingManager: colocação iniciada para '{data.buildingName}'. Use o botão configurado para preview/colocar.");
    }

    public void CancelPlacing()
    {
        if (previewInstance != null)
        {
            RestoreRendererColors(previewOriginalColors);
            Destroy(previewInstance);
        }

        previewInstance = null;
        selectedBuildingData = null;
        currentPrefab = null;
        isPlacing = false;

        foreach (var m in previewInstantiatedMaterials) if (m != null) Destroy(m);
        previewInstantiatedMaterials.Clear();
        previewOriginalColors.Clear();
    }

    // Substitui o sistema local por consumo via MoneyManager
    public bool TrySpendResources(int amount)
    {
        if (amount <= 0) return true;
        if (!requireResources) return true;

        if (MoneyManager.Instance != null)
        {
            // MoneyManager.SpendMoney retorna true se conseguiu gastar
            return MoneyManager.Instance.SpendMoney(amount);
        }
        else
        {
            Debug.LogWarning("[BuildingManager] MoneyManager não encontrado. Permitindo construção sem custo (modo fallback).");
            return true;
        }
    }

    IEnumerator ConstructBuildingRoutine(GameObject placed, float buildTime)
    {
        if (placed == null) yield break;

        // cache
        var constructionOriginalColors = new Dictionary<Renderer, Color>();
        var constructionInstMaterials = new List<Material>();
        CacheRendererColors(placed, constructionOriginalColors);

        // garantir colliders ativos (ocupar espaço)
        var cols = placed.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = true;

        // desativa logic scripts
        var behaviours = placed.GetComponentsInChildren<MonoBehaviour>(true);
        var toDisable = new List<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            toDisable.Add(b);
            b.enabled = false;
        }

        // aplicar cor de construção
        ApplyColorToRenderers(placed, constructionColor, constructionInstMaterials);

        // tentar animator
        Animator anim = placed.GetComponentInChildren<Animator>();
        bool animatorParamSet = false;
        if (anim != null && AnimatorHasBoolParameter(anim, animatorUnderConstructionBool))
        {
            anim.SetBool(animatorUnderConstructionBool, true);
            animatorParamSet = true;
        }

        // fallback pulse
        Coroutine pulse = null;
        if (!animatorParamSet)
            pulse = StartCoroutine(ConstructionPulseRoutine(placed.transform));

        float t = 0f;
        while (t < buildTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // terminar animação
        if (animatorParamSet && anim != null) anim.SetBool(animatorUnderConstructionBool, false);
        if (pulse != null) StopCoroutine(pulse);

        // restaurar e ativar scripts
        RestoreRendererColors(constructionOriginalColors);
        foreach (var b in toDisable) if (b != null) b.enabled = true;
        foreach (var m in constructionInstMaterials) if (m != null) Destroy(m);

        Debug.Log($"[BuildingManager] Construção concluída: {placed.name}");    }

    IEnumerator ConstructionPulseRoutine(Transform target)
    {
        if (target == null) yield break;
        Vector3 baseScale = target.localScale;
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime * constructionPulseSpeed;
            float s = 1f + Mathf.Sin(elapsed) * constructionPulseScale;
            target.localScale = baseScale * s;
            yield return null;
        }
    }

    // Validações principais
    bool IsValidPlacement(Vector3 worldPos, out Collider2D[] overlapping)
    {
        overlapping = null;

        // dentro do raio da base
        if (militaryBase != null)
        {
            float d = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(militaryBase.position.x, militaryBase.position.y));
            if (d > buildRadius) return false;
        }

        // Fog of War: não permitir em black fog (não visitado)
        if (fogOfWar != null)
        {
            // usa o método combinado público para evitar chamadas ambíguas
            if (!fogOfWar.IsPositionVisitedOrVisible(worldPos))
            {
                return false;
            }
        }

        // overlap usando bounds do preview
        if (previewInstance != null)
        {
            Renderer rend = previewInstance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Vector2 center = rend.bounds.center;
                Vector2 size = rend.bounds.size;
                if (size.sqrMagnitude < 0.0001f) size = Vector2.one * 0.5f;

                float angle = previewInstance.transform.eulerAngles.z;
                overlapping = Physics2D.OverlapBoxAll(center, size, angle, blockingLayers);
                if (overlapping != null && overlapping.Length > 0) return false;
                return true;
            }
        }

        overlapping = Physics2D.OverlapCircleAll(new Vector2(worldPos.x, worldPos.y), 0.5f, blockingLayers);
        return overlapping == null || overlapping.Length == 0;
    }

    void UpdatePreviewVisual(bool valid)
    {
        if (previewInstance == null) return;
        ApplyColorToPreview(valid ? previewValidColor : previewInvalidColor);
    }

    // Helpers: cache / restore / apply colors
    void CacheRendererColors(GameObject obj, Dictionary<Renderer, Color> dict)
    {
        dict.Clear();
        var spriteRends = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in spriteRends) dict[r] = r.color;

        var meshRends = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in meshRends)
        {
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                dict[r] = r.sharedMaterial.color;
        }
    }

    void RestoreRendererColors(Dictionary<Renderer, Color> dict)
    {
        foreach (var kv in dict)
        {
            var rend = kv.Key;
            if (rend == null) continue;

            if (rend is SpriteRenderer sr) sr.color = kv.Value;
            else
            {
                var mr = rend as Renderer;
                if (mr != null)
                {
                    for (int i = 0; i < mr.materials.Length; i++)
                    {
                        if (mr.materials[i] != null && mr.materials[i].HasProperty("_Color"))
                            mr.materials[i].color = kv.Value;
                    }
                }
            }
        }
    }

    void ApplyColorToPreview(Color target)
    {
        if (previewInstance == null) return;

        var spriteRends = previewInstance.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in spriteRends)
        {
            Color orig = previewOriginalColors.ContainsKey(r) ? previewOriginalColors[r] : r.color;
            float finalAlpha = orig.a * target.a;
            r.color = new Color(target.r, target.g, target.b, finalAlpha);
        }

        var meshRends = previewInstance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in meshRends)
        {
            Material[] mats = mr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat != null && mat.HasProperty("_Color"))
                {
                    Material inst = new Material(mat);
                    Color orig = previewOriginalColors.ContainsKey(mr) ? previewOriginalColors[mr] : mat.color;
                    inst.color = new Color(target.r, target.g, target.b, orig.a * target.a);
                    mats[i] = inst;
                    previewInstantiatedMaterials.Add(inst);
                }
            }
            mr.materials = mats;
        }
    }

    void ApplyColorToRenderers(GameObject obj, Color target, List<Material> outInstantiated)
    {
        if (obj == null) return;

        var spriteRends = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in spriteRends)
        {
            r.color = new Color(target.r, target.g, target.b, target.a * r.color.a);
        }

        var meshRends = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in meshRends)
        {
            Material[] mats = mr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat != null && mat.HasProperty("_Color"))
                {
                    Material inst = new Material(mat);
                    inst.color = new Color(target.r, target.g, target.b, target.a * mat.color.a);
                    mats[i] = inst;
                    outInstantiated.Add(inst);
                }
            }
            mr.materials = mats;
        }
    }

    void DisableRuntimeComponents(GameObject obj)
    {
        var cols = obj.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = false;

        var rbs = obj.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (var r in rbs) r.simulated = false;

        var behaviours = obj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            b.enabled = false;
        }
    }

    Vector3 ApplyGridSnap(Vector3 pos, Vector2 grid)
    {
        if (grid.x <= 0f) grid.x = 1f;
        if (grid.y <= 0f) grid.y = 1f;
        float x = Mathf.Round(pos.x / grid.x) * grid.x;
        float y = Mathf.Round(pos.y / grid.y) * grid.y;
        return new Vector3(x, y, pos.z);
    }

    bool AnimatorHasBoolParameter(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == UnityEngine.AnimatorControllerParameterType.Bool && p.name == paramName)
                return true;
        }
        return false;
    }

    public bool IsPlacing() => isPlacing;
}