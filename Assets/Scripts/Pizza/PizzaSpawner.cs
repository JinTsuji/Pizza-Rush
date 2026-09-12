using UnityEngine;

public class PizzaSpawner : MonoBehaviour
{
    [SerializeField] private GameObject pizzaPrefab;
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private PizzaType pizzaType;

    [SerializeField] private float respawnDelay = 3f;

    private GameObject currentPizza;
    private bool isRespawning;

    private void Start()
    {
        SpawnPizza();
    }

    public void SpawnPizza()
    {
        if (currentPizza != null)
            return;

        if (isRespawning)
            return;

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
        // Station sekarang kosong
        currentPizza = null;

        // Mulai timer respawn
        if (!isRespawning)
        {
            StartCoroutine(RespawnPizza());
        }
    }

    private System.Collections.IEnumerator RespawnPizza()
    {
        isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        SpawnPizza();

        isRespawning = false;
    }

    public void PizzaDropped(GameObject pizza)
    {
        // Pizza yang dijatuhkan kembali menjadi pizza aktif.
        currentPizza = pizza;
    }
}