using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform player;
    public BoardManager boardManager;
    public float moveSpeed = 10f;
    public float scrollSpeed = 5f;
    public Vector3 defaultOffset = new Vector3(10, 10, -10);
    private Vector3 offset;
    private Vector3 targetPosition;
    private bool followPlayer = true;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    private float minX, maxX, minZ, maxZ;

    void Start()
    {
        offset = defaultOffset;

        if (boardManager != null)
        {
            float halfWidth = boardManager.columns / 2f;
            float halfHeight = boardManager.rows / 2f;

            minX = -halfWidth;
            maxX = halfWidth;
            minZ = -halfHeight - 5f;
            maxZ = halfHeight - 15f;
        }
        else
        {
            Debug.LogWarning("CameraController: No se ha asignado un BoardManager.");
        }

        if (player != null)
        {
            targetPosition = player.position + offset;
            transform.position = targetPosition;
        }
    }

    void Update()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A)) moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D)) moveDirection += Vector3.right;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        ClampPosition();

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 zoomDirection = transform.forward * scroll * scrollSpeed;
            Vector3 newPosition = transform.position + zoomDirection;

            float newHeight = newPosition.y;
            if (newHeight >= minZoom && newHeight <= maxZoom)
            {
                transform.position = newPosition;
                if (player != null)
                {
                    offset = transform.position - player.position;
                }
            }
            ClampPosition();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            followPlayer = true;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (followPlayer)
        {
            targetPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            ClampPosition();
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            followPlayer = false;
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        followPlayer = true;

        if (player != null)
        {
            transform.position = player.position + offset;
            ClampPosition();
        }
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }
}
