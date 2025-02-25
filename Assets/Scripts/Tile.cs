using UnityEngine;

public class Tile : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public float tileSize;

    public bool discovered = false;
    private TileObject tileObject;

    private Renderer rend;
    private Collider col;

    private Color originalColor;
    public Color highlightColor = Color.yellow;
    public Color highlightAttack = Color.yellow;
    private Color hiddenColor = new Color(0, 0, 0, 1f); // Negro opaco
    private Color transparentColor = new Color(1f, 1f, 1f, 0.1f); // Blanco con % de opacidad

    void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        // Crear y configurar el material para transparencia
        Material transparentMat = new Material(Shader.Find("Unlit/Transparent"));
        transparentMat.color = transparentColor;
        rend.material = transparentMat;  // Asignar el material transparente

        SetDiscovered(false);  // Iniciar el tile oculto
    }




    public void SetDiscovered(bool state)
    {
        discovered = state;

        rend.material = null;

        if (state)
        {
            rend.material.color = transparentColor;
        }
        else
        {
            rend.material.color = hiddenColor;
        }


        if (col != null)
        {
            col.enabled = state;
        }

        if (tileObject != null && state && !tileObject.isDiscovered)
        {
            tileObject.Discover();
        }
    }


    public void AssignTileObject(TileObject obj)
    {
        tileObject = obj;
        tileObject.AddParentTile(this);
    }

    private void Highlight(Color color)
    {
        if (discovered && rend != null)
        {
            rend.material.color = color; // Resaltar con el color especificado
        }
    }

    public void HighlightWalk()
    {
        Highlight(Color.yellow); // Resaltar en amarillo
    }

    public void HighlightAttack()
    {
        Highlight(Color.red); // Resaltar en amarillo
    }

    public void UnHighlight()
    {
        if (discovered && rend != null)
        {
            rend.material.color = transparentColor; // Volver a la transparencia al dejar de resaltar
        }
    }
}
