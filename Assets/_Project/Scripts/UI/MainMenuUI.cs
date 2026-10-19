using EyesOff.Network;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace EyesOff.UI
{
    /// <summary>
    /// Main menu: IP field + Host / Join buttons. Hides itself while a session is running.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField ipInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text localIpText;

        private ConnectionManager _connectionManager;

        private void Start()
        {
            // Looked up at runtime so this UI can live in a prefab (prefabs can't reference scene objects).
            _connectionManager = NetworkManager.Singleton.GetComponent<ConnectionManager>();

            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            _connectionManager.SessionEnded += OnSessionEnded;

            localIpText.text = $"Bu bilgisayarın IP adresi: {ConnectionManager.GetLocalIPv4()}";
            if (string.IsNullOrEmpty(ipInput.text))
                ipInput.text = "127.0.0.1";

            Show(string.Empty);
        }

        private void OnDestroy()
        {
            if (_connectionManager != null)
                _connectionManager.SessionEnded -= OnSessionEnded;
        }

        private void OnHostClicked()
        {
            if (_connectionManager.StartHost())
                panel.SetActive(false);
            else
                statusText.text = "Host başlatılamadı. Port kullanımda olabilir.";
        }

        private void OnJoinClicked()
        {
            if (_connectionManager.StartClient(ipInput.text))
                panel.SetActive(false);
            else
                statusText.text = "Bağlantı başlatılamadı.";
        }

        private void OnSessionEnded(string reason)
        {
            Show(string.IsNullOrEmpty(reason) ? "Bağlantı kapandı." : reason);
        }

        private void Show(string status)
        {
            panel.SetActive(true);
            statusText.text = status;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
