using UnityEngine;
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 0.3f;
    public int maxAmmo = 10;
    public string weaponName = "G3 Rifle";

    // Adicionados para permitir configurar dano/velocidade da bala
    [Header("Bullet")]
    public float bulletSpeed = 10f;
    public int bulletDamage = 1;

    [Header("UI")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI weaponNameText;

    private int currentAmmo;
    private float nextFireTime;
    private Camera mainCam;

    void Start()
    {
        currentAmmo = maxAmmo;
        mainCam = Camera.main;
        UpdateUI();
    }

    void Update()
    {
        HandleShooting();
    }

    void HandleShooting()
    {
        if (Input.GetMouseButton(0)) // botão esquerdo para disparar
        {
            if (Time.time >= nextFireTime && currentAmmo > 0)
            {
                if (bulletPrefab == null || weaponPoint == null) return;

                Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = 0;

                Vector2 direction = (mouseWorld - weaponPoint.position).normalized;

                GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);
                Bullet b = bullet.GetComponent<Bullet>();
                if (b != null)
                {
                    b.SetDirection(direction);
                    b.isEnemyBullet = false; // garantir origem do projétil
                    b.damage = bulletDamage;
                    b.speed = bulletSpeed;
                }
                else
                {
                    // fallback: definir velocidade diretamente se não houver componente Bullet
                    Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                    if (rb != null)
                        rb.velocity = direction * bulletSpeed;
                }

                currentAmmo--;
                nextFireTime = Time.time + fireRate;
                UpdateUI();
            }
        }

        // Recarregar (tecla R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentAmmo = maxAmmo;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (ammoText != null)
            ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
        if (weaponNameText != null)
            weaponNameText.text = weaponName;
    }
}