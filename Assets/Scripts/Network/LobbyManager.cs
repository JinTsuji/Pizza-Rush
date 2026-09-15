using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public void StartHost()
    {
        // Jika sebelumnya sudah menekan tombol Client/Host, matikan koneksi lamanya terlebih dahulu
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Debug.Log("Membuat Game Session (Host)... Memuat GameScene.");

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void StartClient()
    {
        // Jika sebelumnya sudah menekan tombol Client/Host, matikan koneksi lamanya terlebih dahulu
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Debug.Log("Bergabung ke Sesi (Client)... Menunggu Host.");

        NetworkManager.Singleton.StartClient();
    }
}