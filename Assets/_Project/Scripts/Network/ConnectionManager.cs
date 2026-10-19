using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace EyesOff.Network
{
    /// <summary>
    /// Starts/stops the session (Host or Client) and limits the game to 2 players.
    /// Lives on the same GameObject as the NetworkManager.
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class ConnectionManager : MonoBehaviour
    {
        public const int MaxPlayers = 2;

        [SerializeField] private ushort port = 7777;

        /// <summary>Fired when the local session ends; carries a reason when the server rejected us.</summary>
        public event Action<string> SessionEnded;

        private NetworkManager _networkManager;
        private UnityTransport _transport;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();

            // Requires "Connection Approval" to be ticked on the NetworkManager.
            _networkManager.ConnectionApprovalCallback = ApproveConnection;
            _networkManager.OnClientStopped += OnClientStopped;
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
                _networkManager.OnClientStopped -= OnClientStopped;
        }

        public bool StartHost()
        {
            // Listen on every network interface so other machines on the LAN can join.
            _transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            return _networkManager.StartHost();
        }

        public bool StartClient(string hostIp)
        {
            _transport.SetConnectionData(string.IsNullOrWhiteSpace(hostIp) ? "127.0.0.1" : hostIp.Trim(), port);
            return _networkManager.StartClient();
        }

        public void Leave() => _networkManager.Shutdown();

        /// <summary>LAN IPv4 of this machine, shown in the menu so the joiner knows what to type.</summary>
        public static string GetLocalIPv4()
        {
            var address = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return address?.ToString() ?? "127.0.0.1";
        }

        // Runs on the server for every connection request (including the host's own).
        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
                                       NetworkManager.ConnectionApprovalResponse response)
        {
            bool hasRoom = _networkManager.ConnectedClientsIds.Count < MaxPlayers;
            response.Approved = hasRoom;
            response.CreatePlayerObject = hasRoom;
            response.Reason = hasRoom ? string.Empty : "Oda dolu (en fazla 2 oyuncu).";
        }

        private void OnClientStopped(bool wasHost)
        {
            SessionEnded?.Invoke(_networkManager.DisconnectReason);
        }
    }
}
