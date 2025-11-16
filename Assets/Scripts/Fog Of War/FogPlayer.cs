using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog Settings")]
    public float visionRadius = 15f;

    [Header("Update Settings")]
    public float updateThreshold = 0.05f;
    public float forceUpdateInterval = 0.1f;

    private FogOfWar fogOfWar;
    private Vector3 lastPosition;
    private float lastUpdateTime;
    private bool isInitialized = false;

    void Start()
    {
        if (gameObject.tag != "Player")
            gameObject.tag = "Player";

        InitializeFogSystem();
        lastPosition = transform.position;
        lastUpdateTime = Time.time;
    }

    void Update()
    {
        if (!isInitialized) return;

        bool hasMoved = Vector3.Distance(transform.position, lastPosition) > updateThreshold;
        bool needsForceUpdate = Time.time - lastUpdateTime >= forceUpdateInterval;

        if (hasMoved || needsForceUpdate)
        {
            if (fogOfWar != null)
            {
                fogOfWar.RequestUpdate();
                lastPosition = transform.position;
                lastUpdateTime = Time.time;

                // Debug de posición
                Debug.Log($"{name} movido a: {transform.position}");
            }
        }
    }

    private void InitializeFogSystem()
    {
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();

        if (fogOfWar == null)
        {
            Invoke("InitializeFogSystem", 0.1f);
            return;
        }

        fogOfWar.RegisterPlayer(this);
        isInitialized = true;

        Debug.Log($"FogPlayer inicializado: {name} en posición: {transform.position}");
    }

    public void SetFogOfWar(FogOfWar newFogOfWar)
    {
        fogOfWar = newFogOfWar;
        if (!isInitialized)
        {
            fogOfWar.RegisterPlayer(this);
            isInitialized = true;
        }
    }

    public void SetVisionRadius(float newRadius)
    {
        visionRadius = newRadius;
        if (fogOfWar != null && isInitialized)
        {
            fogOfWar.RequestUpdate();
        }
    }

    void OnDestroy()
    {
        if (fogOfWar != null && isInitialized)
        {
            fogOfWar.UnregisterPlayer(this);
        }
    }

    void OnEnable()
    {
        if (fogOfWar != null && !isInitialized)
        {
            fogOfWar.RegisterPlayer(this);
            isInitialized = true;
            fogOfWar.RequestUpdate();
        }
    }

    void OnDisable()
    {
        if (fogOfWar != null && isInitialized)
        {
            fogOfWar.UnregisterPlayer(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
}