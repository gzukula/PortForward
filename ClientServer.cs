using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortForward
{
    class ClientServer
    {
        string address = "127.0.0.1";
        int port = 13001;

        public void Start(string[] args)
        {
            if (args.Length > 1)
            {
                address = args[1];
            }
            if (args.Length > 2)
            {
                port = int.Parse(args[2]);
            }

            Console.CancelKeyPress += delegate
            {
                Environment.Exit(0);
            };

            try
            {
                if (args[0] == "s")
                {
                    IPAddress localAddr = IPAddress.Parse(address);

                    var server = new TcpListener(localAddr, port);
                    server.Start();
                    Console.WriteLine($"Started TCP server at [{address}:{port}]... ");

                    while (true)
                    {
                        //Console.Write("Waiting for a connection... ");
                        TcpClient clientConnection = server.AcceptTcpClient();
                        StartConnection(clientConnection);
                    }
                }
                else if (args[0] == "c")
                {
                    var serverConnection = new TcpClient(address, port);
                    StartConnection(serverConnection).Wait();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        Task StartConnection(TcpClient connection)
        {
            var task = new Task(() =>
            {
                var remoteEndPoint = connection.Client.RemoteEndPoint;
                Console.WriteLine($"Connected to: [{remoteEndPoint}]");

                try
                {
                    NetworkStream localStream = connection.GetStream();

                    using (localStream)
                    {
                        StartConsoleWriter("LocalWriter", connection, localStream);
                        Task.WaitAll(
                            StartReader("LocalReader", localStream)
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in connection to: [{remoteEndPoint}]: " + ex.ToString());
                }

                try
                {
                    connection.Close();
                }
                catch (Exception ex)
                {

                }

                Console.WriteLine($"Disconnected client: [{remoteEndPoint}]");
            });

            task.Start();
            return task;
        }

        Task StartReader(string name, NetworkStream stream)
        {
            var task = new Task(() =>
            {
                var taskName = name;

                byte[] bytes = new byte[4096];
                int i;

                try
                {
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var data = Encoding.ASCII.GetString(bytes, 0, i);
                        Console.Write(data);
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                }
            });

            task.Start();
            return task;
        }

        Task StartConsoleWriter(string name, TcpClient connection, NetworkStream stream)
        {
            var task = new Task(() =>
            {
                var taskName = name;

                try
                {
                    while (true)
                    {
                        var line = Console.ReadLine() + "\n";
                        var bytes = Encoding.ASCII.GetBytes(line);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception ex)
                {

                }
            });

            task.Start();
            return task;
        }
    }
}
