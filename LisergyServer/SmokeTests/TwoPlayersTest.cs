using ClientSDK.SDKEvents;
using Game.Events.ServerEvents;
using Game.Network.ClientPackets;
using NUnit.Framework;
using ServerTests.Integration.Stubs;
using System.Linq;
using System.Threading.Tasks;

namespace ServerTests.Integration
{
    /*
    public class TwoPlayerTest
    {
        StandaloneServer _server;
        TestGameClient _p1;
        TestGameClient _p2;

        [OneTimeSetUp]
        public void Setup()
        {
            _server = new StandaloneServer().Start();
            _p1 = new TestGameClient();
            _p1.PrepareSDK();
            _p2 = new TestGameClient();
            _p2.PrepareSDK();
        }

        [OneTimeTearDown]
        public void TearDown() => _server?.Dispose();

        [Test]
        [NonParallelizable]

        public async Task SmokeTest2p()
        {
            _p1.Modules.Account.SendAuthenticationPacket("abc", "def");
            var result = await _p1.WaitFor<LoginResultPacket>();
            Assert.AreEqual(true, result.Success);

            _p2.Modules.Account.SendAuthenticationPacket("abc2", "def2");
            var resultP2 = await _p2.WaitFor<LoginResultPacket>();
            Assert.AreEqual(true, resultP2.Success);

            var p1Received = await _p1.WaitFor<TileUpdatePacket>();
            var p2Received = await _p2.WaitFor<TileUpdatePacket>();

            Assert.NotNull(p1Received);
            Assert.NotNull(p2Received);
            Assert.AreNotEqual(p1Received.Data, p2Received.Data);

            _p1.EventsInSdk.Clear();
            _p2.EventsInSdk.Clear();

            // TESTING CHAT
            _p1.Modules.Chat.SendMessage("P1 MESSAGE");

            var p1Seen = await _p1.WaitFor<ChatPacket>();
            var p2Seen = await _p2.WaitFor<ChatPacket>();
            
            Assert.IsTrue(_p1.EventsInSdk.Any(e => e is ChatUpdateEvent chat && chat.NewPacket.Message == "P1 MESSAGE"));
            Assert.IsTrue(_p2.EventsInSdk.Any(e => e is ChatUpdateEvent chat && chat.NewPacket.Message == "P1 MESSAGE"));
            Assert.NotNull(p1Seen);
            Assert.NotNull(p2Seen);
        }

        [Test]
        [NonParallelizable]

        public async Task SmokeTestP1Disconnects()
        {
            _p1.Modules.Account.SendAuthenticationPacket("abc", "def");
            var result = await _p1.WaitFor<LoginResultPacket>();
            Assert.AreEqual(true, result.Success);
            _p1.Network.Disconnect();
            _p1.ReceivedPackets.Clear();

            _p2.Modules.Account.SendAuthenticationPacket("abc2", "def2");
            var resultP2 = await _p2.WaitFor<LoginResultPacket>();
            Assert.AreEqual(true, resultP2.Success);

            var p1Received = await _p1.WaitFor<TileUpdatePacket>();
            var p2Received = await _p2.WaitFor<TileUpdatePacket>();

            Assert.Null(p1Received);
            Assert.NotNull(p2Received);
        }
    }
    */
}
