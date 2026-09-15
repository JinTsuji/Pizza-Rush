using UnityEngine;
using Unity.Netcode;

public class PizzaSpawner : NetworkBehaviour
{
    [Header("Pizza")]
    [SerializeField] private GameObject pizzaPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PizzaType pizzaType;

    private GameObject currentPizza;

    private void Start()
    {
        // Hanya Server yang boleh melakukan spawn
        if (!IsServer)
            return;

        SpawnPizza();
    }

    public void SpawnPizza()
    {
        // Hanya Server yang boleh spawn NetworkObject
        if (!IsServer)
            return;

        if (currentPizza != null)
            return;

        if (pizzaPrefab == null)
        {
            Debug.LogError("Pizza Prefab belum diisi!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn Point belum diisi!");
            return;
        }

        // Spawn pizza secara normal
        currentPizza = Instantiate(
            pizzaPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Ambil komponen Pizza
        Pizza pizza = currentPizza.GetComponent<Pizza>();

        if (pizza != null)
        {
            pizza.pizzaType = pizzaType;
            pizza.SetSpawner(this);
        }

        // Ambil NetworkObject
        NetworkObject networkObject =
            currentPizza.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "Pizza Prefab tidak memiliki NetworkObject!"
            );

            Destroy(currentPizza);
            currentPizza = null;
            return;
        }

        // Spawn ke semua client
        networkObject.Spawn();

        Debug.Log(
            pizzaType +
            " Pizza berhasil di-spawn oleh Server."
        );
    }

    public void PizzaTaken()
    {
        // Hanya Server yang mengatur spawn
        if (!IsServer)
            return;

        // Pizza lama sekarang dibawa Player
        currentPizza = null;

        // Langsung spawn pizza baru
        SpawnPizza();

        Debug.Log(
            pizzaType +
            " Pizza diambil. Pizza baru langsung muncul."
        );
    }

    public void PizzaDropped(GameObject pizza)
    {
        if (!IsServer)
            return;

        // Pizza yang dijatuhkan menjadi pizza aktif
        currentPizza = pizza;

        Debug.Log(
            pizzaType +
            " Pizza dijatuhkan dan kembali menjadi pizza aktif."
        );
    }

    public void PizzaServed()
    {
        if (!IsServer)
            return;

        // Pizza yang diberikan ke Customer sudah selesai
        currentPizza = null;

        Debug.Log(
            pizzaType +
            " Pizza berhasil diberikan ke Customer."
        );
    }
}