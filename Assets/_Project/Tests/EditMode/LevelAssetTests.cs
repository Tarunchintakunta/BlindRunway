using NUnit.Framework;

namespace BlindRunway.Tests
{
    public class LevelAssetTests
    {
        // Proves the hand-written L01-L05 YAML imports with the fields the design docs promise.
        [Test]
        public void FiveLevelsLoadInOrder()
        {
            var all = Levels.All;
            Assert.AreEqual(5, all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                Assert.AreEqual(i + 1, all[i].number);
                Assert.IsFalse(string.IsNullOrEmpty(all[i].title), $"L0{i + 1} title");
                Assert.Greater(all[i].timeLimit, 0);
                Assert.Less(all[i].gateStart, all[i].gateEnd);
                Assert.Less(all[i].gateEnd, all[i].runwayLength + 0.01f);
            }
        }
    }
}
