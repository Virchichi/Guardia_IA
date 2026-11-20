using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] float distance = 5f;


    [SerializeField] float minVerticalAngle = -45;
    [SerializeField] float maxVerticalAngle = 45;



    [SerializeField] Vector2 fraimingOffset;

    [SerializeField] bool invertX;
    [SerializeField] bool invertY;


    [SerializeField] float mouseSensitivityX = 1f;
    [SerializeField] float mouseSensitivityY = 1f;

    [Header("FOV")]
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float runFOV = 75f;
    [SerializeField] float wallRunFOV = 78f;
    [SerializeField] float fovSpeed = 8f;

    Camera cam;
    float targetFOV;

    [Header("Tilt")]
    [SerializeField] float tiltAngle = 15f;
    [SerializeField] float tiltSpeed = 5f;

    float currentTilt;
    float targetTilt;


    [Header("Colisión con obstáculos")]
    [Tooltip("Capas que pueden tapar al jugador (paredes, cajas...). Everything por defecto; " +
             "los colliders del propio jugador y los triggers se ignoran solos.")]
    [SerializeField] LayerMask collisionLayers = ~0;
    [Tooltip("Grosor de la cámara: evita que asome por las esquinas o atraviese la pared.")]
    [SerializeField] float collisionRadius = 0.25f;
    [Tooltip("Separación de la pared: la cámara se coloca un poco DELANTE del obstáculo.")]
    [SerializeField] float collisionPadding = 0.1f;
    [Tooltip("Distancia mínima al jugador aunque el obstáculo esté pegado.")]
    [SerializeField] float minDistance = 0.6f;
    [Tooltip("Velocidad a la que vuelve a su distancia normal cuando deja de haber obstáculo. " +
             "Acercarse es instantáneo (para no ver nunca a través de la pared).")]
    [SerializeField] float returnSpeed = 6f;

    float currentDistance;
    readonly RaycastHit[] hits = new RaycastHit[16];

    float rotationY;
    float rotationX;

    float invertXVal;
    float invertYVal;

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetFOV = normalFOV;
        currentDistance = distance;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // Juego pausado (PanelVictoria): la cámara no gira con el ratón.
        if (Time.timeScale == 0f) return;

        invertXVal = invertX ? -1 : 1;
        invertYVal = invertY ? -1 : 1;
    
        rotationY += Mouse.current.delta.x.ReadValue() * invertYVal * mouseSensitivityY;

        rotationX += Mouse.current.delta.y.ReadValue() * invertXVal * mouseSensitivityX;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);

        var targetRotation = Quaternion.Euler(rotationX, rotationY, currentTilt);

        var focusPosition = followTarget.position + new Vector3(fraimingOffset.x, fraimingOffset.y);

        // Si hay un obstáculo entre el jugador y la cámara, la cámara se pone
        // DELANTE de él para que se vea al jugador con claridad.
        float allowed = DistanceWithoutObstacles(focusPosition, targetRotation * Vector3.back);
        if (allowed < currentDistance)
            currentDistance = allowed;                                   // acercarse: al momento
        else
            currentDistance = Mathf.Lerp(currentDistance, allowed,       // alejarse: suave
                                         1f - Mathf.Exp(-returnSpeed * Time.deltaTime));

        transform.position = focusPosition - targetRotation * new Vector3(0, 0, currentDistance);
        transform.rotation = targetRotation;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }
    /// <summary>
    /// Lanza una esfera desde el jugador hacia atrás (donde quiere estar la
    /// cámara). Devuelve la distancia a la que puede ponerse sin quedar detrás
    /// de un obstáculo. Ignora los colliders del propio jugador y los triggers.
    /// </summary>
    float DistanceWithoutObstacles(Vector3 focus, Vector3 backDir)
    {
        int count = Physics.SphereCastNonAlloc(focus, collisionRadius, backDir, hits,
                                               distance, collisionLayers, QueryTriggerInteraction.Ignore);
        float nearest = distance;
        Transform playerRoot = followTarget.root;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hits[i];
            if (h.collider.transform.root == playerRoot) continue;   // el propio jugador
            if (h.distance <= 0f) continue;                          // ya estaba dentro al empezar
            if (h.distance < nearest) nearest = h.distance;
        }
        if (nearest < distance) nearest -= collisionPadding;
        return Mathf.Clamp(nearest, minDistance, distance);
    }

    public void SetFOVState(bool isRunning, bool isWallRunning)
    {
        if (isWallRunning)
            targetFOV = wallRunFOV;
        else if (isRunning)
            targetFOV = runFOV;
        else
            targetFOV = normalFOV;
    }
    public void SetTilt(bool wallRight, bool wallLeft)
    {
        if (wallRight)
            targetTilt = tiltAngle;
        else if (wallLeft)
            targetTilt = -tiltAngle;
        else
            targetTilt = 0f;
    }
    //Esto es una Propiedad que devuelve una rotacion en el plano horizontal, es decir, solo con la rotacion en Y, para que el player se mueva en esa direccion
    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);
}
