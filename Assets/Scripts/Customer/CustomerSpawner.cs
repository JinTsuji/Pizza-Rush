using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Customer Spawn")]
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private int maxCustomers = 5;

    private List<Customer> activeCustomers = new List<Customer>();

    private void Start()
    {
        StartCoroutine(CustomerSpawnRoutine());
    }

    private IEnumerator CustomerSpawnRoutine()
    {
        // Customer pertama langsung muncul
        SpawnCustomer();

        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);

            // Coba spawn customer baru
            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        // Bersihkan customer yang sudah tidak ada
        CleanupCustomerList();

        // Cek maximum customer
        if (activeCustomers.Count >= maxCustomers)
        {
            Debug.Log(
                "Maximum customer tercapai: " +
                activeCustomers.Count +
                "/" +
                maxCustomers
            );

            return;
        }

        if (customerPrefab == null)
        {
            Debug.LogError("Customer Prefab belum diisi!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn Points belum diisi!");
            return;
        }

        // Cari spawn point yang masih kosong
        List<Transform> availableSpawnPoints =
            new List<Transform>();

        foreach (Transform spawnPoint in spawnPoints)
        {
            bool isOccupied = false;

            foreach (Customer customer in activeCustomers)
            {
                if (customer == null)
                    continue;

                if (customer.GetSpawnPoint() == spawnPoint)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }

        // Semua spawn point sedang digunakan
        if (availableSpawnPoints.Count == 0)
        {
            Debug.Log("Semua Customer Spawn Point sedang digunakan.");
            return;
        }

        // Pilih spawn point kosong secara random
        int randomIndex =
            Random.Range(0, availableSpawnPoints.Count);

        Transform selectedSpawnPoint =
            availableSpawnPoints[randomIndex];

        // Random jenis pizza
        PizzaType requestedPizza;

        if (Random.Range(0, 2) == 0)
        {
            requestedPizza = PizzaType.Pepperoni;
        }
        else
        {
            requestedPizza = PizzaType.Cheese;
        }

        // Spawn customer
        GameObject newCustomerObject = Instantiate(
            customerPrefab,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );

        Customer newCustomer =
            newCustomerObject.GetComponent<Customer>();

        if (newCustomer != null)
        {
            newCustomer.SetSpawner(this);
            newCustomer.SetCustomerType(requestedPizza);
            newCustomer.SetSpawnPoint(selectedSpawnPoint);

            activeCustomers.Add(newCustomer);

            Debug.Log(
                "Customer baru muncul di " +
                selectedSpawnPoint.name +
                " | Meminta: " +
                requestedPizza +
                " | Customer aktif: " +
                activeCustomers.Count +
                "/" +
                maxCustomers
            );
        }
        else
        {
            Debug.LogError(
                "Customer Prefab tidak memiliki script Customer!"
            );

            Destroy(newCustomerObject);
        }
    }

    public void CustomerServed(Customer customer)
    {
        if (customer != null)
        {
            activeCustomers.Remove(customer);
        }

        Debug.Log(
            "Customer dilayani. Customer aktif sekarang: " +
            activeCustomers.Count +
            "/" +
            maxCustomers
        );
    }

    private void CleanupCustomerList()
    {
        activeCustomers.RemoveAll(customer => customer == null);
    }
}