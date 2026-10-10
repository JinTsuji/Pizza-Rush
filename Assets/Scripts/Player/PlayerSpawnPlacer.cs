using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tempel di ROOT prefab Player (bersama NetworkObject dan PlayerInteraction).
// Player dibuat saat masih di lobby, jadi posisinya harus dipindah ke titik awal setelah GameScene dimuat.
public class PlayerSpawnPlacer : NetworkBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Cadangan kalau objek PlayerSpawn1/PlayerSpawn2 tidak ada di scene")]
    [SerializeField] private Vector3 fallbackPosition = new Vector3(0f, 1f, -3f);
    [SerializeField] private float fallbackSpacing = 2f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        NetworkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;

        // Kalau player baru muncul saat sudah berada di GameScene
        if (SceneManager.GetActiveScene().name == gameSceneName)
            PlaceAtSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (clientId != OwnerClientId) return;
        if (sceneName != gameSceneName) return;

        PlaceAtSpawn();
    }

    private void PlaceAtSpawn()
    {
        // Host = PlayerSpawn1, client berikutnya = PlayerSpawn2, dst.
        int playerNumber = (int)OwnerClientId + 1;
        GameObject spawn = GameObject.Find("PlayerSpawn" + playerNumber);

        Vector3 position;
        Quaternion rotation;

        if (spawn != null)
        {
            position = spawn.transform.position;
            rotation = spawn.transform.rotation;
        }
        else
        {
            position = fallbackPosition + Vector3.right * (fallbackSpacing * OwnerClientId);
            rotation = Quaternion.identity;
            Debug.LogWarning("[SPAWN] PlayerSpawn" + playerNumber + " tidak ditemukan, memakai posisi cadangan.");
        }

        var controller = GetComponent<CharacterController>();
        var body = GetComponent<Rigidbody>();
        var netTransform = GetComponent<NetworkTransform>();

        if (controller != null) controller.enabled = false;

        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        if (netTransform != null)
            netTransform.Teleport(position, rotation, transform.localScale);
        else
            transform.SetPositionAndRotation(position, rotation);

        if (controller != null) controller.enabled = true;

        Debug.Log("[SPAWN] Player " + playerNumber + " ditempatkan di " + position);
    }
}