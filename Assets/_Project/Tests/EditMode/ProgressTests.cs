using NUnit.Framework;
using UnityEngine;

namespace BlindRunway.Tests
{
    public class ProgressTests
    {
        static readonly string[] Keys = { "br.unlocked", "br.cleared", "br.stars.0", "br.stars.1" };
        const int Unset = int.MinValue;

        // Runs against the real PlayerPrefs in the editor, so put the player's progress back afterwards.
        static int[] Backup()
        {
            var old = new int[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                old[i] = PlayerPrefs.GetInt(Keys[i], Unset);
                PlayerPrefs.DeleteKey(Keys[i]);
            }
            return old;
        }

        static void Restore(int[] old)
        {
            for (int i = 0; i < Keys.Length; i++)
                if (old[i] == Unset) PlayerPrefs.DeleteKey(Keys[i]);
                else PlayerPrefs.SetInt(Keys[i], old[i]);
        }

        [Test]
        public void WinsUnlockTheNextLevelInOrder()
        {
            var old = Backup();
            try
            {
                Assert.AreEqual(0, Progress.Unlocked);
                Progress.Record(new ResultMsg { level = 0, win = false });
                Assert.AreEqual(0, Progress.Unlocked);
                Progress.Record(new ResultMsg { level = 0, win = true, stars = 2 });
                Assert.AreEqual(1, Progress.Unlocked);
                Assert.IsTrue(Progress.Cleared(0));
                Assert.IsFalse(Progress.Cleared(1));
                Assert.AreEqual(2, Progress.Stars(0));
                Progress.Record(new ResultMsg { level = 0, win = true, stars = 1 });
                Assert.AreEqual(2, Progress.Stars(0)); // best stars kept
            }
            finally
            {
                Restore(old);
            }
        }
    }
}
