using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactDistance = 2f;

    [Header("Pizza")]
    [SerializeField] private Transform holdPoint;

    private NetworkVariable<ulong> heldPizzaId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Pizza heldPizza;

    public override void OnNetworkSpawn()
    {
        heldPizzaId.OnValueChanged += OnHeldPizzaChanged;

        UpdateHeldPizzaReference(heldPizzaId.Value);
    }

    public override void OnNetworkDespawn()
    {
        heldPizzaId.OnValueChanged -= OnHeldPizzaChanged;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleInteraction();
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryDropPizza();
        }
    }

    private void HandleInteraction()
    {
        if (heldPizza != null)
        {
            Debug.Log(
                "Player membawa " +
                heldPizza.pizzaType
            );

            // Untuk sementara kita hanya test pickup.
            // Serve Customer akan kita aktifkan setelah pickup berhasil.
        }
        else
        {
            TryPickupPizza();
        }
    }

    private void TryPickupPizza()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactDistance
        );

        Pizza closestPizza = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            Pizza pizza =
                col.GetComponentInParent<Pizza>();

            if (pizza == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                pizza.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPizza = pizza;
            }
        }

        if (closestPizza == null)
        {
            Debug.Log(
                "Tidak ada Pizza di dekat Player."
            );

            return;
        }

        Debug.Log(
            "Meminta Server mengambil Pizza: " +
            closestPizza.pizzaType
        );

        RequestPickupPizzaRpc(
            closestPizza.NetworkObject.NetworkObjectId
        );
    }

    [Rpc(SendTo.Server)]
    private void RequestPickupPizzaRpc(
        ulong pizzaNetworkObjectId
    )
    {
        if (heldPizzaId.Value != ulong.MaxValue)
        {
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                pizzaNetworkObjectId,
                out NetworkObject networkObject))
        {
            Debug.LogWarning(
                "Pizza tidak ditemukan oleh Server."
            );

            return;
        }

        Pizza pizza =
            networkObject.GetComponent<Pizza>();

        if (pizza == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            pizza.transform.position
        );

        if (distance > interactDistance)
        {
            Debug.LogWarning(
                "Player terlalu jauh dari Pizza."
            );

            return;
        }

        // Server mengambil Pizza
        bool success = pizza.PickUpServer(holdPoint);

        if (!success)
        {
            Debug.LogWarning(
                "Pickup Pizza gagal."
            );

            return;
        }

        // Simpan ID Pizza
        heldPizzaId.Value =
            pizza.NetworkObject.NetworkObjectId;

        Debug.Log(
            "Server menerima pickup: " +
            pizza.pizzaType
        );
    }

    private void OnHeldPizzaChanged(
        ulong previousValue,
        ulong newValue
    )
    {
        UpdateHeldPizzaReference(newValue);
    }

    private void UpdateHeldPizzaReference(
        ulong networkObjectId
    )
    {
        if (networkObjectId == ulong.MaxValue)
        {
            heldPizza = null;
            return;
        }

        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                networkObjectId,
                out NetworkObject networkObject))
        {
            heldPizza =
                networkObject.GetComponent<Pizza>();
        }
    }

    private void TryDropPizza()
    {
        if (heldPizza == null)
        {
            Debug.Log(
                "Player tidak membawa Pizza."
            );

            return;
        }

        RequestDropPizzaRpc(
            heldPizza.NetworkObject.NetworkObjectId
        );
    }

    [Rpc(SendTo.Server)]
    private void RequestDropPizzaRpc(
        ulong pizzaNetworkObjectId
    )
    {
        if (heldPizzaId.Value != pizzaNetworkObjectId)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                pizzaNetworkObjectId,
                out NetworkObject networkObject))
        {
            return;
        }

        Pizza pizza =
            networkObject.GetComponent<Pizza>();

        if (pizza == null)
            return;

        Vector3 dropPosition =
            transform.position +
            transform.forward;

        pizza.DropServer(dropPosition);

        heldPizzaId.Value = ulong.MaxValue;

        Debug.Log(
            "Pizza berhasil dijatuhkan."
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactDistance
        );
    }
}