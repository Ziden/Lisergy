using Game;
using Game.Engine.DataTypes;
using Game.Network.ClientPackets;
using Game.Network.ServerPackets;
using Game.Systems.Battle;
using Game.Systems.Battler;
using Game.Systems.Dungeon;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.World;
using NUnit.Framework;
using ServerTests;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    public class TestBattleListener
    {
        private TestGame _game;
        private List<Location> _path;
        private TestServerPlayer _player;
        private PartyEntity _party;
        private DungeonEntity _dungeon;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            _player = _game.GetTestPlayer();
            _path = new List<Location>();
            _party = _player.GetParty(0);
            _dungeon = (DungeonEntity)_game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            _dungeon.BuildFromSpec(_game.Specs.Dungeons[0]);
            _game.Systems.Map.GetLogic(_dungeon).SetPosition(_game.World.Map.GetTile(8, 8));
        }

        private TurnBattle SetupBattle()
        {
            var party = _player.GetParty(0);
            var partyTile = _game.Systems.Map.GetLogic(party).GetPosition();
            var dungeonTile = partyTile.GetNeighbor(Direction.EAST);
            _game.Systems.Map.GetLogic(_dungeon).SetPosition(dungeonTile);
            var component = party.Get<BattleGroupComponent>();
            var unit = component.Units[0];
            unit.Atk = 255;
            component.Units[0] = unit;
            party.Save(component);
            _player.SendMoveRequest(party, dungeonTile, CourseIntent.OffensiveTarget);
            var course = party.Course;
            Assert.AreEqual(0, _player.Data.BattleHeaders.Count());
            course.Tick();
            course.Tick();
            return _game.BattleService.GetRunningBattle(party.Get<BattleGroupComponent>().BattleID);
        }

        [Test]
        public void TestBattleTrack()
        {
            var battle = SetupBattle();
            _game.BattleService.BattleTasks[battle.ID].Tick();

            var attackerPlayer = _game.World.Players.GetPlayer(battle.Attacker.OwnerID);
            _game.BattleService.AllBattles.TryGetValue(battle.ID, out var log);


            Assert.That(attackerPlayer.Data.BattleHeaders.Any(b => b.BattleID == battle.ID));
            Assert.That(log.Turns.Count() == battle.Record.Turns.Count());
        }

        [Test]
        public void TestRequestinBattleLog()
        {
            var battle = SetupBattle();
            _game.BattleService.BattleTasks[battle.ID].Tick();

            _game.HandleClientEvent(_player, new BattleLogRequestPacket() { BattleId = battle.ID });

            var result = _player.ReceivedPacketsOfType<BattleLogPacket>().FirstOrDefault();

            Assert.NotNull(result);
            Assert.AreEqual(result.Turns.Count(), battle.Record.Turns.Count());
        }
    }
}