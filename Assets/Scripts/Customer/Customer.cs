using UnityEngine;
using UnityEngine.UI; // Wajib ditambahkan untuk memanggil UI Image
using Unity.Netcode;

public class Customer : NetworkBehaviour
{
    [Header("Customer Request")]
    public NetworkVariable<PizzaType> requestedPizza = new NetworkVariable<PizzaType>(
        PizzaType.Pepperoni,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI Pesanan")]
    [SerializeField] private Image orderImage;          // Slot untuk objek Image UI di kepala bola
    [SerializeField] private Sprite pepperoniSprite;    // Slot sprite gambar Pepperoni
    [SerializeField] private Sprite cheeseSprite;       // Slot sprite gambar Cheese

    private CustomerSpawner spawner;
    private Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        // Setel tampilan UI awal berdasarkan nilai dari jaringan
        UpdateCustomerUI(requestedPizza.Value);

        // Jika Server mengubah pesanan, layar semua Client otomatis memperbarui gambarnya
        requestedPizza.OnValueChanged += (oldValue, newValue) =>
        {
            UpdateCustomerUI(newValue);
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
        if (IsServer)
        {
            requestedPizza.Value = pizzaType;
        }
    }

    private void UpdateCustomerUI(PizzaType type)
    {
        if (orderImage == null) return;

        if (type == PizzaType.Pepperoni)
        {
            orderImage.sprite = pepperoniSprite;
        }
        else if (type == PizzaType.Cheese)
        {
            orderImage.sprite = cheeseSprite;
        }
    }

    // ==========================================
    // LOGIKA MENERIMA PIZZA DARI PEMAIN
    // ==========================================
    public bool ReceivePizza(Pizza pizza)
    {
        if (pizza == null) return false;

        NetworkObject pizzaNetObj = pizza.GetComponent<NetworkObject>();
        if (pizzaNetObj == null) return false;

        if (IsServer)
        {
            ProcessPizzaLogic(pizzaNetObj);
        }
        else
        {
            ReceivePizzaServerRpc(pizzaNetObj);
        }

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReceivePizzaServerRpc(NetworkObjectReference pizzaRef)
    {
        if (pizzaRef.TryGet(out NetworkObject pizzaNetObj))
        {
            ProcessPizzaLogic(pizzaNetObj);
        }
    }

    private void ProcessPizzaLogic(NetworkObject pizzaNetObj)
    {
        Pizza pizza = pizzaNetObj.GetComponent<Pizza>();
        if (pizza == null) return;

        if (pizza.pizzaType != requestedPizza.Value)
        {
            Debug.Log("Pizza salah! Customer meminta " + requestedPizza.Value + ", tetapi diberikan " + pizza.pizzaType);
            return;
        }

        Debug.Log("Pizza benar! Customer menerima " + requestedPizza.Value);

        if (pizzaNetObj.IsSpawned)
        {
            pizzaNetObj.Despawn();
        }

        if (spawner != null)
        {
            spawner.CustomerServed(this);
        }
        else
        {
            Debug.Log("Spawner tidak ditemukan. Menghancurkan pelanggan secara paksa!");
            if (IsSpawned)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}