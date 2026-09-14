using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; // 1. Tambahkan library Netcode

// 2. Ubah MonoBehaviour menjadi NetworkBehaviour
public class PlayerMovement3D : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 3. Kunci Player Sync: Cek apakah komputer ini adalah pemilik kubus tersebut
        if (!IsOwner) return;

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

        // Arah gerakan di dunia 3D
        Vector3 moveDirection = new Vector3(
            input.x,
            0f,
            input.y
        );

        // Gerakan player
        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );

        // Rotasi player mengikuti arah gerakan
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}