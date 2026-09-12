using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactDistance = 2f;

    [Header("Pizza")]
    [SerializeField] private Transform holdPoint;

    private Pizza heldPizza;

    private void Update()
    {
        // Ambil pizza
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupPizza();
        }

        // Jatuhkan pizza
        if (Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryDropPizza();
        }
    }

    private void TryPickupPizza()
    {
        // Kalau sudah membawa pizza, jangan ambil lagi
        if (heldPizza != null)
        {
            Debug.Log("Player sudah membawa pizza.");
            return;
        }

        // Cari semua Collider di sekitar Player
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactDistance
        );

        Pizza closestPizza = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            Pizza pizza = col.GetComponentInParent<Pizza>();

            if (pizza == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                pizza.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPizza = pizza;
            }
        }

        // Tidak ada pizza di sekitar Player
        if (closestPizza == null)
        {
            Debug.Log("Tidak ada pizza di dekat Player.");
            return;
        }

        // Ambil pizza terdekat
        heldPizza = closestPizza;

        heldPizza.PickUp(holdPoint);

        Debug.Log(
            "Pizza berhasil diambil: " +
            heldPizza.pizzaType
        );
    }

    private void TryDropPizza()
    {
        if (heldPizza == null)
        {
            Debug.Log("Player tidak sedang membawa pizza.");
            return;
        }

        heldPizza.Drop();

        Debug.Log(
            "Pizza dijatuhkan: " +
            heldPizza.pizzaType
        );

        heldPizza = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactDistance
        );
    }
}