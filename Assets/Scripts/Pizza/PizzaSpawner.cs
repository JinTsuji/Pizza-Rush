using UnityEngine;
using Unity.Netcode;

public class PizzaSpawner : NetworkBehaviour
{
    [Header("Pizza")]
    [SerializeField] private GameObject pizzaPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PizzaType pizzaType;

    private GameObject currentPizza;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        SpawnPizza();
    }

    public void SpawnPizza()
    {
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

        currentPizza = Instantiate(
            pizzaPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Pizza pizza = currentPizza.GetComponent<Pizza>();

        if (pizza == null)
        {
            Debug.LogError(
                "Pizza Prefab tidak memiliki script Pizza!"
            );

            Destroy(currentPizza);
            currentPizza = null;
            return;
        }

        pizza.pizzaType = pizzaType;
        pizza.SetSpawner(this);

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

        networkObject.Spawn();

        Debug.Log(
            pizzaType +
            " Pizza berhasil di-spawn oleh Server."
        );
    }

    public void PizzaTaken()
    {
        if (!IsServer)
            return;

        currentPizza = null;

        // Langsung buat pizza pengganti
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

        currentPizza = pizza;

        Debug.Log(
            pizzaType +
            " Pizza dijatuhkan."
        );
    }

    public void PizzaServed()
    {
        if (!IsServer)
            return;

        currentPizza = null;
    }
}