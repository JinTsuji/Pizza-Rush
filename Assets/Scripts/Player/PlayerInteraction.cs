using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactDistance = 2f;

    [Header("Pizza")]
    [SerializeField] private Transform holdPoint;

    [Header("Interaksi Antar Player")]
    [SerializeField] private float stealCooldown = 1.5f;

    private NetworkVariable<ulong> heldPizzaId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Pizza heldPizza;

    // Hanya dipakai di Server, mencegah spam rebut
    private float lastStealTime = -999f;

    private bool HandsEmpty => heldPizzaId.Value == ulong.MaxValue;

    public override void OnNetworkSpawn()
    {
        heldPizzaId.OnValueChanged += OnHeldPizzaChanged;
        UpdateHeldPizzaReference(heldPizzaId.Value);
    }

    public override void OnNetworkDespawn()
    {
        heldPizzaId.OnValueChanged -= OnHeldPizzaChanged;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Keyboard.current == null)
            return;

        // 1. Tombol E HANYA untuk mengambil Pizza dari meja
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldPizza == null)
            {
                TryPickupPizza();
            }
            else
            {
                Debug.Log("Tangan sudah penuh! Tekan F untuk memberikan ke pelanggan, G untuk memberi ke player lain.");
            }
        }

        // 2. Tombol F khusus untuk memberikan Pizza ke Pelanggan
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (heldPizza != null)
            {
                Debug.Log("Player membawa " + heldPizza.pizzaType + ". Mencoba melayani pelanggan...");
                TryServeCustomer();
            }
            else
            {
                Debug.Log("Tidak bisa melayani, tangan masih kosong!");
            }
        }

        // 3. Tombol G untuk memberikan Pizza ke player lain
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            TryGivePizzaToPlayer();
        }

        // 4. Tombol R untuk merebut Pizza dari player lain
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryStealPizzaFromPlayer();
        }

        // 5. Tombol Q untuk menjatuhkan Pizza
        /*if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryDropPizza();
        }*/
    }

    // ==========================================
    // INTERAKSI ANTAR PLAYER (Tombol G dan R)
    // ==========================================

    // Mencari player lain terdekat. Kalau mustHoldPizza true, hanya player yang membawa pizza;
    // kalau false, hanya player yang tangannya kosong.
    private PlayerInteraction FindNearestOtherPlayer(bool mustHoldPizza)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);

        PlayerInteraction closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            PlayerInteraction other = col.GetComponentInParent<PlayerInteraction>();

            if (other == null || other == this)
                continue;

            if (mustHoldPizza == other.HandsEmpty)
                continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = other;
            }
        }

        return closest;
    }

    private void TryGivePizzaToPlayer()
    {
        if (heldPizza == null)
        {
            Debug.Log("Tidak ada pizza yang bisa diberikan, tangan kosong.");
            return;
        }

        PlayerInteraction target = FindNearestOtherPlayer(mustHoldPizza: false);
        if (target == null)
        {
            Debug.Log("Tidak ada player dengan tangan kosong di dekatmu.");
            return;
        }

        RequestGivePizzaRpc(target.NetworkObject.NetworkObjectId);
    }

    private void TryStealPizzaFromPlayer()
    {
        if (heldPizza != null)
        {
            Debug.Log("Tangan masih penuh, tidak bisa merebut pizza.");
            return;
        }

        PlayerInteraction target = FindNearestOtherPlayer(mustHoldPizza: true);
        if (target == null)
        {
            Debug.Log("Tidak ada player yang membawa pizza di dekatmu.");
            return;
        }

        RequestStealPizzaRpc(target.NetworkObject.NetworkObjectId);
    }

    [Rpc(SendTo.Server)]
    private void RequestGivePizzaRpc(ulong targetPlayerNetworkObjectId)
    {
        // Player yang memanggil RPC ini adalah pemberi
        if (!TryGetPlayer(targetPlayerNetworkObjectId, out PlayerInteraction target))
            return;

        TransferPizzaServer(this, target);
    }

    [Rpc(SendTo.Server)]
    private void RequestStealPizzaRpc(ulong targetPlayerNetworkObjectId)
    {
        // Player yang memanggil RPC ini adalah perebut
        if (!TryGetPlayer(targetPlayerNetworkObjectId, out PlayerInteraction victim))
            return;

        if (Time.time - lastStealTime < stealCooldown)
            return;

        if (TransferPizzaServer(victim, this))
            lastStealTime = Time.time;
    }

    private bool TryGetPlayer(ulong networkObjectId, out PlayerInteraction player)
    {
        player = null;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
            return false;

        player = netObj.GetComponent<PlayerInteraction>();
        return player != null;
    }

    // Hanya dijalankan di Server. Memindahkan pizza dari 'from' ke 'to'.
    private static bool TransferPizzaServer(PlayerInteraction from, PlayerInteraction to)
    {
        if (from == to)
            return false;

        // Pemberi harus punya pizza, penerima harus kosong
        if (from.HandsEmpty || !to.HandsEmpty)
            return false;

        // Cek jarak dari sisi Server (+1.5f toleransi lag)
        float distance = Vector3.Distance(from.transform.position, to.transform.position);
        if (distance > from.interactDistance + 1.5f)
        {
            Debug.LogWarning("Dua player terlalu jauh untuk memindahkan pizza.");
            return false;
        }

        ulong pizzaId = from.heldPizzaId.Value;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pizzaId, out NetworkObject pizzaNetObj))
            return false;

        Pizza pizza = pizzaNetObj.GetComponent<Pizza>();
        if (pizza == null)
            return false;

        if (!pizza.TransferServer(to.holdPoint))
            return false;

        from.heldPizzaId.Value = ulong.MaxValue;
        to.heldPizzaId.Value = pizzaId;

        Debug.Log("Server: pizza " + pizza.pizzaType + " berpindah antar player.");
        return true;
    }

    // ==========================================
    // LOGIKA MEMBERIKAN PIZZA KE PELANGGAN (Tombol F)
    // ==========================================
    private void TryServeCustomer()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);

        Customer closestCustomer = null;
        float closestDistance = Mathf.Infinity;

        // Cari pelanggan terdekat
        foreach (Collider col in colliders)
        {
            Customer customer = col.GetComponentInParent<Customer>();

            if (customer == null)
                continue;

            float distance = Vector3.Distance(transform.position, customer.transform.position);

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

        Debug.Log("Meminta Server memberikan Pizza ke Customer!");

        // Panggil Server untuk memproses serah terima
        RequestServePizzaRpc(
            closestCustomer.NetworkObject.NetworkObjectId,
            heldPizza.NetworkObject.NetworkObjectId
        );
    }

    [Rpc(SendTo.Server)]
    private void RequestServePizzaRpc(ulong customerNetworkObjectId, ulong pizzaNetworkObjectId)
    {
        // Validasi apakah Customer dan Pizza benar-benar ada di jaringan Server
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(customerNetworkObjectId, out NetworkObject customerNetObj))
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pizzaNetworkObjectId, out NetworkObject pizzaNetObj))
            return;

        Customer customer = customerNetObj.GetComponent<Customer>();
        Pizza pizza = pizzaNetObj.GetComponent<Pizza>();

        if (customer != null && pizza != null)
        {
            // Pastikan pizza ini memang yang dipegang player ini (mencegah pizza milik player lain dipakai)
            if (heldPizzaId.Value != pizzaNetworkObjectId)
                return;

            // Cek jarak dari Server untuk mencegah kecurangan (cheat)
            float distance = Vector3.Distance(transform.position, customer.transform.position);

            // Tambahan + 1.5f agar Server tidak menolak input karena lag jaringan
            if (distance > interactDistance + 1.5f)
            {
                Debug.LogWarning("Player terlalu jauh dari Customer.");
                return;
            }

            // Panggil fungsi ReceivePizza di skrip Customer
            customer.ReceivePizza(pizza);

            // Jika pesanan BENAR, maka pizza akan dihancurkan (Despawn) oleh Customer.
            // Oleh karena itu, kita harus mengosongkan tangan pemain di sini.
            if (customer.requestedPizza.Value == pizza.pizzaType)
            {
                heldPizzaId.Value = ulong.MaxValue;
                Debug.Log("Server: Serah terima berhasil, tangan pemain dikosongkan.");
            }
        }
    }

    // ==========================================
    // LOGIKA MENGAMBIL PIZZA (Tombol E)
    // ==========================================
    private void TryPickupPizza()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);

        Pizza closestPizza = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            Pizza pizza = col.GetComponentInParent<Pizza>();

            if (pizza == null)
                continue;

            float distance = Vector3.Distance(transform.position, pizza.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPizza = pizza;
            }
        }

        if (closestPizza == null)
        {
            Debug.Log("Tidak ada Pizza di dekat Player.");
            return;
        }

        Debug.Log("Meminta Server mengambil Pizza: " + closestPizza.pizzaType);
        RequestPickupPizzaRpc(closestPizza.NetworkObject.NetworkObjectId);
    }

    [Rpc(SendTo.Server)]
    private void RequestPickupPizzaRpc(ulong pizzaNetworkObjectId)
    {
        if (heldPizzaId.Value != ulong.MaxValue)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pizzaNetworkObjectId, out NetworkObject networkObject))
        {
            Debug.LogWarning("Pizza tidak ditemukan oleh Server.");
            return;
        }

        Pizza pizza = networkObject.GetComponent<Pizza>();
        if (pizza == null)
            return;

        float distance = Vector3.Distance(transform.position, pizza.transform.position);

        // Tambahan + 1.5f agar Server tidak menolak input karena lag jaringan
        if (distance > interactDistance + 1.5f)
        {
            Debug.LogWarning("Player terlalu jauh dari Pizza.");
            return;
        }

        bool success = pizza.PickUpServer(holdPoint);
        if (!success)
        {
            Debug.LogWarning("Pickup Pizza gagal.");
            return;
        }

        heldPizzaId.Value = pizza.NetworkObject.NetworkObjectId;
        Debug.Log("Server menerima pickup: " + pizza.pizzaType);
    }

    private void OnHeldPizzaChanged(ulong previousValue, ulong newValue)
    {
        UpdateHeldPizzaReference(newValue);
    }

    private void UpdateHeldPizzaReference(ulong networkObjectId)
    {
        if (networkObjectId == ulong.MaxValue)
        {
            heldPizza = null;
            return;
        }

        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
        {
            heldPizza = networkObject.GetComponent<Pizza>();
        }
    }

    // ==========================================
    // LOGIKA MENJATUHKAN PIZZA (Tombol Q)
    // ==========================================
    /*private void TryDropPizza()
    {
        if (heldPizza == null)
        {
            Debug.Log("Player tidak membawa Pizza.");
            return;
        }

        RequestDropPizzaRpc(heldPizza.NetworkObject.NetworkObjectId);
    }

    [Rpc(SendTo.Server)]
    private void RequestDropPizzaRpc(ulong pizzaNetworkObjectId)
    {
        if (heldPizzaId.Value != pizzaNetworkObjectId)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pizzaNetworkObjectId, out NetworkObject networkObject))
            return;

        Pizza pizza = networkObject.GetComponent<Pizza>();
        if (pizza == null)
            return;

        Vector3 dropPosition = transform.position + transform.forward;
        pizza.DropServer(dropPosition);

        heldPizzaId.Value = ulong.MaxValue;
        Debug.Log("Pizza berhasil dijatuhkan.");
    }*/

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}