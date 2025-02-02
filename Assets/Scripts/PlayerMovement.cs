using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private FogOfWarManager fogManager;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        fogManager = FindFirstObjectByType<FogOfWarManager>(); // Cacheamos la referencia
        fogManager.RevealArea(transform.position, 10);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Clic izquierdo
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) // Detecta colisión con el suelo
            {
                agent.SetDestination(hit.point); // Mueve al jugador
            }
        }

        if (agent.hasPath)
        {
            fogManager.RevealArea(transform.position, 5);
        }
    }
}
