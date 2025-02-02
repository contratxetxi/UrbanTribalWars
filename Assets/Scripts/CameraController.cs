using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; // Referencia al personaje
    public float moveSpeed = 10f; // Velocidad de movimiento de la cámara
    public float scrollSpeed = 5f; // Velocidad del zoom
    public Vector3 defaultOffset = new Vector3(10, 10, -10); // Offset por defecto
    private Vector3 offset; // Offset actual de la cámara
    private Vector3 targetPosition; // Posición objetivo al seguir al personaje
    private bool followPlayer = true; // Determina si la cámara sigue al personaje

    void Start()
    {
        // Inicializamos el offset con el valor por defecto
        offset = defaultOffset;
        targetPosition = player.position + offset;
        transform.position = targetPosition;
        transform.LookAt(player.position);
    }

    void Update()
    {
        // **Mover la cámara libremente**
        if (Input.GetKey(KeyCode.W)) transform.position += transform.forward * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) transform.position -= transform.forward * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) transform.position -= transform.right * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) transform.position += transform.right * moveSpeed * Time.deltaTime;

        // **Zoom con la rueda del ratón**
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            offset += transform.forward * scroll * scrollSpeed;
        }

        // **Volver a seguir al personaje al pulsar "Espacio"**
        if (Input.GetKeyDown(KeyCode.Space))
        {
            followPlayer = true;
        }
    }

    void LateUpdate()
    {
        if (followPlayer)
        {
            targetPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.LookAt(player.position);
        }

        // Si el jugador mueve la cámara con WASD, deja de seguir al personaje
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            followPlayer = false;
        }
    }
}
