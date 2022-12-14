using System.Globalization;
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
        Dictionary<string, string> UserBRD = new Dictionary<string, string>(); 

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
                    Console.WriteLine(tokens[1] + tokens[2]);
                }
                else if (tokens[0].Trim() == "IDC")
                {
                    Console.WriteLine("수신:" + tokens[1]);
                    nameID = null;
                }
                else if (tokens[0].Trim() == "BR")
                {
                    UserBRD.Add(tokens[1].Trim(), tokens[2].Trim());
                    Console.WriteLine("["+ tokens[1].Trim() + "]님이 "+ tokens[2].Trim() + "메세지를 요청했습니다.\nEX) AC! 해당 닉네임! 의 형식으로 요청을 수락해주세요"  );
                }
                else if (tokens[0].Trim() == "TO" && tokens[1].Trim() == "user")
                {
                    Console.WriteLine("[" + tokens[2].Trim() + "->" + tokens[3].Trim() + "] : "+ tokens[4].Trim());
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
            clearConsole();
            Console.WriteLine("ID를 먼저 입력해주세요!");

            byte[] dataID;
            string r = "manager";
            string cd;
            string jsonManager;
            nameID = null;


            // JSON 직렬화 설정
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            jso.WriteIndented = true;

            do
            {
  
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split('!');
                string m;
                if (nameID != null)
                {
                    // 보내는 부분
                    Console.WriteLine($"매니저 {nameID}님 방갑습니다!\n다음과 같은 포맷으로 메세지를 입력하세요\nex) 명령어! 보내고 싶은 메시지\n");
                   
                }

                if (tokens[0].Equals("ID")) {
                    nameID = tokens[1].Trim();

                    ClientService managerID = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerID, jso);

                    dataID = Encoding.Unicode.GetBytes(jsonManager);
                    clientSocket.Send(dataID);

                }

                else if (tokens[0].Equals("BR"))
                {
                    ClientService managerBR = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = tokens[1].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerBR, jso);

                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[전체전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }

                else if (tokens[0].Equals("AC"))
                {
                    ClientService managerAC = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = UserBRD[tokens[1].Trim()] };
                    jsonManager = JsonSerializer.Serialize(managerAC, jso);

                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[AC 전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }

                else if (tokens[0].Equals("TO"))
                {
                    ClientService managerTo = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), Toid = tokens[1].Trim(), message = tokens[2].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerTo, jso);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[{0}에게 전송]:{1}", tokens[1], tokens[2]);
                    try { ClientSocket.Send(data); } catch { }
                }
                else if (tokens[0].Equals("INFO"))
                {
                    ClientService managerInfo = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerInfo, jso);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("INFO 정보 요청");
                    try { ClientSocket.Send(data); } catch { }
                }
                else if (tokens[0].Equals("RC"))
                {
                    // tokens[1].Trim() -> 보낸 사람의 ID tokens[2].Trim() -> 받는 사람 ID
                    ClientService managerRC = new ClientService() { id = tokens[1].Trim(), Toid = tokens[2].Trim(), roll = r, commend = tokens[0].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerRC, jso);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("RC 정보 요청");
                    try { ClientSocket.Send(data); } catch { }
                }
               
                else if (tokens[0].Equals("OUT"))
                {
                    ClientService managerOUT = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = tokens[1].Trim() };
                    jsonManager = JsonSerializer.Serialize(managerOUT, jso);
                    data = Encoding.Unicode.GetBytes(jsonManager);
                    Console.WriteLine("[OUT 실행] : {0}님", tokens[1]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }
                else if (tokens[0].Equals("CL"))
                {
                    clearConsole();
                }
                else
                {
                    Console.WriteLine("잘못입력하셨습니다!");
                }

            } while (true);
        }
        void UserSend()
        {
            clearConsole();
            Console.WriteLine("ID를 먼저 입력해주세요!");

            byte[] dataID;
            string r = "user";
            string cd;
            string jsonUser;
            nameID = null;


            // JSON 직렬화 설정
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            jso.WriteIndented = true;

            do
            {
                // 보내는 부분
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split('!');
                string m;

                if (nameID != null)
                {
                    // 보내는 부분
                    Console.WriteLine($"유저 {nameID}님 방갑습니다!\n다음과 같은 포맷으로 메세지를 입력하세요\nex) 명령어! 보내고 싶은 메시지\n");
                }

                if (tokens[0].Equals("ID"))
                {
                    Console.WriteLine("ID를 입력하세요");
                    nameID = tokens[1].Trim();

                    ClientService userID = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim() };
                    jsonUser = JsonSerializer.Serialize(userID, jso);

                    dataID = Encoding.Unicode.GetBytes(jsonUser);
                    clientSocket.Send(dataID);

                }

                else if (tokens[0].Equals("BR"))
                {
                    ClientService UserBR = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), message = tokens[1].Trim() };
                    jsonUser = JsonSerializer.Serialize(UserBR, jso);

                    data = Encoding.Unicode.GetBytes(jsonUser);
                    Console.WriteLine("[BR 요청]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { Console.WriteLine("잘못 입력하셨습니다!"); }
                }

                else if (tokens[0].Equals("TO"))
                {
                    ClientService UserTo = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim(), Toid = tokens[1].Trim(), message = tokens[2].Trim() };
                    jsonUser = JsonSerializer.Serialize(UserTo, jso);
                    data = Encoding.Unicode.GetBytes(jsonUser);
                    try { ClientSocket.Send(data); } catch { }
                }

                else if (tokens[0].Equals("INFO"))
                {
                    ClientService userInfo = new ClientService() { id = nameID, roll = r, commend = tokens[0].Trim() };
                    jsonUser = JsonSerializer.Serialize(userInfo, jso);
                    data = Encoding.Unicode.GetBytes(jsonUser);
                    Console.WriteLine("INFO 정보 요청");
                    try { ClientSocket.Send(data); } catch { }
                }
               
                else if (tokens[0].Equals("RC"))
                {
                    ClientService userRC = new ClientService() { id = nameID,Toid = tokens[1].Trim(), roll = r, commend = tokens[0].Trim() };
                    jsonUser = JsonSerializer.Serialize(userRC, jso);
                    data = Encoding.Unicode.GetBytes(jsonUser);
                    Console.WriteLine("RC 정보 요청");
                    try { ClientSocket.Send(data); } catch { }
                }
                
                else if (tokens[0].Equals("CL"))
                {
                    clearConsole();
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
                
            } while (type != "0");
            Console.WriteLine("프로그램을 종료합니다!");
         
        }
       

        void clearConsole()
        {
            Console.Clear();
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
