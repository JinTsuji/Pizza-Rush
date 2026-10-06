using TMPro;
using UnityEngine;

// Tempel di objek UI di Canvas scene game,
// lalu drag dua teks TMP ke Inspector
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text player1Text;
    [SerializeField] private TMP_Text player2Text;

    private void OnEnable()
    {
        PlayerScore.OnAnyScoreChanged += OnScoreChanged;
        PlayerScore.OnPlayersChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        PlayerScore.OnAnyScoreChanged -= OnScoreChanged;
        PlayerScore.OnPlayersChanged -= Refresh;
    }

    private void OnScoreChanged(PlayerScore changed)
    {
        Refresh();
    }

    private void Refresh()
    {
        // Default
        if (player1Text != null)
            player1Text.text = "Player 1: 0";

        if (player2Text != null)
            player2Text.text = "Player 2: 0";

        // Baca semua PlayerScore yang sudah terdaftar
        foreach (PlayerScore p in PlayerScore.All)
        {
            if (p == null)
                continue;

            if (p.PlayerNumber == 1)
            {
                if (player1Text != null)
                {
                    player1Text.text =
                        $"Player 1: {p.Score.Value}";
                }
            }
            else if (p.PlayerNumber == 2)
            {
                if (player2Text != null)
                {
                    player2Text.text =
                        $"Player 2: {p.Score.Value}";
                }
            }
        }
    }
}