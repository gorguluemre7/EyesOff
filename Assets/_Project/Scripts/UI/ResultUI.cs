using System.Linq;
using System.Text;
using EyesOff.Game;
using EyesOff.Network;
using EyesOff.Player;
using EyesOff.Weapons;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace EyesOff.UI
{
    /// <summary>Shown when the match is Finished: shots per player and a button back to the menu.</summary>
    public class ResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button menuButton;

        private void Start()
        {
            menuButton.onClick.AddListener(() => NetworkManager.Singleton.GetComponent<ConnectionManager>().Leave());
            panel.SetActive(false);
        }

        private void Update()
        {
            PhaseManager phases = PhaseManager.Instance;
            bool finished = phases != null && phases.IsSpawned && phases.Phase.Value == GamePhase.Finished;

            if (panel.activeSelf == finished)
                return;

            panel.SetActive(finished);
            if (finished)
            {
                resultText.text = BuildResults();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private static string BuildResults()
        {
            var text = new StringBuilder("MAÇ BİTTİ\n\n");
            ulong localId = NetworkManager.Singleton.LocalClientId;

            foreach (WeaponShooter shooter in FindObjectsByType<WeaponShooter>(FindObjectsSortMode.None)
                         .OrderBy(s => s.OwnerClientId))
            {
                string you = shooter.OwnerClientId == localId ? " (sen)" : string.Empty;
                text.AppendLine($"{PlayerNames.For(shooter.OwnerClientId)}{you}: {shooter.ShotsFired.Value} atış");
            }

            return text.ToString();
        }
    }
}
