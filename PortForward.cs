using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PortForward
{
    class PortForward
    {
        string localAddress = "127.0.0.1";
        int localPort = 13000;

        string remoteAddress = "127.0.0.1";
        int remotePort = 13001;

        public void Start(string[] args)
        {
            if (args.Length > 1)
            {
                localAddress = args[1];
            }
            if (args.Length > 2)
            {
                localPort = int.Parse(args[2]);
            }
            if (args.Length > 3)
            {
                remoteAddress = args[3];
            }
            if (args.Length > 4)
            {
                remotePort = int.Parse(args[4]);
            }

            TcpListener server = null;
            Console.WriteLine($"Started port forwarding from [{localAddress}:{localPort}] to [{remoteAddress}:{remotePort}]... ");

            try
            {
                IPAddress localAddr = IPAddress.Parse(localAddress);

                server = new TcpListener(localAddr, localPort);
                server.Start();

                // test remote server availability
                try
                {
                    var remoteClient = new TcpClient(remoteAddress, remotePort);
                    remoteClient.Close();
                }
                catch (Exception ex)
                {

                }

                while (true)
                {
                    //Console.Write("Waiting for a connection... ");

                    TcpClient client = server.AcceptTcpClient();
                    var connection = StartClientConnection(client);
                    connection.Start();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                server.Stop();
            }
        }

        Task StartReader(string name, NetworkStream stream, ConcurrentQueue<Tuple<byte[], int>> readBuffer)
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
                        //var data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        //Console.WriteLine("Received: {0}", data);

                        byte[] toEnqueue = new byte[i];

                        while (readBuffer.Count > 10)
                        {
                            Task.Delay(10);
                        }

                        readBuffer.Enqueue(new Tuple<byte[], int>(bytes, i));
                        bytes = new byte[4096];
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

        Task StartWriter(string name, TcpClient underlyingClient, TcpClient otherClient, NetworkStream stream, ConcurrentQueue<Tuple<byte[], int>> writeBuffer)
        {
            var task = new Task(() =>
            {
                var taskName = name;

                try
                {
                    while (true)
                    {
                        if (writeBuffer.Count == 0)
                        {
                            Task.Delay(20);
                        }
                        else
                        {
                            Tuple<byte[], int> bytes;

                            while (writeBuffer.Count > 0)
                            {
                                if (writeBuffer.TryDequeue(out bytes))
                                {
                                    stream.Write(bytes.Item1, 0, bytes.Item2);

                                    //var data = System.Text.Encoding.ASCII.GetString(bytes.Item1, 0, bytes.Item2);
                                    //Console.WriteLine("Sent: {0}", data);
                                }
                                else
                                {
                                    Task.Delay(10);
                                }
                            }
                        }

                        if (!underlyingClient.Connected)
                        {
                            otherClient.Close();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            });

            task.Start();
            return task;
        }

        Task StartClientConnection(TcpClient localClient)
        {
            return new Task(() =>
            {
                var remoteEndPoint = localClient.Client.RemoteEndPoint;
                Console.WriteLine($"Connected client: [{remoteEndPoint}]");

                TcpClient remoteClient = null;

                try
                {
                    remoteClient = new TcpClient(remoteAddress, remotePort);

                    NetworkStream localStream = localClient.GetStream();
                    NetworkStream remoteStream = remoteClient.GetStream();

                    var localBuffer = new ConcurrentQueue<Tuple<byte[], int>>();
                    var remoteBuffer = new ConcurrentQueue<Tuple<byte[], int>>();

                    using (localStream)
                    using (remoteStream)
                    {
                        StartReader("RemoteReader", remoteStream, remoteBuffer);
                        StartWriter("RemoteWriter", remoteClient, localClient, remoteStream, localBuffer);

                        Task.WaitAll(
                            StartReader("LocalReader", localStream, localBuffer),
                            StartWriter("LocalWriter", localClient, remoteClient, localStream, remoteBuffer)
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in connection to: [{remoteAddress}:{remotePort}]: " + ex.ToString());
                }

                try
                {
                    localClient.Close();
                }
                catch (Exception ex)
                {

                }

                try
                {
                    remoteClient.Close();
                }
                catch (Exception ex)
                {

                }

                Console.WriteLine($"Disconnected client: [{remoteEndPoint}]");
            });
        }
    }
}
