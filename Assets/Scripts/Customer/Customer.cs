using UnityEngine;
using Unity.Netcode; // Wajib ditambahkan

// 1. Ubah menjadi NetworkBehaviour
public class Customer : NetworkBehaviour
{
    [Header("Customer Request")]
    // 2. Gunakan NetworkVariable agar pesanan (dan warnanya) sinkron ke semua Client
    public NetworkVariable<PizzaType> requestedPizza = new NetworkVariable<PizzaType>(
        PizzaType.Pepperoni,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private CustomerSpawner spawner;
    private Transform spawnPoint;

    // Fungsi bawaan Netcode yang dipanggil saat objek pertama kali muncul di layar
    public override void OnNetworkSpawn()
    {
        // Setel warna awal berdasarkan nilai dari jaringan
        UpdateCustomerColor(requestedPizza.Value);

        // Jika Server tiba-tiba mengubah pesanan, layar semua Client otomatis memperbarui warnanya
        requestedPizza.OnValueChanged += (oldValue, newValue) =>
        {
            UpdateCustomerColor(newValue);
        };
    }

    public void SetSpawner(CustomerSpawner customerSpawner)
    {
        spawner = customerSpawner;
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
    }

    public Transform GetSpawnPoint()
    {
        return spawnPoint;
    }

    public void SetCustomerType(PizzaType pizzaType)
    {
        // Hanya Server yang berhak mengubah isi NetworkVariable
        if (IsServer)
        {
            requestedPizza.Value = pizzaType;
        }
    }

    private void UpdateCustomerColor(PizzaType type)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Material material = rend.material;

        if (type == PizzaType.Pepperoni)
        {
            material.color = Color.red;
        }
        else if (type == PizzaType.Cheese)
        {
            material.color = Color.yellow;
        }
    }

    // ==========================================
    // LOGIKA MENERIMA PIZZA DARI PEMAIN
    // ==========================================
    public bool ReceivePizza(Pizza pizza)
    {
        if (pizza == null) return false;

        // Ambil "KTP Jaringan" dari pizza untuk dikirimkan melalui internet
        NetworkObject pizzaNetObj = pizza.GetComponent<NetworkObject>();
        if (pizzaNetObj == null) return false;

        if (IsServer)
        {
            // Jika Host yang melayani, langsung eksekusi
            ProcessPizzaLogic(pizzaNetObj);
        }
        else
        {
            // Jika Client yang melayani, kirim laporan ke Server
            ReceivePizzaServerRpc(pizzaNetObj);
        }

        return true;
    }

    // Mengizinkan Client manapun untuk mengirim pizza ke pelanggan ini
    [ServerRpc(RequireOwnership = false)]
    private void ReceivePizzaServerRpc(NetworkObjectReference pizzaRef)
    {
        // Menerjemahkan ID Pizza kembali menjadi GameObject di sisi Server
        if (pizzaRef.TryGet(out NetworkObject pizzaNetObj))
        {
            ProcessPizzaLogic(pizzaNetObj);
        }
    }

    // Fungsi ini PASTI hanya dijalankan oleh Server
    private void ProcessPizzaLogic(NetworkObject pizzaNetObj)
    {
        Pizza pizza = pizzaNetObj.GetComponent<Pizza>();
        if (pizza == null) return;

        // Cek apakah jenis pizza sesuai permintaan
        if (pizza.pizzaType != requestedPizza.Value)
        {
            Debug.Log("Pizza salah! Customer meminta " + requestedPizza.Value + ", tetapi diberikan " + pizza.pizzaType);
            return; // Pesanan ditolak, pelanggan belum pergi
        }

        Debug.Log("Pizza benar! Customer menerima " + requestedPizza.Value);

        // 1. Hancurkan objek Pizza dari jaringan (karena sudah dimakan/diserahkan)
        if (pizzaNetObj.IsSpawned)
        {
            pizzaNetObj.Despawn();
        }

        // 2. Hancurkan Pelanggan
        if (spawner != null)
        {
            // Jika NPC ini berasal dari Spawner otomatis
            spawner.CustomerServed(this);
        }
        else
        {
            // Jika NPC ini ditaruh manual di scene saat testing mandiri
            Debug.Log("Spawner tidak ditemukan. Menghancurkan pelanggan secara paksa!");
            if (IsSpawned)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}