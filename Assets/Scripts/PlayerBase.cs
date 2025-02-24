using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    protected Animator animator;
    public Vector2Int currentTilePos; // Mover esto aquí

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
    }

    public virtual void TakeDamage()
    {
        animator.SetTrigger("GetDamageTrigger");
        Debug.Log(name + " ha recibido daño.");
    }
}


