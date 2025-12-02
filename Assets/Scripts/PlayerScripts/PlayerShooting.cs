using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 1.0f;
    public int maxAmmo = 10;
    public string weaponName = "G3 RIFLE";

    [Header("Bullet")]
    public float bulletSpeed = 10f;
    public int bulletDamage = 1;

    [Header("Auto-Aim Settings")]
    public float detectionRange = 8f;
    public LayerMask enemyLayerMask;
    public bool autoAimEnabled = true;
    public float aimUpdateRate = 0.2f;

    [Header("Weapon Behavior")]
    public bool burstMode = false;
    public int burstCount = 3;
    public float burstDelay = 0.2f;
    public float accuracy = 0.95f;

    [Header("UI")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI weaponNameText;

    [Header("Range Visualization")]
    public bool showRangeInGame = true;
    public Color rangeColor = new Color(1f, 1f, 0f, 0.3f);
    public Color targetLineColor = Color.red;

    private int currentAmmo;
    private float nextFireTime;
    private Transform currentTarget;
    private Camera mainCam;
    private Coroutine aimCoroutine;
    private bool isBurstFiring = false;
    private LineRenderer rangeCircle;
    private LineRenderer targetLine;

    void Start()
    {
        currentAmmo = maxAmmo;
        mainCam = Camera.main;

        // Verificar y crear weaponPoint si no existe
        if (weaponPoint == null)
        {
            CreateWeaponPoint();
        }

        UpdateUI();

        // Crear visualización del rango
        if (showRangeInGame)
        {
            CreateRangeVisualization();
            CreateTargetLine();
        }

        if (autoAimEnabled)
        {
            aimCoroutine = StartCoroutine(UpdateAimTarget());
        }
    }

    void CreateWeaponPoint()
    {
        GameObject weaponPointObj = new GameObject("WeaponPoint");
        weaponPointObj.transform.SetParent(transform);
        weaponPointObj.transform.localPosition = new Vector3(0.5f, 0.2f, 0);
        weaponPoint = weaponPointObj.transform;

        Debug.Log("WeaponPoint creado automáticamente en: " + weaponPoint.position);
    }

    void Update()
    {
        HandleShooting();

        // Actualizar visualización del objetivo
        if (targetLine != null)
        {
            if (currentTarget != null && autoAimEnabled)
            {
                targetLine.enabled = true;
                targetLine.SetPosition(0, weaponPoint.position);
                targetLine.SetPosition(1, currentTarget.position);
            }
            else
            {
                targetLine.enabled = false;
            }
        }

        // Cambiar entre modo automático y manual
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleAutoAim();
        }
    }

    void CreateRangeVisualization()
    {
        GameObject rangeObject = new GameObject("RangeVisualization");
        rangeObject.transform.SetParent(transform);
        rangeObject.transform.localPosition = Vector3.zero;

        rangeCircle = rangeObject.AddComponent<LineRenderer>();
        rangeCircle.material = new Material(Shader.Find("Sprites/Default"));
        rangeCircle.startColor = rangeColor;
        rangeCircle.endColor = rangeColor;
        rangeCircle.startWidth = 0.05f;
        rangeCircle.endWidth = 0.05f;
        rangeCircle.useWorldSpace = false;
        rangeCircle.loop = true;

        DrawCircle(rangeCircle, detectionRange, 50);
    }

    void CreateTargetLine()
    {
        GameObject lineObject = new GameObject("TargetLine");
        lineObject.transform.SetParent(transform);

        targetLine = lineObject.AddComponent<LineRenderer>();
        targetLine.material = new Material(Shader.Find("Sprites/Default"));
        targetLine.startColor = targetLineColor;
        targetLine.endColor = targetLineColor;
        targetLine.startWidth = 0.03f;
        targetLine.endWidth = 0.03f;
        targetLine.useWorldSpace = true;
        targetLine.positionCount = 2;
        targetLine.enabled = false;
    }

    void DrawCircle(LineRenderer lineRenderer, float radius, int segments)
    {
        lineRenderer.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += 360f / segments;
        }
    }

    void HandleShooting()
    {
        if (autoAimEnabled)
        {
            // Disparo automático cuando hay objetivo
            if (currentTarget != null && Time.time >= nextFireTime && currentAmmo > 0 && !isBurstFiring)
            {
                if (burstMode)
                {
                    StartCoroutine(BurstFire(currentTarget.position));
                }
                else
                {
                    ShootAtTarget(currentTarget.position); // som ativo
                    nextFireTime = Time.time + fireRate;
                }
            }
        }
        else
        {
            // Disparo manual con clic del mouse
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime && currentAmmo > 0 && !isBurstFiring)
            {
                Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = 0;

                if (burstMode)
                {
                    StartCoroutine(BurstFire(mouseWorld));
                }
                else
                {
                    ShootAtTarget(mouseWorld); // som ativo
                    nextFireTime = Time.time + fireRate;
                }
            }
        }

        // Recargar (tecla R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentAmmo = maxAmmo;
            UpdateUI();
        }
    }

    void ShootAtTarget(Vector3 targetPosition, bool playSound = true)
    {
        if (bulletPrefab == null || weaponPoint == null)
        {
            Debug.LogError("Faltan referencias: bulletPrefab o weaponPoint");
            return;
        }

        Debug.Log($"Disparando desde: {weaponPoint.position} hacia: {targetPosition}");

        Vector2 direction = (targetPosition - weaponPoint.position).normalized;

        // Aplicar imprecisión
        if (accuracy < 1.0f)
        {
            float inaccuracy = (1.0f - accuracy) * 2.0f;
            direction.x += Random.Range(-inaccuracy, inaccuracy);
            direction.y += Random.Range(-inaccuracy, inaccuracy);
            direction.Normalize();
        }

        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);

        Debug.Log($"Bala instanciada en: {bullet.transform.position}");

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(direction);
            b.isEnemyBullet = false;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
        }
        else
        {
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * bulletSpeed;
                Debug.Log($"Velocidad de bala: {rb.velocity}");
            }
            else
            {
                Debug.LogError("La bala no tiene componente Bullet ni Rigidbody2D");
            }
        }

        // 🔊 SOM DE DISPARO (pode ser desligado em burst)
        if (playSound && SoundColector.Instance != null)
        {
            if (CompareTag("Tank"))
                SoundColector.Instance.PlayTankShot();
            else
                SoundColector.Instance.PlayInfantryShot();
        }

        currentAmmo--;
        UpdateUI();
    }

    IEnumerator BurstFire(Vector3 targetPosition)
    {
        isBurstFiring = true;

        int shotsFired = 0;
        while (shotsFired < burstCount && currentAmmo > 0)
        {
            bool playSound = (shotsFired == 0); // só o 1º tiro faz som
            ShootAtTarget(targetPosition, playSound);
            shotsFired++;

            if (shotsFired < burstCount)
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

        isBurstFiring = false;
        nextFireTime = Time.time + fireRate;
    }

    IEnumerator UpdateAimTarget()
    {
        while (true)
        {
            FindNearestEnemy();
            yield return new WaitForSeconds(aimUpdateRate);
        }
    }

    void FindNearestEnemy()
    {
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayerMask);

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            if (enemyCollider.CompareTag("Enemy"))
            {
                RaycastHit2D hit = Physics2D.Raycast(
                    transform.position,
                    enemyCollider.transform.position - transform.position,
                    detectionRange,
                    enemyLayerMask | (1 << LayerMask.NameToLayer("Obstacle"))
                );

                if (hit.collider != null && hit.collider.CompareTag("Enemy"))
                {
                    float distance = Vector2.Distance(transform.position, enemyCollider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemyCollider.transform;
                    }
                }
            }
        }

        currentTarget = nearestEnemy;
    }

    void ToggleAutoAim()
    {
        autoAimEnabled = !autoAimEnabled;

        if (autoAimEnabled)
        {
            if (aimCoroutine != null)
                StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(UpdateAimTarget());
            Debug.Log("Auto-aim ACTIVADO");
        }
        else
        {
            if (aimCoroutine != null)
                StopCoroutine(aimCoroutine);
            currentTarget = null;
            if (targetLine != null) targetLine.enabled = false;
            Debug.Log("Auto-aim DESACTIVADO - Modo manual");
        }
    }

    void UpdateUI()
    {
        if (ammoText != null)
            ammoText.text = $"AMMO: {currentAmmo}/{maxAmmo}";
        if (weaponNameText != null)
            weaponNameText.text = weaponName;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (currentTarget != null && autoAimEnabled && weaponPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(weaponPoint.position, currentTarget.position);
        }

        if (weaponPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(weaponPoint.position, 0.1f);
        }
    }

    void OnDestroy()
    {
        if (aimCoroutine != null)
            StopCoroutine(aimCoroutine);
    }
}
