using UnityEngine;

public class Pizza : MonoBehaviour
{
    public PizzaType pizzaType;

    private PizzaSpawner spawner;

    public void SetSpawner(PizzaSpawner pizzaSpawner)
    {
        spawner = pizzaSpawner;
    }

    public void PickUp(Transform holdPoint)
    {
        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        // Memberitahu spawner bahwa pizza sudah diambil
        if (spawner != null)
        {
            spawner.PizzaTaken();
        }
    }

    public void Drop()
    {
        transform.SetParent(null);

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }
    }
}