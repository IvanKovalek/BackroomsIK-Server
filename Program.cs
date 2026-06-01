using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace BackroomsIK_Server
{
    public class Player
    {
        public int Id { get; set; }
        // Solución al Warning: Le asignamos un texto vacío por defecto
        public string Name { get; set; } = string.Empty; 
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public int State { get; set; } 
    }

    public class Monster
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public int State { get; set; } 
    }

    class Program
    {
        static Dictionary<int, Player> players = new Dictionary<int, Player>();
        static Dictionary<int, Monster> monsters = new Dictionary<int, Monster>();

        static void Main(string[] args)
        {
            monsters.Add(1, new Monster { Id = 1, X = 10f, Y = 0f, Z = 10f, RotationY = 0f, Health = 100, State = 0 });
            monsters.Add(2, new Monster { Id = 2, X = -5f, Y = 0f, Z = 20f, RotationY = 180f, Health = 100, State = 0 });

            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);

            listener.ConnectionRequestEvent += request =>
            {
                if(server.ConnectedPeersCount < 10)
                    request.AcceptIfKey("BackroomsIK_Secret");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"[Servidor] Jugador conectado: {peer.Id}");
                players.Add(peer.Id, new Player { Id = peer.Id, Name = "Jugador_" + peer.Id, Health = 100, State = 0 });
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine($"[Servidor] Jugador desconectado: {peer.Id}");
                players.Remove(peer.Id);
            };

            // Solución al Error CS1593: Agregamos el parámetro 'channel' (el tercer parámetro de la lista)
            listener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) =>
            {
                byte packetType = dataReader.GetByte(); 

                if(packetType == 1 && players.ContainsKey(fromPeer.Id))
                {
                    players[fromPeer.Id].X = dataReader.GetFloat();
                    players[fromPeer.Id].Y = dataReader.GetFloat();
                    players[fromPeer.Id].Z = dataReader.GetFloat();
                    players[fromPeer.Id].RotationY = dataReader.GetFloat();
                    players[fromPeer.Id].State = dataReader.GetInt();
                }
                
                dataReader.Recycle();
            };

            server.Start(9050);
            Console.WriteLine("Servidor BackroomsIK Autoritativo iniciado. Escuchando en puerto 9050...");

            NetDataWriter writer = new NetDataWriter();

            while (true)
            {
                server.PollEvents();

                foreach (var monster in monsters.Values)
                {
                    if (monster.State == 0) 
                    {
                        monster.Z += 0.02f; 
                    }
                }

                if (server.ConnectedPeersCount > 0)
                {
                    writer.Reset();
                    writer.Put((byte)2); 

                    writer.Put(players.Count);
                    foreach (var p in players.Values)
                    {
                        writer.Put(p.Id);
                        writer.Put(p.X);
                        writer.Put(p.Y);
                        writer.Put(p.Z);
                        writer.Put(p.RotationY);
                        writer.Put(p.Health);
                        writer.Put(p.State);
                    }

                    writer.Put(monsters.Count);
                    foreach (var m in monsters.Values)
                    {
                        writer.Put(m.Id);
                        writer.Put(m.X);
                        writer.Put(m.Y);
                        writer.Put(m.Z);
                        writer.Put(m.RotationY);
                        writer.Put(m.Health);
                        writer.Put(m.State);
                    }

                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }

                Thread.Sleep(15); 
            }
        }
    }
}
