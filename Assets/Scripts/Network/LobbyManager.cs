using UnityEngine;
using Unity.Netcode;

public class LobbyController : MonoBehaviour
{
    // Dipanggil saat tombol "Buat Sesi" ditekan
    public void StartHost()
    {
        // 1. Mulai jaringan sebagai Host
        NetworkManager.Singleton.StartHost();

        // 2. PINDAH SCENE MENGGUNAKAN NETCODE (Bukan SceneManager biasa)
        // Pastikan nama "GameScene" sesuai dengan nama file scene game Anda persis (huruf besar/kecilnya)
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // Dipanggil saat tombol "Bergabung Sesi" ditekan
    public void StartClient()
    {
        // Client hanya perlu konek. 
        // Karena "Enable Scene Management" aktif, Client akan OTOMATIS ditarik masuk ke GameScene menyusul Host!
        NetworkManager.Singleton.StartClient();
    }
}