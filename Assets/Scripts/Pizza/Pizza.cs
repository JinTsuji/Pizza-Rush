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
        if (holdPoint == null)
        {
            Debug.LogError("Hold Point belum diatur!");
            return;
        }

        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

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

        if (spawner != null)
        {
            spawner.PizzaDropped(gameObject);
        }
    }

    public void Serve()
    {
        if (spawner != null)
        {
            spawner.PizzaServed();
        }

        Destroy(gameObject);
    }
}