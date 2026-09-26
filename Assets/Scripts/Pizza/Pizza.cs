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

        // Kalau sedang dibawa Player, Pizza mengikuti HoldPoint.
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

        // Simpan HoldPoint sebagai target yang harus diikuti
        followTarget = holdPoint;

        // Langsung pindahkan Pizza ke HoldPoint di Server
        transform.position = holdPoint.position;
        transform.rotation = holdPoint.rotation;

        // PANGGIL CLIENT RPC: Beritahu SEMUA pemain untuk mematikan tabrakan & fisik pizza ini
        UpdatePhysicsClientRpc(true);

        if (spawner != null)
        {
            spawner.PizzaTaken();
        }

        Debug.Log(pizzaType + " Pizza berhasil diambil dan mengikuti HoldPoint.");
        return true;
    }

    public void DropServer(Vector3 dropPosition)
    {
        if (!IsServer)
            return;

        // Berhenti mengikuti HoldPoint
        followTarget = null;
        transform.position = dropPosition;

        // PANGGIL CLIENT RPC: Beritahu SEMUA pemain untuk menyalakan kembali tabrakan & fisik pizza
        UpdatePhysicsClientRpc(false);

        if (spawner != null)
        {
            spawner.PizzaDropped(gameObject);
        }

        Debug.Log(pizzaType + " Pizza berhasil dijatuhkan.");
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

    // ==========================================
    // SINKRONISASI FISIKA KE SEMUA CLIENT
    // ==========================================
    [ClientRpc]
    private void UpdatePhysicsClientRpc(bool isHeld)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>();

        if (isHeld)
        {
            // Jika sedang dipegang: Matikan gravitasi & tabrakan di layar semua orang
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            if (col != null)
            {
                col.enabled = false;
            }
        }
        else
        {
            // Jika dijatuhkan: Nyalakan kembali gravitasi & tabrakan di layar semua orang
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
}