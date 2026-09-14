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
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleInteraction();
        }

        if (Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryDropPizza();
        }
    }

    private void HandleInteraction()
    {
        // Jika sedang membawa pizza,
        // E digunakan untuk memberikan pizza ke customer.
        if (heldPizza != null)
        {
            TryServeCustomer();
        }
        else
        {
            // Jika tidak membawa pizza,
            // E digunakan untuk mengambil pizza.
            TryPickupPizza();
        }
    }

    private void TryPickupPizza()
    {
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

        if (closestPizza == null)
        {
            Debug.Log("Tidak ada pizza di dekat Player.");
            return;
        }

        heldPizza = closestPizza;

        heldPizza.PickUp(holdPoint);

        Debug.Log(
            "Pizza berhasil diambil: " +
            heldPizza.pizzaType
        );
    }

    private void TryServeCustomer()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactDistance
        );

        Customer closestCustomer = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            Customer customer =
                col.GetComponentInParent<Customer>();

            if (customer == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                customer.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCustomer = customer;
            }
        }

        if (closestCustomer == null)
        {
            Debug.Log("Tidak ada Customer di dekat Player.");
            return;
        }

        Pizza pizzaToServe = heldPizza;

        bool success = closestCustomer.ReceivePizza(
            pizzaToServe
        );

        if (success)
        {
            heldPizza = null;
        }
    }

    private void TryDropPizza()
    {
        if (heldPizza == null)
        {
            Debug.Log("Player tidak sedang membawa pizza.");
            return;
        }

        Pizza pizzaToDrop = heldPizza;

        pizzaToDrop.Drop();

        Debug.Log(
            "Pizza dijatuhkan: " +
            pizzaToDrop.pizzaType
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