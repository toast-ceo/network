using System.Net.Sockets;
using System.Net;
using System.Text;

namespace AServer
{
    public class Server
    {
        private readonly static int BufferSize = 4096;

        public static void Main()
        {
            try
            {
                new Server().Init(); // 서버 시작
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // Dictionary 키값으로 간편하게 연결?
        private Dictionary<string, Socket> connectedClients = new();

        public Dictionary<string, Socket> ConnectedClients
        {
            get => connectedClients;
            set => connectedClients = value;
        }

        private Socket ServerSocket;

        private readonly IPEndPoint EndPoint = new(IPAddress.Parse("127.0.0.1"), 5001);

        int clientNum; // 크리티컬 섹션 공유 자원(변수)... 접근성 적용 해야함
        Server() // 1. 소켓 생성 @ 생성자
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
            ServerSocket.Bind(EndPoint);    // 2. Bind
            ServerSocket.Listen(100);       // 3. Listen
            Console.WriteLine("Waiting connection request.");

            Accept();                       // 4-1. Accept 호출

        }
 

        void Accept()
        {
            do
            {
                Socket client = ServerSocket.Accept(); // 4-2. Accept() 클라이언트와 연결

                
                Console.WriteLine($"Client accepted: {client.RemoteEndPoint}.");

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                client.ReceiveAsync(args);

            } while (true);     // 4-3. 무한 반복
        }

        void Disconnected(Socket client)    // 6. 소켓 종료 , 연결이 끊겼다! 하면 id를 찾아 삭제함
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
            client.Disconnect(false);
            client.Close();
        }

        void Received(object? sender, SocketAsyncEventArgs e)
        {
            Socket client = (Socket)sender!; // 누가 보냈는지 알 수 있게 됨
            byte[] data = new byte[BufferSize];
            try
            {
                int n = client.Receive(data); // 5-1. 데이터 받기
                if (n > 0) // 0 보다 크면 정상적인 연결
                {

                    // 다른 메세지를 처리하는 함수가 들어있음 
                    // 누가 보냈는지 받은 데이터 넘겨줌
                    MessageProc(client, data);

                    // 반복적으로 데이터를 받기 위해 다시 등록함 (대기하도록)
                    SocketAsyncEventArgs argsR = new SocketAsyncEventArgs();
                    argsR.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                    client.ReceiveAsync(argsR);
                }
                else { throw new Exception(); } // 정상적이지 않을 때
            } catch (Exception) { // 캐치문으로 들어감
                Disconnected(client); // 6. 전송 오류 나면 종료
            }
        }

        void MessageProc(Socket s, byte[] bytes)
        {
            string m = Encoding.Unicode.GetString(bytes);
            // 메세지를 상세하게 만드는 곳
            string[] tokens = m.Split(":"); // :으로 구분해놓은 곳을 더 상세하게
            string fromID;
            string toID;
            string code = tokens[0]; // ID

            if (code.Equals("ID"))
            {
                clientNum++; // 클라이언트 추가 생성시 필요
                fromID = tokens[1].Trim(); // Trim?
                Console.WriteLine("[접속{0}]ID:{1},{2}",
                    clientNum, fromID, s.RemoteEndPoint);
                // 클라이언트에 아이디 추가
                connectedClients.Add(fromID, s);
                s.Send(Encoding.Unicode.GetBytes("ID_REG_Success:")); // ID 등록 성공
                Broadcast(s, m); // 소켓값 하나 추가
            }
            else if (tokens[0].Equals("BR")) // BR 부분 (브로드캐스트)
            {
                fromID = tokens[1].Trim();
                string msg = tokens[2];
                Console.WriteLine("[전체]{0}:{1}", fromID, msg);
                //

                s.Send(Encoding.Unicode.GetBytes("BR_Success:"));
            }
            else if (code.Equals("TO"))
            {
                fromID = tokens[1].Trim();
                toID = tokens[2].Trim();
                string msg = tokens[3];
                string rMsg = "[From:" + fromID + "][TO:" + toID + "]" + msg;
                Console.WriteLine(rMsg);

                //
                SendTo(toID, m);
                s.Send(Encoding.Unicode.GetBytes("To_Success:"));
                
            }
            else
            {
                Broadcast(s, m);
            }
        }

        void SendTo(string id, string m)
        {
            Socket socket;
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            if (connectedClients.ContainsKey(id))
            {
                // 
                connectedClients.TryGetValue(id, out socket!);
                try { 
                    socket.Send(bytes); 
                } catch {
                    Disconnected(client.Value);
                }
            }
        }

        void Broadcast(Socket s, string msg) // 5-2ㅡ모든 클라이언트에게 Send, 보낸사람 s (Socket 값), 보낸메세지 msg (메세지 값) 
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            //
            {
                foreach (keyvaluepair<string, Socket> client in connectedClients.ToArray())
                {
                    try
                    {
                        //5-2 send
                        //
                        if (s! = clinet.Value)
                        {
                            client.Value.Send(bytes);
                        }
                    }
                    catch (Exception)
                    {
                        Disconnected(client.Value);
                    }
                }
            }
        }
    }
}