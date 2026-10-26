using NUnit.Framework;

namespace BlindRunway.Tests
{
    public class JoinCodeTests
    {
        [Test]
        public void CodeRoundTripsLanIp()
        {
            Assert.AreEqual("C0A8-0105", Net.ToCode("192.168.1.5"));
            Assert.IsTrue(Net.TryParseCode("c0a8-0105", out var ip));
            Assert.AreEqual("192.168.1.5", ip);
        }

        [Test]
        public void AcceptsRawIpAndBlank()
        {
            Assert.IsTrue(Net.TryParseCode(" 10.0.0.7 ", out var ip));
            Assert.AreEqual("10.0.0.7", ip);
            Assert.IsTrue(Net.TryParseCode("", out ip));
            Assert.AreEqual("127.0.0.1", ip);
        }

        [Test]
        public void RejectsGarbage()
        {
            Assert.IsFalse(Net.TryParseCode("ZZZZ-0000", out _));
            Assert.IsFalse(Net.TryParseCode("C0A8", out _));
            Assert.IsFalse(Net.TryParseCode("999.1.1.1", out _));
        }
    }
}
