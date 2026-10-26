using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BlindRunway
{
    // Netcode bootstrap + JSON named messages. No NetworkObjects/prefabs: the server simulates and sends
    // snapshots, clients send input. Keeps everything code-built and ownership rules in one place (Session).
    // ponytail: join code is the host's LAN IPv4 in hex; swap to Unity Relay join codes for internet play.
    public static class Net
    {
        public const ushort Port = 7777;
        public const int MaxPlayers = 3;
        public static NetworkManager NM;
        public static Func<bool> AcceptingPlayers = () => true;
        static UnityTransport transport;
        static readonly Dictionary<string, Action<ulong, string>> handlers = new Dictionary<string, Action<ulong, string>>();

        public static void Init()
        {
            var go = new GameObject("Network");
            go.SetActive(false); // configure before NetworkManager.Awake
            UnityEngine.Object.DontDestroyOnLoad(go); // NGO expects a root object
            transport = go.AddComponent<UnityTransport>();
            NM = go.AddComponent<NetworkManager>();
            NM.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                EnableSceneManagement = false,
            };
            NM.ConnectionApprovalCallback = (req, res) =>
            {
                bool full = PlayerCount >= MaxPlayers;
                res.Approved = !full && AcceptingPlayers();
                res.CreatePlayerObject = false;
                if (!res.Approved) res.Reason = full ? "Crew is full (3/3)." : "Match already in progress.";
            };
            go.SetActive(true);
        }

        public static bool Running => NM != null && NM.IsListening;
        public static bool IsServer => NM != null && NM.IsServer;

        public static bool Host()
        {
            transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            Gone.Clear();
            if (!NM.StartHost()) return false;
            RegisterHandlers();
            return true;
        }

        public static bool Join(string ip)
        {
            transport.SetConnectionData(ip, Port);
            if (!NM.StartClient()) return false;
            RegisterHandlers();
            return true;
        }

        public static void Shutdown()
        {
            if (NM != null && NM.IsListening) NM.Shutdown();
        }

        // ---- messaging ----

        public static void On(string name, Action<ulong, string> handler) => handlers[name] = handler;

        static void RegisterHandlers()
        {
            foreach (var key in handlers.Keys)
            {
                var name = key;
                NM.CustomMessagingManager.RegisterNamedMessageHandler(name, (sender, reader) =>
                {
                    reader.ReadValueSafe(out string json);
                    Dispatch(name, sender, json);
                });
            }
        }

        static void Dispatch(string name, ulong sender, string json)
        {
            if (handlers.TryGetValue(name, out var h)) h(sender, json);
        }

        public static void Send(ulong target, string name, object msg)
        {
            var json = JsonUtility.ToJson(msg);
            if (target == NM.LocalClientId)
            {
                Dispatch(name, target, json); // host talking to itself skips the transport
                return;
            }
            using (var w = new FastBufferWriter(256, Allocator.Temp, 1 << 16))
            {
                w.WriteValueSafe(json);
                NM.CustomMessagingManager.SendNamedMessage(name, target, w, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        public static void ToServer(string name, object msg) => Send(NetworkManager.ServerClientId, name, msg);

        // Ids that disconnected this frame can still be listed while callbacks run.
        public static readonly HashSet<ulong> Gone = new HashSet<ulong>();

        public static int PlayerCount
        {
            get
            {
                int n = 0;
                foreach (var id in NM.ConnectedClientsIds) if (!Gone.Contains(id)) n++;
                return n;
            }
        }

        public static void ToAll(string name, object msg)
        {
            foreach (var id in NM.ConnectedClientsIds)
                if (!Gone.Contains(id)) Send(id, name, msg);
        }

        // ---- join codes ----

        public static string LocalIp()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var a in ni.GetIPProperties().UnicastAddresses)
                        if (a.Address.AddressFamily == AddressFamily.InterNetwork) return a.Address.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("LAN IP lookup failed: " + e.Message);
            }
            return "127.0.0.1";
        }

        public static string ToCode(string ip)
        {
            var b = IPAddress.Parse(ip).GetAddressBytes();
            return $"{b[0]:X2}{b[1]:X2}-{b[2]:X2}{b[3]:X2}";
        }

        // Accepts a code (C0A8-0105), a raw IPv4, or blank for same machine.
        public static bool TryParseCode(string code, out string ip)
        {
            ip = null;
            code = (code ?? "").Trim();
            if (code.Length == 0)
            {
                ip = "127.0.0.1";
                return true;
            }
            if (code.Contains("."))
            {
                if (!IPAddress.TryParse(code, out var addr) || addr.AddressFamily != AddressFamily.InterNetwork) return false;
                ip = addr.ToString();
                return true;
            }
            code = code.Replace("-", "").ToUpperInvariant();
            if (code.Length != 8) return false;
            var bytes = new byte[4];
            for (int i = 0; i < 4; i++)
                if (!byte.TryParse(code.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                    return false;
            ip = new IPAddress(bytes).ToString();
            return true;
        }
    }
}
