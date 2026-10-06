using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Pizza : NetworkBehaviour
{
    public PizzaType pizzaType;
    private PizzaSpawner spawner;

    // ==========================================
    // DEBUG NETWORK SPAWN
    // ==========================================
    public override void OnNetworkSpawn()
    {
        Debug.Log(
            $"[PIZZA SPAWN] " +
            $"Type={pizzaType} | " +
            $"IsServer={IsServer} | " +
            $"IsClient={IsClient} | " +
            $"Owner={OwnerClientId} | " +
            $"NetworkObjectId={NetworkObjectId} | " +
            $"Position={transform.position}"
        );
    }

    public void SetSpawner(PizzaSpawner pizzaSpawner)
    {
        spawner = pizzaSpawner;
    }

    public bool PickUpServer(Transform holdPoint)
    {
        if (!IsServer) return false;
        if (holdPoint == null) return false;

        NetworkObject playerNetObj =
            holdPoint.GetComponentInParent<NetworkObject>();

        ulong playerId =
            playerNetObj != null
                ? playerNetObj.NetworkObjectId
                : ulong.MaxValue;

        string holdPointName = holdPoint.name;

        // Matikan sync posisi selama dipegang (untuk dedicated server)
        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

        // Server: tempelkan pizza ke HoldPoint
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Beritahu semua client
        UpdatePhysicsClientRpc(true, playerId, holdPointName);

        if (spawner != null)
            spawner.PizzaTaken();

        Debug.Log(
            $"[PIZZA PICKUP] {pizzaType} berhasil diambil oleh Player {playerId}"
        );

        return true;
    }

    // Memindahkan pizza yang sedang dipegang ke HoldPoint player lain
    // (tidak memanggil spawner.PizzaTaken() lagi karena pizza sudah pernah diambil)
    public bool TransferServer(Transform newHoldPoint)
    {
        if (!IsServer) return false;
        if (newHoldPoint == null) return false;

        NetworkObject playerNetObj =
            newHoldPoint.GetComponentInParent<NetworkObject>();

        ulong playerId =
            playerNetObj != null
                ? playerNetObj.NetworkObjectId
                : ulong.MaxValue;

        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

        transform.SetParent(newHoldPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        UpdatePhysicsClientRpc(true, playerId, newHoldPoint.name);

        Debug.Log(
            $"[PIZZA TRANSFER] {pizzaType} dipindahkan ke Player {playerId}"
        );

        return true;
    }

    public void DropServer(Vector3 dropPosition)
    {
        if (!IsServer) return;

        // Lepaskan dari tangan
        transform.SetParent(null);
        transform.position = dropPosition;

        // Aktifkan lagi sync posisi, lalu teleport agar client tidak interpolasi dari posisi lama
        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt != null)
        {
            nt.enabled = true;
            nt.Teleport(dropPosition, transform.rotation, transform.localScale);
        }

        UpdatePhysicsClientRpc(false, ulong.MaxValue, "");

        if (spawner != null)
            spawner.PizzaDropped(gameObject);

        Debug.Log(
            $"[PIZZA DROP] {pizzaType} dijatuhkan di {dropPosition}"
        );
    }

    public void ServeServer()
    {
        if (!IsServer) return;

        transform.SetParent(null);

        if (spawner != null)
            spawner.PizzaServed();

        Debug.Log(
            $"[PIZZA SERVE] {pizzaType} dihancurkan oleh Server"
        );

        NetworkObject.Despawn(true);
    }

    // ==========================================
    // SINKRONISASI FISIKA & LOKASI KE CLIENT
    // ==========================================
    [ClientRpc]
    private void UpdatePhysicsClientRpc(
        bool isHeld,
        ulong playerId,
        string hpName)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>();
        NetworkTransform nt = GetComponent<NetworkTransform>();

        Debug.Log(
            $"[PIZZA RPC] Type={pizzaType} | " +
            $"isHeld={isHeld} | " +
            $"playerId={playerId} | " +
            $"hpName={hpName} | " +
            $"IsServer={IsServer} | " +
            $"IsClient={IsClient}"
        );

        if (isHeld)
        {
            // Matikan fisika lokal agar tidak melawan posisi
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (col != null)
                col.enabled = false;

            // Matikan sync posisi dari network selama dipegang,
            // supaya posisi dari HoldPoint (lokal) tidak tertimpa data lama dari server
            if (nt != null)
                nt.enabled = false;

            // Client: tempelkan pizza ke HoldPoint pemain yang membawa
            if (!IsServer && playerId != ulong.MaxValue)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                    .TryGetValue(playerId, out NetworkObject playerObj))
                {
                    Debug.Log(
                        $"[PIZZA HOLDPOINT] Player {playerId} ditemukan. " +
                        $"Mencari HoldPoint: {hpName}"
                    );

                    Transform[] allChildren =
                        playerObj.GetComponentsInChildren<Transform>();

                    bool foundHoldPoint = false;

                    foreach (Transform t in allChildren)
                    {
                        if (t.name == hpName)
                        {
                            transform.SetParent(t);
                            transform.localPosition = Vector3.zero;
                            transform.localRotation = Quaternion.identity;

                            foundHoldPoint = true;

                            Debug.Log(
                                $"[PIZZA HOLDPOINT] {pizzaType} " +
                                $"berhasil ditempel ke {hpName}"
                            );

                            break;
                        }
                    }

                    if (!foundHoldPoint)
                    {
                        Debug.LogError(
                            $"[PIZZA HOLDPOINT] HoldPoint '{hpName}' " +
                            $"TIDAK DITEMUKAN pada Player {playerId}!"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        $"[PIZZA HOLDPOINT] Player NetworkObject " +
                        $"{playerId} TIDAK DITEMUKAN di Client!"
                    );
                }
            }
        }
        else
        {
            // Kembalikan fisika
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (col != null)
                col.enabled = true;

            // Aktifkan lagi sync posisi setelah dilepas
            if (nt != null)
                nt.enabled = true;

            // Client: lepaskan dari tangan
            if (!IsServer)
            {
                transform.SetParent(null);

                Debug.Log(
                    $"[PIZZA DROP CLIENT] {pizzaType} dilepas dari HoldPoint"
                );
            }
        }
    }
}