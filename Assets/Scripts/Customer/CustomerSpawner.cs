using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawner : NetworkBehaviour
{
    [Header("Customer")]
    [SerializeField] private GameObject customerPrefab;

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
            Debug.Log("Maximum customer tercapai: " + activeCustomers.Count + "/" + maxCustomers);
            return;
        }

        if (customerPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        List<Transform> availableSpawnPoints = new List<Transform>();

        foreach (Transform spawnPoint in spawnPoints)
        {
            bool isOccupied = false;
            foreach (Customer customer in activeCustomers)
            {
                if (customer == null) continue;
                if (customer.GetSpawnPoint() == spawnPoint)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied) availableSpawnPoints.Add(spawnPoint);
        }

        if (availableSpawnPoints.Count == 0) return;

        int randomIndex = Random.Range(0, availableSpawnPoints.Count);
        Transform selectedSpawnPoint = availableSpawnPoints[randomIndex];

        PizzaType requestedPizza = (Random.Range(0, 2) == 0) ? PizzaType.Pepperoni : PizzaType.Cheese;

        GameObject newCustomerObject = Instantiate(
            customerPrefab,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );

        Customer newCustomer = newCustomerObject.GetComponent<Customer>();

        if (newCustomer != null)
        {
            newCustomer.SetSpawner(this);
            newCustomer.SetSpawnPoint(selectedSpawnPoint);

            activeCustomers.Add(newCustomer);

            // 1. SPAWN OBJEKNYA TERLEBIH DAHULU KE JARINGAN
            newCustomerObject.GetComponent<NetworkObject>().Spawn();

            // 2. BARU SETELAH ITU SET JENIS PESANANNYA (Agar NetworkVariable tersinkronisasi)
            newCustomer.SetCustomerType(requestedPizza);

            Debug.Log($"Customer baru muncul di {selectedSpawnPoint.name} | Meminta: {requestedPizza}");
        }
        else
        {
            Destroy(newCustomerObject);
        }
    }

    // ==========================================
    // LOGIKA PENGHAPUSAN PELANGGAN YANG DILAYANI
    // ==========================================
    public void CustomerServed(Customer customer)
    {
        // 1. Pastikan hanya Host/Server yang boleh menghapus pelanggan
        if (!IsServer) return;

        if (customer != null)
        {
            // 2. Kosongkan slot di daftar agar timer bisa memunculkan pelanggan baru
            activeCustomers.Remove(customer);

            // 3. Hancurkan wujud fisik pelanggan dari layar semua pemain (Host & Client)
            NetworkObject netObj = customer.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(); // Despawn otomatis akan menghancurkan GameObject-nya
            }
        }

        Debug.Log("Customer dilayani. Customer aktif sekarang: " + activeCustomers.Count + "/" + maxCustomers);
    }

    private void CleanupCustomerList()
    {
        activeCustomers.RemoveAll(customer => customer == null);
    }
}