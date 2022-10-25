using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;


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

        public Client() // 1. 소켓 생성
        {
            ClientSocket = new(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
        }

        void Init() // 
        {
            ClientSocket.Connect(EndPoint); // 2. 서버 연결
            Console.WriteLine($"Server connected.");

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
            ClientSocket.ReceiveAsync(args);

            Send(); // 3-1. 전송
        }


        void Received(object? sender, SocketAsyncEventArgs e)
        {
            try // 확장 가능성 있음
            {
                byte[] data = new byte[BufferSize];
                Socket server = (Socket)sender!;
                int n=server.Receive(data);

                string str = Encoding.Unicode.GetString(data);
                str = str.Replace("\0", "");
                Console.WriteLine("수신:"+str); // Split 예쁘게 구분해서 상세하게 보여주게 할 수 있음


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

        void Send()
        {
            byte[] dataID;
            Console.WriteLine("ID를 입력하세요");
            string nameID = Console.ReadLine()!;
            // ReadLine() ! => 느낌표 내 코드상 널이 아니야
            byte[] massage = "ID:" + nameID + ":";
            dataID = Encoding.Unicode.GetBytes(message);
            clientSocket.Send(dataID);
            //

            Console.WriteLine("특정 사용자에게 보낼 때는 사용자ID:메시지 로 입력하시고\n" +
                "브로드캐스트하려면 BR:메시지 를 입력하세요");
            do // 3-1. 읽어서 전송 무한 반복
            {
                // 입력할 때 까지 기다리는 곳
                byte[] data; 
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split(':');
                string m;
                if (tokens[0].Equals("BR"))
                {
                    // 브로드캐스트 메세지
                    m = "BR:" + nameID + ":" + totoken[1]; // "BR" 메세지 서버로 감 서버쪽 receive 함수로 이동

                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[전체전송]{0}", tokens[1]);
                }
                else //  (tokens[0].Equals("TO"))
                {
                    //
                    m = "TO:" + nameID + ":" + tokens[1] + ":" + tokens[2] + ":";
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[{0}에게 전송]:{1}", tokens[0], tokens[1]);
                 }

                try { ClientSocket.Send(data); } catch { }


            } while (true);
        }
    }
}
