using NUnit.Framework;

namespace BlindRunway.Tests
{
    public class SoloModeTests
    {
        static LobbyState Multi(long pilot, long nav, long cabin, int players = 3) =>
            new LobbyState { seat = new[] { pilot, nav, cabin }, ready = new[] { true, true, true }, players = players };

        static LobbyState Solo() =>
            new LobbyState { seat = new long[] { 0, 0, 0 }, players = 1, mode = MatchMode.Solo };

        [Test]
        public void SoloSkipsTheThreeReadyGate()
        {
            Assert.IsTrue(Session.CanStart(Solo()));
            Assert.IsTrue(Session.CanStart(Multi(0, 1, 2)));
            Assert.IsFalse(Session.CanStart(Multi(0, 1, 2, 2)), "multiplayer still needs three players");
            var notReady = Multi(0, 1, 2);
            notReady.ready[1] = false;
            Assert.IsFalse(Session.CanStart(notReady));
        }

        [Test]
        public void MultiplayerClientDrivesOnlyItsOwnSeat()
        {
            var L = Multi(0, 5, 9);
            Assert.AreEqual(Role.Navigator, Session.SeatFor(L, 5, (int)Role.Pilot), "claimed role is ignored");
            Assert.AreEqual(Role.Cabin, Session.SeatFor(L, 9, (int)Role.Pilot));
            Assert.AreEqual(Role.None, Session.SeatFor(L, 7, (int)Role.Pilot), "spectator drives nothing");
        }

        [Test]
        public void SoloPlayerOwnsAllThreeSeatsAndPicksOne()
        {
            var L = Solo();
            foreach (var r in new[] { Role.Pilot, Role.Navigator, Role.Cabin })
                Assert.AreEqual(r, Session.SeatFor(L, 0, (int)r));
            Assert.AreEqual(Role.None, Session.SeatFor(L, 0, 7), "bad seat index");
            Assert.AreEqual(Role.None, Session.SeatFor(L, 3, (int)Role.Pilot), "nobody else can drive a solo flight");
        }

        [Test]
        public void SoloPilotSnapshotStillHasNoNumbers()
        {
            var m = new Match(UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>());
            m.Step(0.02f);
            Assert.IsFalse(m.SnapshotFor(Role.Pilot).nav, "asymmetry holds in solo: server sends per driven seat");
            Assert.IsTrue(m.SnapshotFor(Role.Navigator).nav);
        }
    }
}
