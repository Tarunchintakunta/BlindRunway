using System;
using Unity.Netcode;
using UnityEngine;

namespace BlindRunway
{
    public enum Phase { Menu, Lobby, Intro, Playing, Result }

    [Serializable]
    public class LobbyState
    {
        public long[] seat = { -1, -1, -1 }; // client id per Role, -1 = open
        public bool[] ready = new bool[3];
        public int players;
        public int level;
        public string code = "";
    }

    [Serializable]
    public class LobbyAction
    {
        public string act;
        public int role;
        public int level;
    }

    [Serializable]
    public class PhaseMsg
    {
        public Phase phase;
        public int level;
        public string note;
    }

    public static class Msg
    {
        public const string Lobby = "br.lobby", Act = "br.act", Phase = "br.phase";
    }

    // Lobby + match flow. Server half keeps the authoritative state (srv*), client half mirrors what it is sent.
    public class Session : MonoBehaviour
    {
        public Phase Phase { get; private set; } = Phase.Menu;
        public LobbyState Lobby = new LobbyState();
        public int Level;
        public event Action Changed;

        LobbyState srv = new LobbyState();
        Phase srvPhase = Phase.Menu;

        public bool IsHost => Net.IsServer;

        public Role MyRole
        {
            get
            {
                if (Net.NM == null || !Net.Running) return Role.None;
                return (Role)Array.IndexOf(Lobby.seat, (long)Net.NM.LocalClientId);
            }
        }

        void Awake()
        {
            Net.On(Msg.Lobby, (s, j) =>
            {
                Lobby = JsonUtility.FromJson<LobbyState>(j);
                Changed?.Invoke();
            });
            Net.On(Msg.Phase, (s, j) => OnPhase(JsonUtility.FromJson<PhaseMsg>(j)));
            Net.On(Msg.Act, OnAct);
            Net.AcceptingPlayers = () => srvPhase == Phase.Lobby;
        }

        void Start()
        {
            Net.NM.OnClientConnectedCallback += OnConnected;
            Net.NM.OnClientDisconnectCallback += OnDisconnected;
        }

        // ---- client API ----

        public void Host()
        {
            if (!Net.Host())
            {
                App.I.Toast("Could not host (is port 7777 busy?)");
                return;
            }
            srv = new LobbyState { code = Net.ToCode(Net.LocalIp()) };
            SetPhase(Phase.Lobby, null);
        }

        public void Join(string code)
        {
            if (!Net.TryParseCode(code, out var ip))
            {
                App.I.Toast("That join code looks wrong.");
                return;
            }
            if (!Net.Join(ip))
            {
                App.I.Toast("Could not start the client.");
                return;
            }
            Lobby = new LobbyState();
            Phase = Phase.Lobby;
            Changed?.Invoke();
        }

        public void Leave(string why = null)
        {
            Phase = srvPhase = Phase.Menu;
            Net.Shutdown();
            Lobby = new LobbyState();
            Changed?.Invoke();
            if (why != null) App.I.Toast(why);
        }

        public void Act(string act, int role = 0, int level = 0) =>
            Net.ToServer(Msg.Act, new LobbyAction { act = act, role = role, level = level });

        void OnPhase(PhaseMsg m)
        {
            Phase = m.phase;
            Level = m.level;
            if (!string.IsNullOrEmpty(m.note)) App.I.Toast(m.note);
            Changed?.Invoke();
        }

        // ---- server ----

        void OnConnected(ulong id)
        {
            if (!Net.IsServer) return;
            Net.Gone.Remove(id);
            Net.Send(id, Msg.Phase, new PhaseMsg { phase = srvPhase, level = srv.level });
            Push();
        }

        void OnDisconnected(ulong id)
        {
            if (Phase == Phase.Menu) return;
            if (!Net.IsServer)
            {
                var reason = Net.NM.DisconnectReason;
                if (string.IsNullOrEmpty(reason))
                    reason = Lobby.players == 0 ? "Could not reach that host." : "Lost connection to the host.";
                Leave(reason);
                return;
            }
            Net.Gone.Add(id);
            int seat = Array.IndexOf(srv.seat, (long)id);
            if (seat >= 0)
            {
                srv.seat[seat] = -1;
                srv.ready[seat] = false;
            }
            Push();
        }

        void OnAct(ulong sender, string json)
        {
            if (!Net.IsServer) return;
            var a = JsonUtility.FromJson<LobbyAction>(json);
            int mine = Array.IndexOf(srv.seat, (long)sender);
            switch (a.act)
            {
                case "claim":
                    if (srvPhase != Phase.Lobby || a.role < 0 || a.role > 2 || srv.seat[a.role] != -1) return;
                    if (mine >= 0)
                    {
                        srv.seat[mine] = -1;
                        srv.ready[mine] = false;
                    }
                    srv.seat[a.role] = (long)sender;
                    break;
                case "ready":
                    if (srvPhase != Phase.Lobby || mine < 0) return;
                    srv.ready[mine] = !srv.ready[mine];
                    break;
                case "start":
                    if (!IsHostSender(sender) || srvPhase != Phase.Lobby || !CanStart(srv)) return;
                    StartMatch();
                    return;
                default:
                    return;
            }
            Push();
        }

        // Exactly three connected players, every seat taken, everyone ready.
        public static bool CanStart(LobbyState L)
        {
            if (L.players != Net.MaxPlayers) return false;
            for (int i = 0; i < 3; i++)
                if (L.seat[i] == -1 || !L.ready[i]) return false;
            return true;
        }

        void StartMatch()
        {
            for (int i = 0; i < 3; i++) srv.ready[i] = false;
            SetPhase(Phase.Playing, null);
        }

        void SetPhase(Phase p, string note)
        {
            srvPhase = p;
            Net.ToAll(Msg.Phase, new PhaseMsg { phase = p, level = srv.level, note = note });
            Push();
        }

        void Push()
        {
            srv.players = Net.PlayerCount;
            Net.ToAll(Msg.Lobby, srv);
        }

        bool IsHostSender(ulong sender) => sender == NetworkManager.ServerClientId;
    }
}
