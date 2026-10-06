using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawner : NetworkBehaviour
{
    [Header("Customer Prefabs")]
    [SerializeField] private GameObject[] customerPrefabs;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Customer Spawn")]
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private int maxCustomers = 5;

    private List<Customer> activeCustomers = new List<Customer>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(CustomerSpawnRoutine());
        }
    }

    private IEnumerator CustomerSpawnRoutine()
    {
        // Customer pertama langsung muncul
        SpawnCustomer();

        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);
            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        CleanupCustomerList();

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

        // Cek prefab Customer
        if (customerPrefabs == null ||
            customerPrefabs.Length == 0)
        {
            Debug.LogError(
                "Customer Prefabs belum diisi!"
            );

            return;
        }

        // Cek Spawn Point
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "Spawn Points belum diisi!"
            );

            return;
        }

        // ==========================================
        // CARI SPAWN POINT YANG MASIH KOSONG
        // ==========================================

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

        if (availableSpawnPoints.Count == 0)
        {
            Debug.Log(
                "Semua Customer Spawn Point sedang digunakan."
            );

            return;
        }

        // ==========================================
        // PILIH SPAWN POINT RANDOM
        // ==========================================

        int randomSpawnIndex =
            Random.Range(
                0,
                availableSpawnPoints.Count
            );

        Transform selectedSpawnPoint =
            availableSpawnPoints[randomSpawnIndex];

        // ==========================================
        // PILIH MODEL CUSTOMER RANDOM
        // ==========================================

        int randomCustomerIndex =
            Random.Range(
                0,
                customerPrefabs.Length
            );

        GameObject selectedCustomerPrefab =
            customerPrefabs[randomCustomerIndex];

        // ==========================================
        // PILIH PESANAN RANDOM
        // ==========================================

        PizzaType requestedPizza =
            Random.Range(0, 2) == 0
            ? PizzaType.Pepperoni
            : PizzaType.Cheese;

        // ==========================================
        // SPAWN CUSTOMER
        // ==========================================

        GameObject newCustomerObject = Instantiate(
            selectedCustomerPrefab,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );

        Customer newCustomer =
            newCustomerObject.GetComponent<Customer>();

        NetworkObject networkObject =
            newCustomerObject.GetComponent<NetworkObject>();

        // ==========================================
        // VALIDASI
        // ==========================================

        if (newCustomer == null)
        {
            Debug.LogError(
                "Customer Prefab tidak memiliki " +
                "script Customer!"
            );

            Destroy(newCustomerObject);
            return;
        }

        if (networkObject == null)
        {
            Debug.LogError(
                "Customer Prefab tidak memiliki " +
                "NetworkObject!"
            );

            Destroy(newCustomerObject);
            return;
        }

        // ==========================================
        // SET DATA CUSTOMER
        // ==========================================

        newCustomer.SetSpawner(this);
        newCustomer.SetSpawnPoint(
            selectedSpawnPoint
        );

        activeCustomers.Add(newCustomer);

        // ==========================================
        // SPAWN KE NETWORK
        // ==========================================

        networkObject.Spawn();

        // Set request setelah NetworkObject spawn
        newCustomer.SetCustomerType(
            requestedPizza
        );

        Debug.Log(
            "Customer baru muncul | " +
            "Model: " +
            selectedCustomerPrefab.name +
            " | Spawn Point: " +
            selectedSpawnPoint.name +
            " | Meminta: " +
            requestedPizza +
            " | Customer aktif: " +
            activeCustomers.Count +
            "/" +
            maxCustomers
        );
    }

    // ==========================================
    // CUSTOMER DILAYANI
    // ==========================================

    public void CustomerServed(Customer customer)
    {
        if (!IsServer)
            return;

        if (customer != null)
        {
            activeCustomers.Remove(customer);

            NetworkObject netObj =
                customer.GetComponent<NetworkObject>();

            if (netObj != null &&
                netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }

        Debug.Log(
            "Customer dilayani. " +
            "Customer aktif sekarang: " +
            activeCustomers.Count +
            "/" +
            maxCustomers
        );
    }

    private void CleanupCustomerList()
    {
        activeCustomers.RemoveAll(
            customer => customer == null
        );
    }
}