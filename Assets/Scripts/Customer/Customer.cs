using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Customer Request")]
    public PizzaType requestedPizza;

    private CustomerSpawner spawner;

    private Transform spawnPoint;

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
        requestedPizza = pizzaType;

        UpdateCustomerColor();
    }

    private void UpdateCustomerColor()
    {
        Renderer rend = GetComponent<Renderer>();

        if (rend == null)
            return;

        Material material = rend.material;

        if (requestedPizza == PizzaType.Pepperoni)
        {
            material.color = Color.red;
        }
        else if (requestedPizza == PizzaType.Cheese)
        {
            material.color = Color.yellow;
        }
    }

    public bool ReceivePizza(Pizza pizza)
    {
        if (pizza == null)
            return false;

        // Cek apakah pizza sesuai permintaan
        if (pizza.pizzaType != requestedPizza)
        {
            Debug.Log(
                "Pizza salah! Customer meminta " +
                requestedPizza +
                ", tetapi diberikan " +
                pizza.pizzaType
            );

            return false;
        }

        Debug.Log(
            "Pizza benar! Customer menerima " +
            requestedPizza
        );

        // Beritahu PizzaSpawner bahwa pizza berhasil diberikan
        pizza.Serve();

        // Beritahu CustomerSpawner bahwa customer sudah dilayani
        if (spawner != null)
        {
            spawner.CustomerServed(this);
        }

        // Hapus customer
        Destroy(gameObject);

        return true;
    }
}