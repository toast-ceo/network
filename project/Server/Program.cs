﻿using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace AServer
{
    public class Server
    {
        private readonly static int BufferSize = 4096;

        public static void Main()
        {
            try
            {
                new Server().Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        private Dictionary<string, Socket> connectedClients = new();
        private Dictionary<string, Socket> connectedUsers = new();
        private Dictionary<string, Socket> connectedManagers = new();

        public Dictionary<string, Socket> ConnectedClients
        {
            get => connectedClients;
            set => connectedClients = value;
        }
        public Dictionary<string, Socket> ConnectedUsers
        {
            get => connectedUsers;
            set => connectedUsers = value;
        }
        public Dictionary<string, Socket> ConnectedManagers
        {
            get => connectedManagers;
            set => connectedManagers = value;
        }

        private Socket ServerSocket;

        private readonly IPEndPoint EndPoint = new(IPAddress.Parse("127.0.0.1"), 5001);

        int clientNum;
        Server()
        {
            ServerSocket = new(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            clientNum = 0;
        }

        void Init()
        {
            ServerSocket.Bind(EndPoint);
            ServerSocket.Listen(100);
            Console.WriteLine("Waiting connection request.");

            Accept();

        }


        void Accept()
        {
            do
            {
                Socket client = ServerSocket.Accept();


                Console.WriteLine($"Client accepted: {client.RemoteEndPoint}.");

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                client.ReceiveAsync(args);

            } while (true);
        }

        void Disconnected(Socket client)
        {
            Console.WriteLine($"Client disconnected: {client.RemoteEndPoint}.");
            foreach (KeyValuePair<string, Socket> clients in connectedClients)
            {
                if (clients.Value == client)
                {
                    ConnectedClients.Remove(clients.Key);
                    clientNum--;
                }
            }
            foreach (KeyValuePair<string, Socket> clients in connectedManagers)
            {
                if (clients.Value == client)
                {
                    ConnectedManagers.Remove(clients.Key);
                }
            }
            foreach (KeyValuePair<string, Socket> clients in connectedUsers)
            {
                if (clients.Value == client)
                {
                    ConnectedUsers.Remove(clients.Key);
                }
            }
            client.Disconnect(false);
            client.Close();
        }

        void Received(object? sender, SocketAsyncEventArgs e)
        {
            Socket client = (Socket)sender!;
            byte[] data = new byte[BufferSize];
          
            try
            {
                int n = client.Receive(data);
                if (n > 0)
                {
                    MessageProc(client, data);

                    SocketAsyncEventArgs argsR = new SocketAsyncEventArgs();
                    argsR.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                    client.ReceiveAsync(argsR);
                }
                else { throw new Exception(); }
            }
            catch (Exception)
            {
                Disconnected(client);
            }
        }

        void MessageProc(Socket s, byte[] bytes)
        {
            string m = Encoding.Unicode.GetString(bytes);
            var clientBody  = JsonConvert.DeserializeObject<ClientService>(m);
            string msg;

            string[] tokens = m.Split(':');
            string fromID;
            string toID;
            string code = tokens[0];

            
            if (clientBody.commend == "ID")
            {
                clientNum++;
                fromID = clientBody.id;
                Console.WriteLine("[접속{0}]ID:{1},{2}",
                    clientNum, fromID, s.RemoteEndPoint);
                //
                connectedClients.Add(fromID, s);
                if(clientBody.roll == "manager")
                {
                    connectedManagers.Add(fromID, s);
                }
                else if(clientBody.roll == "user")
                {
                    connectedUsers.Add(fromID, s);
                }
                s.Send(Encoding.Unicode.GetBytes("연결이 성공했습니다!"));
                msg = $"{clientBody.id}연결 접속됐습니다!";

                Broadcast(s, msg);
            }

            else if (clientBody.commend == "BR")
            {
                Console.WriteLine(m);
                /*//
                fromID = tokens[1].Trim();
                string msg = tokens[2];
                Console.WriteLine("[전체]{0}:{1}", fromID, msg);
                //
                Broadcast(s, m);
                s.Send(Encoding.Unicode.GetBytes("BR_Success:"));*/
            }

            else if (code.Equals("TO"))
            {
                fromID = tokens[1].Trim();
                toID = tokens[2].Trim();
                msg = tokens[3];
                string rMsg = "[From:" + fromID + "][TO:" + toID + "]" + msg;
                Console.WriteLine(rMsg);

                //
                SendTo(toID, m);
                s.Send(Encoding.Unicode.GetBytes("To_Success:"));
            }
            else if (code.Equals("File"))
            {
                ReceiveFile(s, m);
            }
            else
            {
                Broadcast(s, m);
            }
        }
        void ReceiveFile(Socket s, string m)
        {
            string output_path = "FileDown";
            if (!Directory.Exists(output_path))
            {
                Directory.CreateDirectory(output_path); 
            }
            string[] tokens = m.Split(':');
            string fileName = tokens[1].Trim();
            long fileLength = Convert.ToInt64(tokens[2].Trim());
            string FileDest = output_path +fileName;

            long flen = 0;
            FileStream fs = new FileStream(FileDest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            while(flen < fileLength)
            {
                byte[] fdata = new byte[4096];
                int r = s.Receive(fdata, 0, 4096, SocketFlags.None);
                fs.Write(fdata, 0, r);
                flen += r;
            }
            fs.Close(); 
        }
        void SendTo(string id, string msg)
        {
            Socket socket;
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            if (connectedClients.ContainsKey(id))
            {
                //
                connectedClients.TryGetValue(id, out socket!);
                try { socket.Send(bytes); } catch { }
            }
        }
        
        void Broadcast(Socket s, string msg) // 5-2ㅡ모든 클라이언트에게 Send
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            //
            foreach (KeyValuePair<string, Socket> client in connectedClients.ToArray())
            {
                try
                {
                    //5-2 send
                    //
                    if (s != client.Value)
                        client.Value.Send(bytes);
                }
                catch (Exception)
                {
                    Disconnected(client.Value);
                }
            }
        }

    }
    public class ClientService
    {
        public string id { get; set; }
        public string roll { get; set; }
        public string commend { get; set; }
        public string message { get; set; }

    }

}