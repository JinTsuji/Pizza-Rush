using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;

public class LobbyController : MonoBehaviour
{
    [Header("UI")]
    // Kotak tempat client mengetik IP host (kosong = 127.0.0.1, hanya untuk tes di satu PC)
    [FormerlySerializedAs("joinCodeInput")]
    [SerializeField] private TMP_InputField ipInput;

    // Teks tempat host melihat IP-nya sendiri
    [FormerlySerializedAs("joinCodeText")]
    [SerializeField] private TMP_Text ipText;

    // Pesan status (opsional)
    [SerializeField] private TMP_Text statusText;

    [Header("Setting")]
    [SerializeField] private ushort port = 7777;
    [SerializeField] private string gameSceneName = "GameScene";

    private void SetStatus(string message)
    {
        Debug.Log("[LOBBY] " + message);
        if (statusText != null) statusText.text = message;
    }

    // Dipanggil saat tombol "Buat Sesi" ditekan
    public void StartHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // 0.0.0.0 supaya host bisa dihubungi dari komputer lain, bukan hanya dari komputer sendiri
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");

        if (!NetworkManager.Singleton.StartHost())
        {
            SetStatus("Gagal membuat sesi.");
            return;
        }

        // Pindah ke GameScene setelah client SELESAI sinkronisasi (bukan sekadar tersambung)
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnClientSynchronized;

        if (ipText != null)
            ipText.text = "IP kamu:\n" + string.Join("\n", GetLocalIPv4Addresses());

        SetStatus("Sesi dibuat. Beri tahu IP di atas ke temanmu. Game mulai otomatis saat dia masuk.");
    }

    private void OnClientSynchronized(ulong clientId)
    {
        // Abaikan host itu sendiri
        if (clientId == NetworkManager.Singleton.LocalClientId)
            return;

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnClientSynchronized;

        // Pindah scene lewat Netcode supaya client ikut
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    // Dipanggil saat tombol "Bergabung Sesi" ditekan
    public void StartClient()
    {
        string ip = ipInput != null ? ipInput.text.Trim() : "";
        if (string.IsNullOrEmpty(ip))
            ip = "127.0.0.1";

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        // Karena "Enable Scene Management" aktif, client otomatis ditarik ke GameScene oleh Host
        if (NetworkManager.Singleton.StartClient())
            SetStatus("Menyambung ke " + ip + " ...");
        else
            SetStatus("Gagal memulai client.");
    }

    // Daftar IPv4 komputer ini (WiFi/LAN, dan adapter VPN seperti Radmin/Tailscale kalau ada)
    private static List<string> GetLocalIPv4Addresses()
    {
        var result = new List<string>();
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress address in host.AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                result.Add(address.ToString());
        }

        if (result.Count == 0)
            result.Add("(tidak ditemukan)");

        return result;
    }
}