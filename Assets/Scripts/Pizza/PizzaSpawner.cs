using UnityEngine;

public class PizzaSpawner : MonoBehaviour
{
    [Header("Pizza")]
    [SerializeField] private GameObject pizzaPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PizzaType pizzaType;

    private GameObject currentPizza;

    private void Start()
    {
        SpawnPizza();
    }

    public void SpawnPizza()
    {
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

        if (pizza != null)
        {
            pizza.pizzaType = pizzaType;
            pizza.SetSpawner(this);
        }
    }

    public void PizzaTaken()
    {
        // Pizza sedang dibawa Player.
        // Jangan spawn pizza baru dulu.
        currentPizza = null;
    }

    public void PizzaDropped(GameObject pizza)
    {
        // Pizza yang dijatuhkan menjadi pizza aktif lagi.
        currentPizza = pizza;
    }

    public void PizzaServed()
    {
        // Pizza sudah diberikan kepada Customer.
        // Langsung buat pizza baru di spawn point.
        currentPizza = null;

        SpawnPizza();

        Debug.Log(
            pizzaType + " Pizza muncul kembali di Pizza Spawner."
        );
    }
}