using JazzRelay.Enums;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;

namespace JazzRelay.Plugins
{
    internal class PacketSpam : IPlugin
    {
        public enum Classes : ushort
        {
            Rogue = 768,
            Archer = 775,
            Wizard = 782,
            Priest = 784,
            Samurai = 785,
            Bard = 796,
            Warrior = 797,
            Knight = 798,
            Paladin = 799,
            Assassin = 800,
            Necromancer = 801,
            Huntress = 802,
            Mystic = 803,
            Trickster = 804,
            Sorcerer = 805,
            Ninja = 806,
            Summoner = 817,
            Kensei = 818,
        }

        Dictionary<int, Entity> entites = new();

        int offset = 0;

        public void HookHello(Client client, Hello packet)
        {
            //packet.GameId = -1;
            entites = new();
        }

        public void HookNewTick(Client client, NewTick packet)
        {
            foreach (var status in packet.statuses)
            {
                entites[status.ObjectId].Stats.Position = status.Position;
            }
        }

        public void HookPong(Client client, Pong packet)
        {
            packet.Time += offset;
        }

        public void HookMove(Client client, Move packet) => packet.Time += offset;

        public async void HookPlayerText(Client client, PlayerText packet)
        {
            offset += 5000;
            await client.SendToClient(new Goto
            {
                ObjectId = client.ObjectId,
                Position = new(client.Position.X, client.Position.Y-10),
            });
            packet.Send = false;
        }

        public void HookGotoAck(Client client, GotoAck packet) => packet.Send = false;

        public void HookUpdate(Client client, Update packet)
        {
            foreach (var entity in packet.Entities)
            {
                entites[entity.Stats.ObjectId] = entity;
            }

            foreach (var drop in packet.Drops)
            {
                entites.Remove(drop);
            }
        }

        public async Task HookUseItem(Client client, UseItem packet)
        {
            //Entity? closest = null;
            //foreach (var entity in entites.Values.ToList())
            //{
            //    if (closest == null || entity.Stats.Position.DistanceTo(packet.ItemUsePosition) < closest.Stats.Position.DistanceTo(packet.ItemUsePosition))
            //    {
            //        closest = entity;
            //    }
            //}
            //if (closest == null) return;
            // packet.ItemUsePosition = closest!.Stats.Position;
            var tasks = new List<Task>();
            for (int i = 0; i < 350000; i++)
            {
                var useItem = new UseItem
                {
                    ItemUsePosition = packet.ItemUsePosition,
                    SlotObjectData = packet.SlotObjectData,
                    Time = packet.Time + i * 500,
                    useType = packet.useType
                };
                tasks.Add(client.SendToServer(useItem));
            }
            await Task.WhenAll(tasks);
        }
    }
}
