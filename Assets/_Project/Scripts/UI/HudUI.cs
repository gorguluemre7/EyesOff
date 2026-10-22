using EyesOff.Game;
using TMPro;
using UnityEngine;

namespace EyesOff.UI
{
    /// <summary>In-game HUD: current phase, its countdown and the match timer.</summary>
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text phaseTimerText;
        [SerializeField] private TMP_Text matchTimerText;

        private void Update()
        {
            PhaseManager phases = PhaseManager.Instance;
            bool show = phases != null && phases.IsSpawned && phases.Phase.Value != GamePhase.Finished;

            if (panel.activeSelf != show)
                panel.SetActive(show);
            if (!show)
                return;

            GamePhase phase = phases.Phase.Value;
            phaseText.text = phase switch
            {
                GamePhase.WaitingForPlayers => "Rakip bekleniyor...",
                GamePhase.Dark => "KARANLIK",
                GamePhase.Panic => "PANIC TIME!",
                GamePhase.Light => "IŞIKLAR AÇIK",
                _ => string.Empty
            };

            // Dark lasts until someone shoots, so only Panic and Light have a countdown.
            bool timed = phase is GamePhase.Panic or GamePhase.Light;
            phaseTimerText.text = timed ? phases.PhaseTimeLeft.ToString("0.0") : string.Empty;

            matchTimerText.text = phase == GamePhase.WaitingForPlayers
                ? string.Empty
                : $"Maç: {Mathf.CeilToInt(phases.MatchTimeLeft)} sn";
        }
    }
}
