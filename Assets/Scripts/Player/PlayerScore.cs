using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Tempel di prefab Player (objek root yang punya NetworkObject)
public class PlayerScore : NetworkBehaviour
{
    // Daftar semua player yang sedang aktif
    public static readonly List<PlayerScore> All = new List<PlayerScore>();

    // Dipanggil setiap kali score player berubah
    public static event Action<PlayerScore> OnAnyScoreChanged;

    // Dipanggil ketika player masuk atau keluar
    public static event Action OnPlayersChanged;

    // Server yang menulis, semua client membaca
    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Host = Player 1, Client = Player 2, dst.
    public int PlayerNumber => (int)OwnerClientId + 1;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Pastikan tidak terdaftar dua kali
        if (!All.Contains(this))
        {
            All.Add(this);
        }

        // Dengarkan perubahan score di Host maupun Client
        Score.OnValueChanged += HandleScoreChanged;

        // Beritahu ScoreUI bahwa ada player baru
        OnPlayersChanged?.Invoke();

        // Paksa UI membaca nilai score awal
        OnAnyScoreChanged?.Invoke(this);

        Debug.Log(
            $"[PLAYER SCORE] Spawn | " +
            $"Player={PlayerNumber} | " +
            $"OwnerClientId={OwnerClientId} | " +
            $"Score={Score.Value} | " +
            $"IsServer={IsServer} | " +
            $"IsClient={IsClient}"
        );
    }

    public override void OnNetworkDespawn()
    {
        Score.OnValueChanged -= HandleScoreChanged;

        All.Remove(this);

        OnPlayersChanged?.Invoke();

        base.OnNetworkDespawn();
    }

    private void HandleScoreChanged(
        int previousValue,
        int newValue)
    {
        Debug.Log(
            $"[PLAYER SCORE] " +
            $"Player {PlayerNumber}: " +
            $"{previousValue} -> {newValue}"
        );

        OnAnyScoreChanged?.Invoke(this);
    }

    // Hanya boleh dipanggil oleh Server/Host
    public void AddScoreServer(int points)
    {
        if (!IsServer)
            return;

        Score.Value += points;

        Debug.Log(
            $"[PLAYER SCORE] " +
            $"Player {PlayerNumber} mendapat {points} poin. " +
            $"Total Score = {Score.Value}"
        );
    }
}