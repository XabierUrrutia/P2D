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
        if (Input.GetMouseButton(0)) // botão direito para disparar
        {
            if (Time.time >= nextFireTime && currentAmmo > 0)
            {
                Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = 0;

                Vector2 direction = (mouseWorld - weaponPoint.position).normalized;

                GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().SetDirection(direction);

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
