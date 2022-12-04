using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Collections.Generic;

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

        // 받는 사람 , 보내는 사람 , 챗 내용 
        public DualKeyDictionary<string, string, List<string>> RecodeUserChat = new DualKeyDictionary<string, string, List<string>>();
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
            foreach (KeyValuePair<string, Socket> clients in connectedClients)
            {
                if (clients.Value == client)
                {
                    ConnectedClients.Remove(clients.Key);
                    clientNum--;
                }
            }
            client.Disconnect(false);
            client.Close();
        }
        string bMsg;

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

            string fromID;
            string toID;

            if (clientBody.commend == "ID")
            {
                clientNum++;
                fromID = clientBody.id;
                Console.WriteLine("[접속{0}]ID:{1},{2}",
                    clientNum, fromID, s.RemoteEndPoint);
                //
                try
                {
                    connectedClients.Add(fromID, s);
                    if (clientBody.roll == "manager")
                    {
                        connectedManagers.Add(fromID, s);
                    }
                    else if (clientBody.roll == "user")
                    {
                        connectedUsers.Add(fromID, s);
                    }
                    s.Send(Encoding.Unicode.GetBytes("연결이 성공했습니다!"));
                    msg = $"ID!{clientBody.id}!연결 접속됐습니다!";

                    Broadcast(s, msg);
                }
                catch (ArgumentException)
                {
                    s.Send(Encoding.Unicode.GetBytes("IDC! 이미 존재하는 아이디입니다. 다시 입력하세요."));
                }


            }
            else if (clientBody.commend == "BR")
            {
                if (clientBody.roll == "manager")
                {
                    msg = clientBody.message;
                    Console.WriteLine("[전체]: {0}", msg);
                    UserBroadcast(s, msg);
                    s.Send(Encoding.Unicode.GetBytes("BR_완료 메세지"));
                }

                else if (clientBody.roll == "user")
                {
                    msg = clientBody.commend + "!" + clientBody.id + "!" + clientBody.message;
                    Console.WriteLine("[전체]: {0}", msg);
                    ManagerBroadcast(s, msg);
                    s.Send(Encoding.Unicode.GetBytes("매니저에게 BR를 요청했습니다."));
                }

            }
            else if (clientBody.commend == "AC")
            {
                msg = clientBody.message;
                Console.WriteLine("[전체]: {0}", msg);
                UserBroadcast(s, msg);
                s.Send(Encoding.Unicode.GetBytes("AC 요청 완료"));
            }
            else if (clientBody.commend == "TO")
            {
                if (clientBody.roll == "manager")

                {
                    fromID = clientBody.id;
                    toID = clientBody.Toid;
                    msg = clientBody.message;

                    string rMsg = "[From:" + fromID + "]" + msg;
                    Console.WriteLine("[From:" + fromID + "] [To:" + toID + "]" + msg);
                    SendTo(s, clientBody.roll, toID, rMsg);
                    s.Send(Encoding.Unicode.GetBytes("To 송신 완료"));
                }

                else if (clientBody.roll == "user")

                {
                    fromID = clientBody.id;
                    toID = clientBody.Toid;
                    msg = clientBody.message;

                  
                    string rMsg = "[From:" + fromID + "]" + msg;
                    bMsg = $"{clientBody.commend}!{clientBody.roll}!{toID}!{fromID}!{msg}";
                    Console.WriteLine("[From:" + fromID + "] [To:" + toID + "]" + msg);

                    SendTo(s, clientBody.roll, toID, rMsg);
                    ManagerBroadcast(s, bMsg);
                    s.Send(Encoding.Unicode.GetBytes("To 송신 완료"));

                    try
                    {
                        List<string> tempMsg = new List<string>();
                        if (RecodeUserChat.ContainsKey(fromID, toID) == false || RecodeUserChat[fromID, toID].Count <= 0)
                        {
                            tempMsg.Add(msg);
                            RecodeUserChat.Add(fromID, toID, tempMsg);
                        }
                        else
                        {
                            tempMsg = RecodeUserChat[fromID, toID];
                            tempMsg.Add(msg);
                            RecodeUserChat.Add(fromID, toID, tempMsg);

                        }
                        
                    }
                    catch
                    {
                        Console.WriteLine("저장 실패");
                    }


                }
            }
            else if (clientBody.commend == "INFO")
            {
                msg = String.Join(", ", connectedUsers.Keys.ToArray());
                s.Send(Encoding.Unicode.GetBytes(msg));
            }
            else if (clientBody.commend == "RC") {
                // toID -> 받은 사람의 입장
                // fromID -> 보내은 사람의 입장 
                toID = clientBody.id;
                fromID = clientBody.Toid;

                try
                {
                    string RC = fromID + "님이 보낸 메세지 입니다.\n" + RecodeUserChat[fromID, toID].Aggregate((i, j) => i + "\n" + j).ToString();
                    Console.WriteLine(RC);
                    s.Send(Encoding.Unicode.GetBytes(RC));
                }
                catch { 
                    s.Send(Encoding.Unicode.GetBytes("보낸 메세지가 없습니다!"));
                }
            }
            else if (clientBody.commend == "OUT")
            {
                string outID;
                outID = clientBody.message;
                try
                {
                    msg = outID + "님이 강퇴당하셨습니다.";
                    Console.WriteLine($"Client disconnected: {connectedUsers[outID].RemoteEndPoint}.");
                    connectedUsers[outID].Send(Encoding.Unicode.GetBytes("매니저님에 의해 강퇴당하셨습니다."));
                    connectedClients.Remove(outID);
                    connectedUsers.Remove(outID);
                    clientNum--;
                    Broadcast(s, msg);
                    s.Send(Encoding.Unicode.GetBytes("OUT 처리 완료"));
                }
                catch
                {
                    s.Send(Encoding.Unicode.GetBytes("OUT 처리 실패"));
                }
               
            }
            else
            {
                Broadcast(s, m);
            }
        }
       
        void SendTo(Socket us, string roll, string id, string msg)
        {
            Socket socket;
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            if (roll == "manager")
            {
                if (connectedClients.ContainsKey(id))
                {
                    //
                    connectedClients.TryGetValue(id, out socket!);
                    try { socket.Send(bytes);
                        us.Send(Encoding.Unicode.GetBytes("[" + id + "]님에게 전송되었습니다."));
                    }
                    catch { }
                }
            }else if (roll == "user")
                {
                if (connectedUsers.ContainsKey(id))
                {
                    //
                    connectedUsers.TryGetValue(id, out socket!);
                    try { socket.Send(bytes); 
                        us.Send(Encoding.Unicode.GetBytes("["+id+"]님에게 전송되었습니다."));
                    } catch { }
                }
                else
                {
                    us.Send(Encoding.Unicode.GetBytes("유저 정보가 없습니다."));
                }
                }

        }
        
        void Broadcast(Socket s, string msg) // 모든 클라이언트에게 Send
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
        void UserBroadcast(Socket s, string msg) // 유저 클라이언트에게 Send
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            //
            foreach (KeyValuePair<string, Socket> client in connectedUsers.ToArray())
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
        void ManagerBroadcast(Socket s, string msg) // 매니저 클라이언트에게 Send
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            //
            foreach (KeyValuePair<string, Socket> client in connectedManagers.ToArray())
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
        public string Toid { get; set; }

    }

    public class DualKeyDictionary<TKey1, TKey2, TValue> : Dictionary<TKey1, Dictionary<TKey2, TValue>>
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Property
        ////////////////////////////////////////////////////////////////////////////////////////// Public

        #region 인덱서 - this[key1, key2]

        /// <summary>
        /// 인덱서
        /// </summary>
        /// <param name="key1">첫번째 키</param>
        /// <param name="key2">두번째 키</param>
        /// <returns>값</returns>
        public TValue this[TKey1 key1, TKey2 key2]
        {
            get
            {
                if (!ContainsKey(key1) || !this[key1].ContainsKey(key2))
                {
                    throw new ArgumentOutOfRangeException();
                }

                return base[key1][key2];
            }
            set
            {
                if (!ContainsKey(key1))
                {
                    this[key1] = new Dictionary<TKey2, TValue>();
                }

                this[key1][key2] = value;
            }
        }

        #endregion
        #region 값 열거형 - Values

        /// <summary>
        /// 값 열거형
        /// </summary>
        public new IEnumerable<TValue> Values
        {
            get
            {
                return from baseDictionary in base.Values
                       from baseKey in baseDictionary.Keys
                       select baseDictionary[baseKey];
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////// Method
        ////////////////////////////////////////////////////////////////////////////////////////// Public

        #region 추가하기 - Add(key1, key2, value)

        /// <summary>
        /// 추가하기
        /// </summary>
        /// <param name="key1">첫번째 키</param>
        /// <param name="key2">두번째 키</param>
        /// <param name="value">값</param>
        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            if (!ContainsKey(key1))
            {
                this[key1] = new Dictionary<TKey2, TValue>();
            }

            this[key1][key2] = value;
        }

        #endregion
        #region 키 포함 여부 구하기 - ContainsKey(key1, key2)

        /// <summary>
        /// 키 포함 여부 구하기
        /// </summary>
        /// <param name="key1">첫번째 키</param>
        /// <param name="key2">두번쨰 키</param>
        /// <returns>키 포함 여부</returns>
        public bool ContainsKey(TKey1 key1, TKey2 key2)
        {
            return base.ContainsKey(key1) && this[key1].ContainsKey(key2);
        }

        #endregion
    }
}

