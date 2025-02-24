using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform player; // Referencia al personaje
    public BoardManager boardManager; // Referencia al BoardManager
    public float moveSpeed = 10f; // Velocidad de movimiento de la cámara
    public float scrollSpeed = 5f; // Velocidad del zoom
    public Vector3 defaultOffset = new Vector3(10, 10, -10); // Offset por defecto
    private Vector3 offset; // Offset actual de la cámara
    private Vector3 targetPosition; // Posición objetivo al seguir al personaje
    private bool followPlayer = true; // Determina si la cámara sigue al personaje
    public float minZoom = 5f; // Mínimo zoom permitido
    public float maxZoom = 50f; // Máximo zoom permitido

    // Límites del tablero
    private float minX, maxX, minZ, maxZ;

    void Start()
    {
        // Inicializar el offset con el valor por defecto
        offset = defaultOffset;

        // Obtener los límites del tablero si el BoardManager está asignado
        if (boardManager != null)
        {
            float halfWidth = boardManager.columns / 2f;
            float halfHeight = boardManager.rows / 2f;

            minX = -halfWidth;
            maxX = halfWidth;
            minZ = -halfHeight - 5f;
            maxZ = halfHeight - 20f;
        }
        else
        {
            Debug.LogWarning("CameraController: No se ha asignado un BoardManager.");
        }

        // Verificar si hay un jugador antes de acceder a su posición
        if (player != null)
        {
            targetPosition = player.position + offset;
            transform.position = targetPosition;
            transform.LookAt(player.position);
        }
        else
        {
            Debug.LogWarning("CameraController: No se ha asignado un jugador todavía.");
        }
    }

    void Update()
    {
        // **Mover la cámara paralela al tablero**
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDirection += Vector3.forward; // Norte
        if (Input.GetKey(KeyCode.S)) moveDirection += Vector3.back; // Sur
        if (Input.GetKey(KeyCode.A)) moveDirection += Vector3.left; // Oeste
        if (Input.GetKey(KeyCode.D)) moveDirection += Vector3.right; // Este

        // Aplicar el movimiento
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // **Restringir la cámara dentro de los límites del tablero**
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, minX, maxX),
            transform.position.y, // No restringimos la altura aquí (se maneja con el zoom)
            Mathf.Clamp(transform.position.z, minZ, maxZ)
        );

        // **Zoom con la rueda del ratón**
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 zoomDirection = transform.forward * scroll * scrollSpeed;
            Vector3 newPosition = transform.position + zoomDirection;

            // **Limitar el zoom dentro de los valores permitidos**
            float currentHeight = newPosition.y;
            if (currentHeight >= minZoom && currentHeight <= maxZoom)
            {
                transform.position = newPosition;
                offset = transform.position - player.position; // Mantener el nuevo zoom en el offset
            }
        }

        // **Volver a seguir al personaje al pulsar "Espacio"**
        if (Input.GetKeyDown(KeyCode.Space))
        {
            followPlayer = true;
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        followPlayer = true;

        if (player != null)
        {
            transform.position = player.position + offset; // Mantiene el zoom actual
            transform.LookAt(player.position);
        }
    }

    void LateUpdate()
    {
        if (player == null) return; // No hacer nada si no hay jugador

        if (followPlayer)
        {
            targetPosition = player.position + offset; // Mantiene el offset actual
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.LookAt(player.position);

            // **Mantener la cámara dentro de los límites al seguir al jugador**
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, minX, maxX),
                transform.position.y,
                Mathf.Clamp(transform.position.z, minZ, maxZ)
            );
        }

        // Si el jugador mueve la cámara con WASD, deja de seguir al personaje
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            followPlayer = false;
        }
    }
}
