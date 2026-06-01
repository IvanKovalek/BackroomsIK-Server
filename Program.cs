using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace BackroomsIK_Server
{
    // 1. Estructuras de datos para guardar el estado del mundo
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public int State { get; set; } // 0=Quieto, 1=Caminando, 2=Corriendo, etc.
    }

    public class Monster
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public int State { get; set; } // 0=Patrullando, 1=Persiguiendo, 2=Atacando
    }

    class Program
    {
        // Diccionarios para mantener en memoria a los jugadores y monstruos activos
        static Dictionary<int, Player> players = new Dictionary<int, Player>();
        static Dictionary<int, Monster> monsters = new Dictionary<int, Monster>();

        static void Main(string[] args)
        {
            // Creamos un par de monstruos de prueba (El servidor tiene el control de ellos)
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
                // Registramos al jugador nuevo en el mundo con datos por defecto
                players.Add(peer.Id, new Player { Id = peer.Id, Name = "Jugador_" + peer.Id, Health = 100, State = 0 });
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine($"[Servidor] Jugador desconectado: {peer.Id}");
                // Lo borramos del mundo
                players.Remove(peer.Id);
            };

            // Cuando recibimos paquetes desde Unity (El cliente manda su input/posición)
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                byte packetType = dataReader.GetByte(); // Leemos el primer byte para saber qué acción es

                // Tipo 1: El cliente nos manda su nueva posición
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

            // Bucle principal del servidor (Tickrate)
            while (true)
            {
                server.PollEvents();

                // --- 1. LÓGICA DE MONSTRUOS (IA DE SERVIDOR) ---
                foreach (var monster in monsters.Values)
                {
                    if (monster.State == 0) // Si están patrullando
                    {
                        // Ejemplo: movemos al monstruo lentamente en el eje Z
                        monster.Z += 0.02f; 
                    }
                }

                // --- 2. BROADCAST (Transmitir el mundo a los jugadores) ---
                if (server.ConnectedPeersCount > 0)
                {
                    writer.Reset();
                    writer.Put((byte)2); // Tipo 2: Paquete de actualización global del mundo

                    // Escribimos a los jugadores
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

                    // Escribimos a los monstruos
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

                    // Enviamos el paquete gigante a todos (Usamos Unreliable para posiciones por su velocidad)
                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }

                Thread.Sleep(15); // ~60 Ticks por segundo (optimizado para CPU)
            }
        }
    }
}
