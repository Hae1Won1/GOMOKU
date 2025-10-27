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

    // [�ٽ� 1] ���� ���� �Լ�
    public void StartServer(int port, int backlog)
    {
        if (thread != null) Stop();
        bServer = true;
        // ���� ���� ������ ������� ����
        StartThread(() => ServerLoop(port, backlog));
    }

    // [�ٽ� 2] Ŭ���̾�Ʈ ���� �Լ�
    public void ConnectToServer(string address, int port)
    {
        if (thread != null) Stop();
        bServer = false;
        // Ŭ���̾�Ʈ ���� ������ ������� ����
        StartThread(() => ClientLoop(address, port));
    }

    public void Stop()
    {
        bThread = false;
        // ������ ���� �ݾ� ���ŷ ����(Accept, Receive ��)���� ��� ���������� ��
        if (socketClient != null) socketClient.Close();
        if (socketServer != null) socketServer.Close();

        if (thread != null)
        {
            thread.Join(500); // ������ ���� ���
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

    // [�ٽ� 3] ���� ���� ����
    private void ServerLoop(int port, int backlog)
    {
        try
        {
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketServer.Bind(new IPEndPoint(IPAddress.Any, port));
            socketServer.Listen(backlog);
            Debug.Log("����: Ŭ���̾�Ʈ ��� ��...");

            socketClient = socketServer.Accept(); // Ŭ���̾�Ʈ ���ӱ��� ���
            bConnect = true;
            Debug.Log("����: Ŭ���̾�Ʈ ���� ����!");
        }
        catch (System.Exception e)
        {
            // Stop()�� ���� ������ ������ ���� �߻�, �������� ���� ������
            if (bThread) Debug.LogError($"���� ����: {e.Message}");
            Stop();
            return;
        }

        // ���� �� �ۼ��� �ݺ�
        while (bThread && bConnect)
        {
            UpdateSend();
            UpdateReceive();
            Thread.Sleep(10);
        }
        Stop();
    }

    // [�ٽ� 4] Ŭ���̾�Ʈ ���� ����
    private void ClientLoop(string address, int port)
    {
        try
        {
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Debug.Log("Ŭ���̾�Ʈ: ������ ���� �õ�...");
            socketClient.Connect(address, port); // ���� ���ӱ��� ���
            bConnect = true;
            Debug.Log("Ŭ���̾�Ʈ: ���� ���� ����!");
        }
        catch (System.Exception e)
        {
            if (bThread) Debug.LogError($"Ŭ���̾�Ʈ ����: {e.Message}");
            Stop();
            return;
        }

        // ���� �� �ۼ��� �ݺ�
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
                else // size�� 0�̸� ���� ����
                {
                    bConnect = false;
                    break;
                }
            }
        }
        catch (SocketException)
        {
            bConnect = false; // ���� ���� ����
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

    // �ӽ� ����
    Buffer sendBuffer;
    Buffer receiveBuffer;

    bool bServer = false; // �ش� ��ü�� �������� �ƴ���
    bool bConnect = false; // 1��1��� ������ ��� �����ߴ���
    bool bThread = false; // ������ ���� 
    Thread thread = null;

    // ������ ������(Thread Safety)�� ���� lock�� ����� ��ü
    private readonly object sendLock = new object();
    private readonly object receiveLock = new object();

    public bool IsServer() => bServer;
    public bool IsConnect() => bConnect;

    // �̱������� ����
    private void Awake()
    {
        if(tcp_Instance == null)
        {
            tcp_Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �ı����� �ʵ���
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
        // ���� ���� �Ұ����ϵ���
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

        // ���� ���� Thread�� ���� ������ �������� �� �ֵ��� �÷��� ����
        bThread = false;
        if (thread != null)
        {
            // �����尡 ������ ����� ������ ���
            thread.Join();
            // ����Ǹ� ����
            thread = null;
        }
        bConnect = false;
        bServer = false;
    }

    public bool StartServer(int port, int backlog)
    {
        if (port == null) return false;

        // IPv4 ����� TCP ��ſ� ����
        socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // ������ Ư�� IP + ��Ʈ�� ����
        // IPAddress.Any : ��� ��Ʈ��ũ �������̽����� ���� ���
        socketServer.Bind(new IPEndPoint(IPAddress.Any, port));
        // ���� ��û ���� �غ�
        // backlog : ��⿭ ũ�� (���ÿ� �� ���� ���� ��û�� �׾Ƶ� �� �ִ���)
        socketServer.Listen(backlog);

        bServer = true;
        Debug.Log("���� ����");

        return StartThread();
    }

    bool StartThread()
    {
        bThread = true;
        // ��Ʈ��ũ ����� ���� ���ο� Thread ����
        thread = new Thread(new ThreadStart(NetworkUpdate));
        thread.Start();

        return true;
    }

    public void NetworkUpdate()
    {
        while (bThread)
        {
            if(IsServer())
                WaitClient(); // Ŭ���̾�Ʈ ������ ��ٸ�

            if(socketClient != null && bConnect == true)
            {
                // ��� ���� �Ǿ��� ��, �ۼ����� �ݺ�
                UpdateSend();
                UpdateReceive();
            }
            // ����ȭ ������ 
            Thread.Sleep(5);
        }
    }

    // ������ ������ �� Ŭ���̾�Ʈ ������ �õ��Ǿ������� Ȯ��
    private void WaitClient()
    {
        // Poll : ������ ���� �� �ִ� ���������� ��ȯ
        // ���� ��û�� ������ �� Ȯ��
        if (socketServer == null) return;
        try
        {
            if (socketServer.Poll(0, SelectMode.SelectRead))
            {
                socketClient = socketServer.Accept();
                bConnect = true;
                Debug.Log("Ŭ���̾�Ʈ ���� ����");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Accept ����: {e.Message}");
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
                // ���۰� ��� �� ������ �ݺ�
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
            // while���� ��� ���ѷ����� �ɸ� �� �־�, if�� �ѹ����� üũ
            if (socketClient.Poll(0, SelectMode.SelectRead))
            {
                // ����ڵ� �� �� ��
                if (socketClient.Available > 0)
                {
                    byte[] data = new byte[1024];
                    // socketClient.Reeive
                    // ��ȯ�� > 0 : ���ŵ� ���� ����Ʈ ��
                    // ��ȯ�� = 0 : ���� ȣ��Ʈ�� ������ ������
                    // ���c�� = -1 : ���� �߻�, ���� try-catch�� ������ ������
                    int iSize = socketClient.Receive(data, data.Length, SocketFlags.None);
                    if (iSize == 0)
                    {
                        // ������ ���������� ������ ��������
                        ClientDisconnect();   // ���� ����
                    }
                    else if (iSize > 0)
                    {
                        // ���ŵ� �����Ͱ� �����Ƿ� ó��
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

    // Ŭ���̾�Ʈ ����
    public bool ClientConnect(string address, int port)
    {
        if (address == null) return false;
        bool ret = false;
        socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socketClient.Connect(address, port);  // ������ �����ϰ� �ʹٰ� ��û�� ����
        if (!socketClient.Connected) return false;
        ret = StartThread();

        if(ret == true)
        {
            bConnect = true;
            Debug.Log("Ŭ���̾�Ʈ ���� ����");
        }

        return bConnect;
    }


    // Ŭ���̾�Ʈ ���� ����
    public void ClientDisconnect()
    {
        if (socketClient != null)
        {
            try
            {
                // Shutdown : ���� ����� �����ϴ� �޼���
                // Both : ������ �ޱ� ��� ����
                // Receive : ���� ����
                // Send : �۽�����
                socketClient.Shutdown(SocketShutdown.Both);
                // ������ �ݰ� ���ҽ� ��� ����
                socketClient.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"���� �ݱ� ����: {e.Message}");
            }
            socketClient = null;
        }
    }

    public void Disconnect()
    {
        Debug.Log($"{(bServer ? "����" : "Ŭ���̾�Ʈ")} ���� ���� ����");

        bThread = false;
        bConnect = false;

        ClientDisconnect();

        // ������ ��� Listen ���� �ݱ�
        if (bServer && socketServer != null)
        {
            try
            {
                socketServer.Close();
                Debug.Log("���� Listen ���� ����");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"���� ���� �ݱ� ����: {e.Message}");
            }
            socketServer = null;
        }

        if(thread != null)
        {
            if (!thread.Join(1000))
            {
                Debug.LogWarning("������ ���� ����");
            }
            thread = null;
        }
        bConnect = false;
        bServer = false;
    }
}