using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace AClient
{
    public class Client
    {
        private readonly static int BufferSize = 4096;

        public static void Main()
        {
            try
            {
                new Client().Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any key to exit the program.");
            Console.ReadKey();
        }


        private Socket clientSocket;
        public Socket ClientSocket
        {
            get => clientSocket;
            set => clientSocket = value;
        }
        private readonly IPEndPoint EndPoint = new(IPAddress.Parse("127.0.0.1"), 5001);

        public Client()
        {
            ClientSocket = new(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
        }

        void Init()
        {
            ClientSocket.Connect(EndPoint);
            Console.WriteLine($"Server connected.");

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
            ClientSocket.ReceiveAsync(args);

            Send();
        }


        void Received(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                byte[] data = new byte[BufferSize];
                Socket server = (Socket)sender!;
                int n = server.Receive(data);

                string str = Encoding.Unicode.GetString(data);
                str = str.Replace("\0", "");
                Console.WriteLine("수신:" + str);


                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                ClientSocket.ReceiveAsync(args);
            }
            catch (Exception)
            {
                Console.WriteLine($"Server disconnected.");
                ClientSocket.Close();
            }
        }

        void ManagerSend()
        {
            byte[] dataID;
            Console.WriteLine("ID를 입력하세요");
            string nameID = Console.ReadLine()!;
            string r = "manager";
            string cd;

            // ID 입력
            // JSON 직렬화 
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            jso.WriteIndented = true;

            cd = "ID";
            ClientService managerID  = new ClientService() { id = nameID, roll = r,  commend = cd};
            string jsonManager = JsonSerializer.Serialize(managerID, jso);

            dataID = Encoding.Unicode.GetBytes(jsonManager);
            clientSocket.Send(dataID);

            // 보내는 부분
            Console.WriteLine("다음과 같은 포맷으로 메세지를 입력하세요\nex) 명령어! 보내고 싶은 메시지");
            do
            {
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split('!');
                string m;

                if (tokens[0].Equals("BR"))
                {
                    cd = "BR";
                    ClientService managerBR = new ClientService() { id = nameID, roll = r, commend = cd, message = tokens[1] };
                    jsonManager = JsonSerializer.Serialize(managerBR, jso);

                    Console.WriteLine(jsonManager);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[전체전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { }
                }
                /*    else if (tokens[0].Equals("File"))
                    {
                        SendFile(tokens[1]);
                    }*//*
                    else //  (tokens[0].Equals("TO"))
                    {
                        //
                        m = "TO:" + nameID + ":" + tokens[0] + ":" + tokens[1] + ":";
                        data = Encoding.Unicode.GetBytes(m);
                        Console.WriteLine("[{0}에게 전송]:{1}", tokens[0], tokens[1]);
                        try { ClientSocket.Send(data); } catch { }
                    }
    */
            } while (true);
        }
        void UserSend()
        {
            byte[] dataID;
            Console.WriteLine("ID를 입력하세요");
            string nameID = Console.ReadLine()!;
            string r = "user";
            //
            string message = "ID:" + nameID + ":";
            dataID = Encoding.Unicode.GetBytes(message);
            clientSocket.Send(dataID);
            // 

            Console.WriteLine("특정 사용자에게 보낼 때는 사용자ID:메시지 로 입력하시고\n" +
                "브로드캐스트하려면 BR:메시지 를 입력하세요");
            do
            {
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split(':');
                string m;
                if (tokens[0].Equals("BR"))
                {
                    //
                    m = "BR:" + nameID + ":" + tokens[1] + ":";

                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[전체전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { }
                }
                else if (tokens[0].Equals("File"))
                {
                    SendFile(tokens[1]);
                }
                else //  (tokens[0].Equals("TO"))
                {
                    //
                    m = "TO:" + nameID + ":" + tokens[0] + ":" + tokens[1] + ":";
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[{0}에게 전송]:{1}", tokens[0], tokens[1]);
                    try { ClientSocket.Send(data); } catch { }
                }

            } while (true);
        }
        void Send()
        {
            string type;
            do {
                // 모드 설정
                Console.WriteLine("1. Manager 모드 2. User 모드 0. 나가기");
                type = Console.ReadLine()!;

                if(type == "1")
                {
                    ManagerSend();
                }
                else if (type == "2")
                {
                    UserSend();
                }
                else
                {
                    Console.WriteLine("잘못 입력했습니다! 다음과 같은 형식으로 입력하세요! ex) 1");
                }
                
            } while (type == "0");
            Console.WriteLine("프로그램을 종료합니다!");
         
        }

        void SendFile(string filename)
        {
            FileInfo fi = new FileInfo(filename);   
            string fileLength = fi.Length.ToString();

            byte[] bDts = Encoding.Unicode.GetBytes
                ("File:" + filename + ":" + fileLength + ":");
            clientSocket.Send(bDts);

            byte[] bDtsRx = new byte[4096];
            // FileShare - 파일 공유 나혼자 
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            long received = 0;
            while (received < fi.Length)
            {
                received += fs.Read(bDtsRx, 0, 4096);
                clientSocket.Send(bDtsRx);
                Array.Clear(bDtsRx); 
            }
            fs.Close();
            Console.WriteLine("파일 송신 종료");
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
