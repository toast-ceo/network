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
        Dictionary<string, string> UserBR = new Dictionary<string, string>(); 

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

        private string nameID;

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
                string[] tokens = str.Split('!');
                if (tokens[0].Trim() == "ID")
                {
                    Console.WriteLine("수신:" + tokens[1]);
                    nameID = "";
                } else if (tokens[0].Trim() == "BR")
                {
                    UserBR.Add(tokens[1].Trim(), tokens[2].Trim());
                    Console.WriteLine("["+ tokens[1] + "]님이 "+ tokens[2] + "메세지를 요청했습니다.\nEX) AC! 해당 닉네임! 의 형식으로 요청을 수락해주세요"  );
                }
                else
                {
                    Console.WriteLine("수신:" + str);

                }
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
            string r = "manager";
            string cd;
            string jsonManager;
            nameID = "";


            // JSON 직렬화 설정
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            jso.WriteIndented = true;

            do
            {
                // 보내는 부분
                Console.WriteLine($"매니저 {nameID}님 방갑습니다!\n다음과 같은 포맷으로 메세지를 입력하세요\nex) 명령어! 보내고 싶은 메시지\n");
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split('!');
                string m;


                if (tokens[0].Equals("ID")){
                    Console.WriteLine("ID를 입력하세요");
                    nameID = Console.ReadLine()!;
                    
                    ClientService managerID = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerID, jso);

                    dataID = Encoding.Unicode.GetBytes(jsonManager);
                    clientSocket.Send(dataID);

                }

                else if (tokens[0].Equals("BR"))
                {
                    ClientService managerBR = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = tokens[1].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerBR, jso);

                    Console.WriteLine(jsonManager);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[전체전송]{0}", tokens[2]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }
                else if (tokens[0].Equals("AC"))
                {
                    ClientService managerBR = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = UserBR[tokens[1].Trim()] };
                    jsonManager = JsonSerializer.Serialize(managerBR, jso);

                    Console.WriteLine(jsonManager);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[AC 전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }
                /*    else if (tokens[0].Equals("File"))
                    {
                        SendFile(tokens[1]);
                    }
                */
                else if (tokens[0].Equals("TO"))
                {
                    ClientService managerTo = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), Toid = tokens[1].Trim(), message = tokens[2].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerTo, jso);
                    Console.WriteLine(jsonManager);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[{0}에게 전송]:{1}", tokens[1], tokens[2]);
                    try { ClientSocket.Send(data); } catch { }
                }
                else
                {
                    Console.WriteLine("잘못입력하셨습니다!");
                }

            } while (true);
        }
        void UserSend()
        {
            byte[] dataID;
            string r = "user";
            string cd;
            string jsonUser;
            nameID = "";


            // JSON 직렬화 설정
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            jso.WriteIndented = true;

            do
            {
                // 보내는 부분
                Console.WriteLine($"유저 {nameID}님 방갑습니다!\n다음과 같은 포맷으로 메세지를 입력하세요\nex) 명령어! 보내고 싶은 메시지\n");
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split('!');
                string m;


                if (tokens[0].Equals("ID"))
                {
                    Console.WriteLine("ID를 입력하세요");
                    nameID = Console.ReadLine()!;

                    ClientService userID = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim() };
                    jsonUser = JsonSerializer.Serialize(userID, jso);

                    dataID = Encoding.Unicode.GetBytes(jsonUser);
                    clientSocket.Send(dataID);

                }

                else if (tokens[0].Equals("BR"))
                {
                    ClientService UserBR = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = tokens[1].Trim() };
                    jsonUser = JsonSerializer.Serialize(UserBR, jso);

                    Console.WriteLine(jsonUser);
                    data = Encoding.Unicode.GetBytes(jsonUser);
                    Console.WriteLine("[BR 요청]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }
                /*    else if (tokens[0].Equals("File"))
                    {
                        SendFile(tokens[1]);
                    }
                */
                else if (tokens[0].Equals("TO"))
                {
                    ClientService managerTo = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), Toid = tokens[1].Trim(), message = tokens[2].Trim() };
                    jsonUser = JsonSerializer.Serialize(managerTo, jso);
                    Console.WriteLine(jsonUser);
                    data = Encoding.Unicode.GetBytes(jsonUser);
                    Console.WriteLine("[{0}에게 전송]:{1}", tokens[1], tokens[2]);
                    try { ClientSocket.Send(data); } catch { }
                }
                else
                {
                    Console.WriteLine("잘못입력하셨습니다!");
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
                    Console.WriteLine("잘못 입력했습니다! 다음과 같은 형식으로 입력하세요! ex) 숫자 입력");
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
        public string Toid { get; set; }
        public string roll { get; set; }
        public string commend { get; set; }
        public string message { get; set; }


    }

}
