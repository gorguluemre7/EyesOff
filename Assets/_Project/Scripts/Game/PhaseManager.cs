using EyesOff.Network;
using EyesOff.Player;
using EyesOff.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace EyesOff.Game
{
    /// <summary>
    /// Runs the match: Waiting -> Dark -> (someone shoots) Panic -> Light -> Dark ... -> Finished.
    ///
    /// The server decides the phase and when it ends; clients only read the NetworkVariables
    /// and apply the visuals (lights, outlines) and local rules (frozen shooter, shooting allowed).
    /// Lives in the scene with a NetworkObject (in-scene placed network object).
    /// </summary>
    public class PhaseManager : NetworkBehaviour
    {
        public static PhaseManager Instance { get; private set; }

        [SerializeField] private GameSettings settings;
        [SerializeField] private RoomLighting roomLighting;

        public readonly NetworkVariable<GamePhase> Phase = new(GamePhase.WaitingForPlayers);
        public readonly NetworkVariable<double> PhaseEndTime = new();
        public readonly NetworkVariable<double> MatchEndTime = new();
        public readonly NetworkVariable<ulong> PanicShooterId = new(ulong.MaxValue);

        /// <summary>Shared clock: server time, kept in sync on clients by NGO.</summary>
        private double Now => NetworkManager.ServerTime.Time;

        public float PhaseTimeLeft => Mathf.Max(0f, (float)(PhaseEndTime.Value - Now));
        public float MatchTimeLeft => Mathf.Max(0f, (float)(MatchEndTime.Value - Now));

        /// <summary>Lights are on in every phase except Dark and Panic.</summary>
        public bool IsLitPhase => Phase.Value is not (GamePhase.Dark or GamePhase.Panic);

        private void Awake() => Instance = this;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            // In-scene objects keep their values between sessions, so start every session fresh.
            Phase.Value = GamePhase.WaitingForPlayers;
            PanicShooterId.Value = ulong.MaxValue;
            WeaponShooter.ServerShotFired += OnServerShotFired;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
                WeaponShooter.ServerShotFired -= OnServerShotFired;

            if (roomLighting != null) // may already be destroyed when quitting
                roomLighting.SetLit(true); // back to the menu look
        }

        private void Update()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
                ServerUpdatePhase();

            ApplyLocalState();
        }

        private void ServerUpdatePhase()
        {
            switch (Phase.Value)
            {
                case GamePhase.WaitingForPlayers:
                    if (NetworkManager.ConnectedClientsIds.Count >= ConnectionManager.MaxPlayers)
                    {
                        MatchEndTime.Value = Now + settings.matchDuration;
                        EnterPhase(GamePhase.Dark, 0f);
                    }
                    break;

                case GamePhase.Dark:
                    // The match only ends from Dark, so a running Panic/Light cycle is never cut short.
                    if (Now >= MatchEndTime.Value)
                        EnterPhase(GamePhase.Finished, 0f);
                    break;

                case GamePhase.Panic:
                    if (Now >= PhaseEndTime.Value)
                        EnterPhase(GamePhase.Light, settings.lightDuration);
                    break;

                case GamePhase.Light:
                    if (Now >= PhaseEndTime.Value)
                        EnterPhase(GamePhase.Dark, 0f);
                    break;
            }
        }

        private void OnServerShotFired(ulong shooterClientId)
        {
            if (Phase.Value != GamePhase.Dark)
                return;

            PanicShooterId.Value = shooterClientId;
            EnterPhase(GamePhase.Panic, settings.panicDuration);
        }

        private void EnterPhase(GamePhase phase, float duration)
        {
            PhaseEndTime.Value = Now + duration;
            Phase.Value = phase;
        }

        /// <summary>
        /// Every client, every frame: make lights and the local player match the current phase.
        /// Checking each frame (instead of OnValueChanged events) also covers late joiners and
        /// doesn't depend on the order in which NetworkVariable updates arrive.
        /// </summary>
        private void ApplyLocalState()
        {
            if (roomLighting.IsLit != IsLitPhase)
                roomLighting.SetLit(IsLitPhase);

            NetworkObject localPlayer = NetworkManager.LocalClient?.PlayerObject;
            if (localPlayer == null)
                return;

            GamePhase phase = Phase.Value;
            bool frozenShooter = phase == GamePhase.Panic && PanicShooterId.Value == NetworkManager.LocalClientId;

            localPlayer.GetComponent<PlayerController>().MovementLocked = frozenShooter || phase == GamePhase.Finished;
            localPlayer.GetComponent<WeaponShooter>().CanShoot = phase == GamePhase.Dark;
        }
    }
}
