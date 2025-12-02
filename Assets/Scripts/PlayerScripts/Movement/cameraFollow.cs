using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Movimiento Libre con Teclado")]
    public float moveSpeed = 5f;
    private bool isManualControl = true;

    [Header("Límites de la Cámara")]
    public bool useBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    [Header("Zoom (Mouse Scroll)")]
    [Tooltip("Camera a controlar. Se vazio, tentará obter Camera.main ou Camera no mesmo GameObject.")]
    public Camera cam;
    public float zoomSpeed = 5f;
    public float zoomSmoothSpeed = 10f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 10f;
    public float minFOV = 15f;
    public float maxFOV = 60f;

    private float _targetZoom; // orthoSize ou FOV dependendo do tipo
    private bool _isOrthographic = true;

    void Start()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = Camera.main;
        }

        if (cam != null)
        {
            _isOrthographic = cam.orthographic;
            _targetZoom = _isOrthographic ? cam.orthographicSize : cam.fieldOfView;
        }
    }

    void LateUpdate()
    {
        HandleZoomInput();

        if (isManualControl)
        {
            // Movimiento manual con WASD
            Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
            Vector3 desiredPosition = transform.position + moveInput * moveSpeed * Time.deltaTime;

            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            transform.position = desiredPosition;
        }
        else if (target != null)
        {
            // Seguimiento normal del personaje
            Vector3 desiredPosition = target.position + offset;

            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }

        // Aplicar zoom suavemente
        if (cam != null)
        {
            if (_isOrthographic)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, _targetZoom, Time.deltaTime * zoomSmoothSpeed);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _targetZoom, Time.deltaTime * zoomSmoothSpeed);
            }
        }
    }

    void HandleZoomInput()
    {
        if (cam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            // Scroll para cima (positivo) deve aproximar (zoom in): diminuir orthoSize / FOV
            float delta = -scroll * zoomSpeed;

            if (_isOrthographic)
            {
                _targetZoom = Mathf.Clamp(_targetZoom + delta, minOrthoSize, maxOrthoSize);
            }
            else
            {
                _targetZoom = Mathf.Clamp(_targetZoom + delta, minFOV, maxFOV);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}