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
    public float zoomSmoothSpeed = 10f;

    [Tooltip("Níveis de zoom ortográfico (4 pontos). Se vazio, será preenchido automaticamente.")]
    public float[] orthoZoomLevels = new float[4] { 3f, 5f, 7f, 10f };

    [Tooltip("Níveis de zoom em FOV (4 pontos). Se vazio, será preenchido automaticamente.")]
    public float[] fovZoomLevels = new float[4] { 25f, 35f, 45f, 60f };

    public float minOrthoSize = 2f;
    public float maxOrthoSize = 10f;
    public float minFOV = 15f;
    public float maxFOV = 60f;

    private int _currentZoomLevel = 1; // começa num nível intermédio
    private float _targetZoom;         // orthoSize ou FOV dependendo do tipo
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

            // Garantir 4 níveis válidos
            if (orthoZoomLevels == null || orthoZoomLevels.Length != 4)
                orthoZoomLevels = new float[4] { 3f, 5f, 7f, 10f };

            if (fovZoomLevels == null || fovZoomLevels.Length != 4)
                fovZoomLevels = new float[4] { 25f, 35f, 45f, 60f };

            // Clampar níveis dentro dos min/max
            for (int i = 0; i < orthoZoomLevels.Length; i++)
                orthoZoomLevels[i] = Mathf.Clamp(orthoZoomLevels[i], minOrthoSize, maxOrthoSize);

            for (int i = 0; i < fovZoomLevels.Length; i++)
                fovZoomLevels[i] = Mathf.Clamp(fovZoomLevels[i], minFOV, maxFOV);

            // Definir target inicial no nível atual
            if (_isOrthographic)
            {
                _targetZoom = orthoZoomLevels[Mathf.Clamp(_currentZoomLevel, 0, orthoZoomLevels.Length - 1)];
                cam.orthographicSize = _targetZoom;
            }
            else
            {
                _targetZoom = fovZoomLevels[Mathf.Clamp(_currentZoomLevel, 0, fovZoomLevels.Length - 1)];
                cam.fieldOfView = _targetZoom;
            }
        }
    }

    void LateUpdate()
    {
        // NÃO chamamos mais HandleZoomInput se estamos a usar novo sistema para zoom,
        // mas se ainda usas o scroll antigo, podes chamar aqui:
        HandleZoomInput();

        if (!isManualControl && target != null)
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

        // Aplicar zoom suavemente (continua igual)
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
            // Scroll para cima (positivo) aproxima (zoom in): passar para nível mais "perto" (índice menor)
            if (scroll > 0f)
                _currentZoomLevel--;
            else
                _currentZoomLevel++;

            if (_isOrthographic)
            {
                _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, orthoZoomLevels.Length - 1);
                _targetZoom = orthoZoomLevels[_currentZoomLevel];
            }
            else
            {
                _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, fovZoomLevels.Length - 1);
                _targetZoom = fovZoomLevels[_currentZoomLevel];
            }
        }
    }

    public void ManualMove(Vector2 input)
    {
        isManualControl = true;

        Vector3 moveInput = new Vector3(input.x, input.y, 0f);
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 desiredPosition = transform.position + moveInput * moveSpeed * Time.deltaTime;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        transform.position = desiredPosition;
    }

    public void ManualZoom(float scrollDelta)
    {
        if (cam == null)
            return;

        // Em vez de usar delta contínuo, vamos só mudar de nível com base no sinal do scrollDelta
        if (Mathf.Abs(scrollDelta) <= 0.0001f)
            return;

        if (scrollDelta > 0f)
            _currentZoomLevel--;
        else
            _currentZoomLevel++;

        if (_isOrthographic)
        {
            _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, orthoZoomLevels.Length - 1);
            _targetZoom = orthoZoomLevels[_currentZoomLevel];
        }
        else
        {
            _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, fovZoomLevels.Length - 1);
            _targetZoom = fovZoomLevels[_currentZoomLevel];
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