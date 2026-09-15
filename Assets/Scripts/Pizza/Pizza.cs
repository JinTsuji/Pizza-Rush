using UnityEngine;
using Unity.Netcode;

public class Pizza : NetworkBehaviour
{
    public PizzaType pizzaType;

    private PizzaSpawner spawner;

    // HoldPoint yang sedang diikuti Pizza
    private Transform followTarget;

    public void SetSpawner(PizzaSpawner pizzaSpawner)
    {
        spawner = pizzaSpawner;
    }

    private void LateUpdate()
    {
        // Hanya Server yang mengatur posisi Pizza.
        if (!IsServer)
            return;

        // Kalau sedang dibawa Player,
        // Pizza mengikuti HoldPoint.
        if (followTarget != null)
        {
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
        }
    }

    public bool PickUpServer(Transform holdPoint)
    {
        if (!IsServer)
            return false;

        if (holdPoint == null)
        {
            Debug.LogError("Hold Point belum diatur!");
            return false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        // Simpan HoldPoint sebagai target yang harus diikuti
        followTarget = holdPoint;

        // Langsung pindahkan Pizza ke HoldPoint
        transform.position = holdPoint.position;
        transform.rotation = holdPoint.rotation;

        if (spawner != null)
        {
            spawner.PizzaTaken();
        }

        Debug.Log(
            pizzaType +
            " Pizza berhasil diambil dan mengikuti HoldPoint."
        );

        return true;
    }

    public void DropServer(Vector3 dropPosition)
    {
        if (!IsServer)
            return;

        // Berhenti mengikuti HoldPoint
        followTarget = null;

        transform.position = dropPosition;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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

        Debug.Log(
            pizzaType +
            " Pizza berhasil dijatuhkan."
        );
    }

    public void ServeServer()
    {
        if (!IsServer)
            return;

        followTarget = null;

        if (spawner != null)
        {
            spawner.PizzaServed();
        }

        NetworkObject.Despawn(true);
    }
}