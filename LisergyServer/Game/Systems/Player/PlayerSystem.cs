using Game.Engine.ECS;
using Game.Network.ClientPackets;
using Game.Network.ServerPackets;
using Game.Systems.ActionPoints;
using Game.Systems.FogOfWar;

namespace Game.Systems.Player
{
    public class PlayerSystem : LogicSystem<PlayerComponent, PlayerLogic>
    {
        public PlayerSystem(LisergyGame game) : base(game)
        {
        }

        public override void RegisterListeners()
        {
            Game.Network.On<BattleResultPacket>(OnBattleResultPacket);
            Game.Network.On<TakeTurnPacket>(TakeTurn);
            EntityEvents.On<TileVisibilityChangedForPlayerEvent>(OnTileVisibilityChanged);

        }
        
        private static void TakeTurn(TakeTurnPacket p)
        {
            p.Sender.EntityLogic.Player.TakeTurn();
        }

        /// <summary>
        /// Received from battle server
        /// </summary>
        private void OnBattleResultPacket(BattleResultPacket packet)
        {
            var atkPlayer = Game.Players[packet.Header.Attacker.OwnerID];
            var defPlayer = Game.Players[packet.Header.Defender.OwnerID];

            if (atkPlayer != null) GetLogic(atkPlayer).RecordBattleHeader(packet.Header);
            if (defPlayer != null) GetLogic(defPlayer).RecordBattleHeader(packet.Header);
        }

        private void OnTileVisibilityChanged(IEntity player, TileVisibilityChangedForPlayerEvent ev)
        {
            var tileObj = ev.Tile;
            var owner = Players.GetPlayer(ev.Explorer.OwnerID);
            if (ev.Visible)
            {
                //owner.VisibilityReferences.OnceExplored.Add(tileObj.Position);
                owner.VisibilityReferences.VisibleTiles.Add(tileObj.Position);
            }
            else
            {
                owner.VisibilityReferences.VisibleTiles.Remove(tileObj.Position);
            }
        }
    }
}
