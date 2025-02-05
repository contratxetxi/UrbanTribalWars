using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Posición en la cuadrícula")]
    public int gridX;
    public int gridY;
    public float tileSize;

    [HideInInspector]
    public bool discovered = false;   // Estado de exploración

    private Renderer rend;
    private Color originalColor;
    public Color highlightColor = Color.yellow;  // Color para resaltar la casilla

    void Awake()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Color diferente si está descubierto o no
            Gizmos.color = discovered ? Color.green : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireCube(transform.position, new Vector3(tileSize, 0.1f, tileSize));
        }
    }


    // Configura si la casilla está descubierta o no.
    public void SetDiscovered(bool state)
    {
        discovered = state;
        // // Se muestra u oculta la casilla cambiando la visibilidad del renderer.
        // if (rend != null)
        //     rend.enabled = state;
        // // Además se habilita o deshabilita el collider para impedir la interacción en casillas ocultas.
        // Collider col = GetComponent<Collider>();
        // if (col != null)
        //     col.enabled = state;

        gameObject.SetActive(state); // Mejora el rendimiento al desactivar el GameObject completo
    }

    // Cambia el color de la casilla para resaltar el camino.
    public void Highlight()
    {
        if (rend != null)
            rend.material.color = highlightColor;
    }

    // Restaura el color original.
    public void UnHighlight()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }
}
