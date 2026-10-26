using System;
using Unity.Netcode;
using UnityEngine;

namespace BlindRunway
{
    public enum Phase { Menu, Lobby, Intro, Playing, Result }

    // Multiplayer: three clients, one seat each. Solo: one human on a host-only server holding all three seats.
    public enum MatchMode { Multiplayer, Solo }

    [Serializable]
    public class LobbyState
    {
        public long[] seat = { -1, -1, -1 }; // client id per Role, -1 = open
        public bool[] ready = new bool[3];
        public int players;
        public int level;
        public string code = "";
        public MatchMode mode;
        public bool Solo => mode == MatchMode.Solo;
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
        public const string Lobby = "br.lobby", Act = "br.act", Phase = "br.phase", Input = "br.input", Snap = "br.snap", Result = "br.result";
    }

    // Lobby + match flow. Server half keeps the authoritative state (srv*), client half mirrors what it is sent.
    public class Session : MonoBehaviour
    {
        public Phase Phase { get; private set; } = Phase.Menu;
        public LobbyState Lobby = new LobbyState();
        public int Level;
        public event Action Changed;
        public event Action<Role, InputMsg> InputReceived; // server: validated input per seat
        public Role SoloView = Role.Pilot;
        public Snapshot Snap;          // latest snapshot for this client
        public LevelDefinition Def;    // level being played
        public int PickedLevel;        // chosen on the route map before hosting
        public ResultMsg Result;       // last finished attempt
        float sendT;

        Match match;                   // server only
        Role soloSeat = Role.Pilot;    // server: seat the solo player is driving right now
        float simAcc, snapT, introT;
        readonly bool[] gotIt = new bool[3];
        const float IntroTimeout = 30;
        const float SimDt = 0.02f, SnapInterval = 0.05f;

        LobbyState srv = new LobbyState();
        Phase srvPhase = Phase.Menu;

        public bool IsHost => Net.IsServer;

        public Role MyRole
        {
            get
            {
                if (Net.NM == null || !Net.Running) return Role.None;
                if (Lobby.Solo) return SoloView;
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
            Net.On(Msg.Input, OnInput);
            Net.On(Msg.Snap, (s, j) => Snap = JsonUtility.FromJson<Snapshot>(j));
            Net.On(Msg.Result, (s, j) =>
            {
                Result = JsonUtility.FromJson<ResultMsg>(j);
                Progress.Record(Result);
            });
            InputReceived += (r, m) => match?.Apply(r, m);
            Net.AcceptingPlayers = () => srvPhase == Phase.Lobby;
        }

        void Start()
        {
            Net.NM.OnClientConnectedCallback += OnConnected;
            Net.NM.OnClientDisconnectCallback += OnDisconnected;
        }

        void Update()
        {
            if (Net.IsServer && srvPhase == Phase.Playing && match != null) ServerTick(Time.deltaTime);
            if (Net.IsServer && srvPhase == Phase.Intro && (introT += Time.deltaTime) > IntroTimeout) BeginFlight();
            if (Phase != Phase.Playing) return;
            if (Lobby.Solo)
            {
                if (Input.GetKeyDown(KeyCode.F1)) SwitchSeat(Role.Pilot);
                if (Input.GetKeyDown(KeyCode.F2)) SwitchSeat(Role.Navigator);
                if (Input.GetKeyDown(KeyCode.F3)) SwitchSeat(Role.Cabin);
            }
            var me = MyRole;
            if (me == Role.None) return;
            Controls.Poll(me);
            sendT += Time.deltaTime;
            if (sendT < 0.05f) return;
            sendT = 0;
            Net.ToServer(Msg.Input, Controls.Take(me));
        }

        // ---- client API ----

        public void Host() => Host(false);

        // Single player: host-only listen server, straight into the match, no lobby.
        public void PlayAlone(int level)
        {
            PickedLevel = level;
            Host(true);
        }

        void Host(bool solo)
        {
            if (!Net.Host())
            {
                App.I.Toast("Could not host (is port 7777 busy?)");
                return;
            }
            srv = new LobbyState { code = Net.ToCode(Net.LocalIp()), level = Mathf.Min(PickedLevel, Progress.Unlocked) };
            if (!solo)
            {
                SetPhase(Phase.Lobby, null);
                return;
            }
            for (int i = 0; i < 3; i++) srv.seat[i] = (long)NetworkManager.ServerClientId;
            srv.mode = MatchMode.Solo;
            StartMatch();
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
            App.I.Overlay.Close();
            Phase = srvPhase = Phase.Menu;
            Net.Shutdown();
            Lobby = new LobbyState();
            Changed?.Invoke();
            if (why != null) App.I.Toast(why);
        }

        // Solo hot-seat: the seat you leave goes idle (neutral input), the new one takes over keyboard and HUD.
        public void SwitchSeat(Role seat)
        {
            if (!Lobby.Solo || seat == SoloView || seat < Role.Pilot || seat > Role.Cabin) return;
            Controls.Take(SoloView); // drop half-latched presses from the old seat
            if (Phase == Phase.Playing) Net.ToServer(Msg.Input, new InputMsg { role = (int)SoloView });
            SoloView = seat;
            sendT = 1; // announce the new seat on the next frame so its snapshot follows quickly
            Changed?.Invoke();
        }

        public void Act(string act, int role = 0, int level = 0) =>
            Net.ToServer(Msg.Act, new LobbyAction { act = act, role = role, level = level });

        void OnPhase(PhaseMsg m)
        {
            bool entering = m.phase == Phase.Intro || m.phase == Phase.Playing;
            if (entering && Phase != Phase.Intro && Phase != Phase.Playing)
            {
                Snap = null;
                Def = Levels.Get(m.level);
            }
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
            if (srvPhase != Phase.Lobby)
            {
                match = null;
                SetPhase(Phase.Lobby, "A crewmate left. Back to the lobby.");
                return;
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
                case "level":
                    if (!IsHostSender(sender) || srvPhase != Phase.Lobby) return;
                    srv.level = Mathf.Clamp(a.level, 0, Progress.Unlocked); // host's unlocks gate the campaign
                    break;
                case "ready":
                    if (srvPhase != Phase.Lobby || mine < 0) return;
                    srv.ready[mine] = !srv.ready[mine];
                    break;
                case "solo": // dev SOLO TEST from a lobby with nobody else in it
                    if (!IsHostSender(sender) || srvPhase != Phase.Lobby || Net.PlayerCount != 1) return;
                    for (int i = 0; i < 3; i++) srv.seat[i] = (long)sender;
                    srv.mode = MatchMode.Solo;
                    StartMatch();
                    return;
                case "gotit":
                    if (srvPhase != Phase.Intro) return;
                    for (int i = 0; i < 3; i++)
                        if (srv.seat[i] == (long)sender) gotIt[i] = true; // solo host holds all three
                    if (gotIt[0] && gotIt[1] && gotIt[2]) BeginFlight();
                    return;
                case "retry":
                    if (!IsHostSender(sender) || srvPhase != Phase.Result) return;
                    StartMatch();
                    return;
                case "next":
                    if (!IsHostSender(sender) || srvPhase != Phase.Result || srv.level + 1 > Progress.Unlocked) return;
                    srv.level++;
                    StartMatch();
                    return;
                case "lobby":
                    if (!IsHostSender(sender) || srvPhase != Phase.Result || srv.Solo) return; // solo leaves via Leave()
                    if (srv.Solo) srv.seat = new long[] { -1, -1, -1 };
                    srv.mode = MatchMode.Multiplayer;
                    SetPhase(Phase.Lobby, null);
                    return;
                case "start":
                    if (!IsHostSender(sender) || srvPhase != Phase.Lobby || !CanStart(srv)) return;
                    StartMatch();
                    return;
                default:
                    return;
            }
            Push();
        }

        // Ownership rule. Multiplayer: a client drives only the seat it holds.
        // Solo: the one local player holds all three seats and names the one it is driving.
        public static Role SeatFor(LobbyState L, ulong sender, int claimedRole)
        {
            if (L.Solo)
            {
                bool ownsAll = L.seat[0] == (long)sender && L.seat[1] == (long)sender && L.seat[2] == (long)sender;
                return ownsAll && claimedRole >= 0 && claimedRole <= 2 ? (Role)claimedRole : Role.None;
            }
            return (Role)Array.IndexOf(L.seat, (long)sender);
        }

        void OnInput(ulong sender, string json)
        {
            if (!Net.IsServer || srvPhase != Phase.Playing) return;
            var m = JsonUtility.FromJson<InputMsg>(json);
            var role = SeatFor(srv, sender, m.role);
            if (role == Role.None) return;
            if (srv.Solo) soloSeat = role;
            if (m.say != null && m.say.Length > 40) m.say = m.say.Substring(0, 40);
            InputReceived?.Invoke(role, m);
        }

        // Multiplayer: exactly three connected players, every seat taken, everyone ready. Solo: always.
        public static bool CanStart(LobbyState L)
        {
            if (L.Solo) return true;
            if (L.players != Net.MaxPlayers) return false;
            for (int i = 0; i < 3; i++)
                if (L.seat[i] == -1 || !L.ready[i]) return false;
            return true;
        }

        void StartMatch()
        {
            for (int i = 0; i < 3; i++) srv.ready[i] = false;
            match = new Match(Levels.Get(srv.level));
            simAcc = snapT = introT = 0;
            soloSeat = SoloView = Role.Pilot;
            for (int i = 0; i < 3; i++) gotIt[i] = false;
            SetPhase(Phase.Intro, null);
        }

        void BeginFlight()
        {
            if (srvPhase != Phase.Intro) return;
            SetPhase(Phase.Playing, null);
        }

        void ServerTick(float dt)
        {
            simAcc += Mathf.Min(dt, 0.25f);
            while (simAcc >= SimDt)
            {
                match.Step(SimDt);
                simAcc -= SimDt;
            }
            var result = LevelRules.Check(match);
            if (result != null)
            {
                EndMatch(result);
                return;
            }
            snapT += dt;
            if (snapT < SnapInterval) return;
            snapT = 0;
            SendSnapshots();
        }

        void EndMatch(ResultMsg r)
        {
            r.level = srv.level;
            r.campaignClear = r.win && srv.level == Levels.Count - 1;
            SendSnapshots(); // final frame for the freeze-frame
            Net.ToAll(Msg.Result, r);
            match = null;
            SetPhase(Phase.Result, null);
        }

        void SendSnapshots()
        {
            if (srv.Solo) // only the seat being driven: solo never shows the Pilot numbers
            {
                Net.Send(NetworkManager.ServerClientId, Msg.Snap, match.SnapshotFor(soloSeat));
                return;
            }
            for (int i = 0; i < 3; i++)
                if (srv.seat[i] >= 0 && !Net.Gone.Contains((ulong)srv.seat[i]))
                    Net.Send((ulong)srv.seat[i], Msg.Snap, match.SnapshotFor((Role)i));
        }

        void SetPhase(Phase p, string note)
        {
            srvPhase = p;
            Push(); // seats first, so screens built on the phase change see who sits where
            Net.ToAll(Msg.Phase, new PhaseMsg { phase = p, level = srv.level, note = note });
        }

        void Push()
        {
            srv.players = Net.PlayerCount;
            Net.ToAll(Msg.Lobby, srv);
        }

        bool IsHostSender(ulong sender) => sender == NetworkManager.ServerClientId;
    }
}
