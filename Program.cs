using System;

namespace BackroomsIK_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Servidor de BackroomsIK iniciado....");
            Console.WriteLine("Esperando conexiones...");

            // Bucle infinito para que el servidor de Ubuntu no se apague
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
