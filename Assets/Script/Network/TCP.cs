/*using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCP : MonoBehaviour
{
    public static TCP tcp_Instance { get; private set; }

    private Socket socketServer = null;
    private Socket socketClient = null;

    private Buffer sendBuffer;
    private Buffer receiveBuffer;

    private bool bServer = false;
    private volatile bool bConnect = false;
    private volatile bool bThread = false;
    private Thread thread = null;

    private readonly object sendLock = new object();
    private readonly object receiveLock = new object();

    public bool IsServer() => bServer;
    public bool IsConnect() => bConnect;

    private void Awake()
    {
        if (tcp_Instance == null)
        {
            tcp_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sendBuffer = new Buffer();
        receiveBuffer = new Buffer();
    }

    private void OnApplicationQuit()
    {
        Stop();
    }

    public int Send(byte[] data, int size)
    {
        lock (sendLock)
        {
            if (sendBuffer == null) return 0;
            return sendBuffer.Push(data, size);
        }
    }

    public int Receive(ref byte[] data, int size)
    {
        lock (receiveLock)
        {
            if (receiveBuffer == null) return 0;
            return receiveBuffer.Pop(ref data, size);
        }
    }

    // [핵심 1] 서버 시작 함수
    public void StartServer(int port, int backlog)
    {
        if (thread != null) Stop();
        bServer = true;
        // 서버 전용 루프를 스레드로 실행
        StartThread(() => ServerLoop(port, backlog));
    }

    // [핵심 2] 클라이언트 연결 함수
    public void ConnectToServer(string address, int port)
    {
        if (thread != null) Stop();
        bServer = false;
        // 클라이언트 전용 루프를 스레드로 실행
        StartThread(() => ClientLoop(address, port));
    }

    public void Stop()
    {
        bThread = false;
        // 소켓을 먼저 닫아 블로킹 상태(Accept, Receive 등)에서 즉시 빠져나오게 함
        if (socketClient != null) socketClient.Close();
        if (socketServer != null) socketServer.Close();

        if (thread != null)
        {
            thread.Join(500); // 스레드 종료 대기
            thread = null;
        }

        socketClient = null;
        socketServer = null;
        bConnect = false;
    }

    private void StartThread(ThreadStart loop)
    {
        bThread = true;
        thread = new Thread(loop);
        thread.IsBackground = true;
        thread.Start();
    }

    // [핵심 3] 서버 전용 루프
    private void ServerLoop(int port, int backlog)
    {
        try
        {
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketServer.Bind(new IPEndPoint(IPAddress.Any, port));
            socketServer.Listen(backlog);
            Debug.Log("서버: 클라이언트 대기 중...");

            socketClient = socketServer.Accept(); // 클라이언트 접속까지 대기
            bConnect = true;
            Debug.Log("서버: 클라이언트 접속 성공!");
        }
        catch (System.Exception e)
        {
            // Stop()에 의해 소켓이 닫히면 예외 발생, 정상적인 종료 과정임
            if (bThread) Debug.LogError($"서버 오류: {e.Message}");
            Stop();
            return;
        }

        // 접속 후 송수신 반복
        while (bThread && bConnect)
        {
            UpdateSend();
            UpdateReceive();
            Thread.Sleep(10);
        }
        Stop();
    }

    // [핵심 4] 클라이언트 전용 루프
    private void ClientLoop(string address, int port)
    {
        try
        {
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Debug.Log("클라이언트: 서버에 접속 시도...");
            socketClient.Connect(address, port); // 서버 접속까지 대기
            bConnect = true;
            Debug.Log("클라이언트: 서버 접속 성공!");
        }
        catch (System.Exception e)
        {
            if (bThread) Debug.LogError($"클라이언트 오류: {e.Message}");
            Stop();
            return;
        }

        // 접속 후 송수신 반복
        while (bThread && bConnect)
        {
            UpdateSend();
            UpdateReceive();
            Thread.Sleep(10);
        }
        Stop();
    }

    private void UpdateSend()
    {
        if (socketClient == null || !socketClient.Connected) return;

        try
        {
            if (socketClient.Poll(0, SelectMode.SelectWrite))
            {
                lock (sendLock)
                {
                    byte[] data = new byte[1024];
                    int size = sendBuffer.Pop(ref data, data.Length);
                    if (size > 0)
                    {
                        socketClient.Send(data, 0, size, SocketFlags.None);
                    }
                }
            }
        }
        catch (SocketException) { bConnect = false; }
    }

    private void UpdateReceive()
    {
        if (socketClient == null || !socketClient.Connected) return;
        try
        {
            while (socketClient.Available > 0)
            {
                byte[] data = new byte[1024];
                int size = socketClient.Receive(data, 0, data.Length, SocketFlags.None);
                if (size > 0)
                {
                    lock (receiveLock)
                    {
                        receiveBuffer.Push(data, size);
                    }
                }
                else // size가 0이면 정상 종료
                {
                    bConnect = false;
                    break;
                }
            }
        }
        catch (SocketException)
        {
            bConnect = false; // 연결 강제 종료
        }
    }
}
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCP : MonoBehaviour
{
    public static TCP tcp_Instance { get; private set; }

    Socket socketServer = null;
    Socket socketClient = null;

    // 임시 버퍼
    Buffer sendBuffer;
    Buffer receiveBuffer;

    bool bServer = false; // 해당 객체가 서버인지 아닌지
    bool bConnect = false; // 1대1통신 연결이 모두 성공했는지
    bool bThread = false; // 스레드 생성 
    Thread thread = null;

    // 스레드 안전성(Thread Safety)을 위해 lock을 사용할 객체
    private readonly object sendLock = new object();
    private readonly object receiveLock = new object();

    public bool IsServer() => bServer;
    public bool IsConnect() => bConnect;

    // 싱글톤으로 관리
    private void Awake()
    {
        if(tcp_Instance == null)
        {
            tcp_Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sendBuffer = new Buffer();
        receiveBuffer = new Buffer();
    }

    public int Send(byte[] data, int size)
    {
        // 동시 접근 불가능하도록
        lock (sendLock)
        {
            if (sendBuffer == null)
                return 0;
            return sendBuffer.Push(data, size);
        }
    }

    public int Receive(ref byte[] data, int size)
    {
        lock (receiveLock)
        {
            if (receiveBuffer == null)
                return 0;
            return receiveBuffer.Pop(ref data, size);
        }
    }

    public void StopServer()
    {

        ClientDisconnect();

        if (socketServer != null)
        {
            socketServer.Close();
            socketServer = null;
        }

        // 실행 중인 Thread에 대해 루프를 빠져나올 수 있도록 플래그 설정
        bThread = false;
        if (thread != null)
        {
            // 스레드가 완전히 종료될 때까지 대기
            thread.Join();
            // 종료되면 해제
            thread = null;
        }
        bConnect = false;
        bServer = false;
    }

    public bool StartServer(int port, int backlog)
    {
        if (port == null) return false;

        // IPv4 기반의 TCP 통신용 소켓
        socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // 소켓을 특정 IP + 포트에 연결
        // IPAddress.Any : 모든 네트워크 인터페이스에서 접속 허용
        socketServer.Bind(new IPEndPoint(IPAddress.Any, port));
        // 연결 요청 받을 준비
        // backlog : 대기열 크기 (동시에 몇 개의 연결 요청을 쌓아둘 수 있는지)
        socketServer.Listen(backlog);

        bServer = true;
        Debug.Log("서버 시작");

        return StartThread();
    }

    bool StartThread()
    {
        bThread = true;
        // 네트워크 통신을 위한 새로운 Thread 생성
        thread = new Thread(new ThreadStart(NetworkUpdate));
        thread.Start();

        return true;
    }

    public void NetworkUpdate()
    {
        while (bThread)
        {
            if(IsServer())
                WaitClient(); // 클라이언트 연결을 기다림

            if(socketClient != null && bConnect == true)
            {
                // 모든 연결 되었을 때, 송수식을 반복
                UpdateSend();
                UpdateReceive();
            }
            // 과부화 방지용 
            Thread.Sleep(5);
        }
    }

    // 본인이 서버일 때 클라이언트 접속이 시도되었는지를 확인
    private void WaitClient()
    {
        // Poll : 소켓이 읽을 수 있는 상태인지를 반환
        // 접속 요청이 들어왔을 때 확인
        if (socketServer == null) return;
        try
        {
            if (socketServer.Poll(0, SelectMode.SelectRead))
            {
                socketClient = socketServer.Accept();
                bConnect = true;
                Debug.Log("클라이언트 접속 성공");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Accept 오류: {e.Message}");
        }
    }

    private void UpdateSend()
    {
        if(socketClient.Poll(0, SelectMode.SelectWrite))
        {
            byte[] data = new byte[1024];
            int iSize = sendBuffer.Pop(ref data, data.Length);
            while (iSize > 0)
            {
                // 버퍼가 모두 빌 때까지 반복
                socketClient.Send(data, iSize, SocketFlags.None);
                iSize = sendBuffer.Pop(ref data, data.Length);
            }
        }
    }

   
    private void UpdateReceive()
    {
        if (socketClient == null || !socketClient.Connected) return;
        try
        {
            // while문의 경우 무한루프에 걸릴 수 있어, if로 한번씩만 체크
            if (socketClient.Poll(0, SelectMode.SelectRead))
            {
                // 방어코드 한 번 더
                if (socketClient.Available > 0)
                {
                    byte[] data = new byte[1024];
                    // socketClient.Reeive
                    // 반환값 > 0 : 수신된 실제 바이트 수
                    // 반환값 = 0 : 원격 호스트가 연결을 종료함
                    // 반홤값 = -1 : 오류 발생, 보통 try-catch로 오류를 던져줌
                    int iSize = socketClient.Receive(data, data.Length, SocketFlags.None);
                    if (iSize == 0)
                    {
                        // 상대방이 정상적으로 연결을 종료했음
                        ClientDisconnect();   // 연결 종료
                    }
                    else if (iSize > 0)
                    {
                        // 수신된 데이터가 있으므로 처리
                        receiveBuffer.Push(data, iSize);
                    }
                }
            }
        }
        catch (SocketException)
        {
            bConnect = false;
        }
    }

    // 클라이언트 연결
    public bool ClientConnect(string address, int port)
    {
        if (address == null) return false;
        bool ret = false;
        socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socketClient.Connect(address, port);  // 서버에 접속하고 싶다고 요청을 보냄
        if (!socketClient.Connected) return false;
        ret = StartThread();

        if(ret == true)
        {
            bConnect = true;
            Debug.Log("클라이언트 접속 성공");
        }

        return bConnect;
    }


    // 클라이언트 연결 해제
    public void ClientDisconnect()
    {
        if (socketClient != null)
        {
            try
            {
                // Shutdown : 소켓 기능을 제한하는 메서드
                // Both : 보내기 받기 모두 제한
                // Receive : 수신 제한
                // Send : 송신제한
                socketClient.Shutdown(SocketShutdown.Both);
                // 연결을 닫고 리소스 모두 해제
                socketClient.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"소켓 닫기 실패: {e.Message}");
            }
            socketClient = null;
        }
    }

    public void Disconnect()
    {
        Debug.Log($"{(bServer ? "서버" : "클라이언트")} 연결 해제 시작");

        bThread = false;
        bConnect = false;

        ClientDisconnect();

        // 서버인 경우 Listen 소켓 닫기
        if (bServer && socketServer != null)
        {
            try
            {
                socketServer.Close();
                Debug.Log("서버 Listen 소켓 닫힘");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"서버 소켓 닫기 실패: {e.Message}");
            }
            socketServer = null;
        }

        if(thread != null)
        {
            if (!thread.Join(1000))
            {
                Debug.LogWarning("스레드 강제 종료");
            }
            thread = null;
        }
        bConnect = false;
        bServer = false;
    }
}