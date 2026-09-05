using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement3D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
        {
            input.y += 1f;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            input.y -= 1f;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            input.x -= 1f;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            input.x += 1f;
        }

        input = input.normalized;

        rb.linearVelocity = new Vector3(
            input.x * moveSpeed,
            rb.linearVelocity.y,
            input.y * moveSpeed
        );
    }
}