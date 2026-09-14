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
        // Pizza lama sedang dibawa Player,
        // sehingga slot Spawner dianggap kosong.
        currentPizza = null;

        // Langsung spawn pizza baru.
        SpawnPizza();

        Debug.Log(
            pizzaType + " Pizza diambil. " +
            "Pizza baru langsung muncul di Spawner."
        );
    }

    public void PizzaDropped(GameObject pizza)
    {
        // Pizza yang dijatuhkan menjadi pizza aktif lagi.
        currentPizza = pizza;
    }

    public void PizzaServed()
    {
        // Pizza yang diberikan kepada Customer sudah selesai.
        // Tidak perlu spawn lagi karena pizza baru
        // sudah dibuat ketika pizza sebelumnya diambil.
        currentPizza = null;

        Debug.Log(
            pizzaType + " Pizza berhasil diberikan ke Customer."
        );
    }
}