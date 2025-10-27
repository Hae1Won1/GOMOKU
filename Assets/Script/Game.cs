using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;
using TMPro;

public class Game : MonoBehaviour
{
    // ���� ���¿� ���� enum
    enum State
    {
        Start = 0,
        Game,
        End,
    };

    // ���ʿ� ���� enum
    enum Turn
    {
        I = 0,
        You,
    };

    // ���� black���� white������ ���� enum
    enum Stone
    {
        None = 0,
        White,
        Black,
    };

    #region GameInspector
    [Header("UI & GameObjects")]
    public InputField ip;
    public Sprite spriteWhite;
    public Sprite spriteBlack;
    public Image imageBoard;
    public Button buttonServer;
    public Button buttonClient;
    //public GameObject startUI;
    public Image turnIndicator;
    public TextMeshProUGUI stoneIText;
    public GameObject stonePrefab;
    public RectTransform boardParent; // GridLayoutGroup�� ��ư�� ������ UI�г�

    public GameObject lastStonePrefab; // ������ ����

    [Header("Game Settings")]
    [SerializeField] private int boardSize = 3; // �� ũ��
    [SerializeField] private int winCount = 3; // ���� ����

    private Image[,] stoneImages; // ���� ���� �ٵϵ� Image ����
    private int[,] board; // ������ ���� ���� (0: None, 1: White, 2: Black)
    private TCP tcp;
    private State state;
    private Stone stoneTurn;
    private Stone stoneI; // ���� �� ��
    private Stone stoneYou; // ����� �� ��
    private Stone stoneWinner;
 
    private float cellSizeX;
    private float cellSizeY;

    private bool bSetTcp = false;

    private System.Random random;

    [SerializeField] private ModeScriptable[] gameModes;
    private ModeScriptable currentMode;

    #endregion
    void Start()
    {
        tcp = FindAnyObjectByType<TCP>();
        state = State.Start;
        ApplyModeByBoardSize(boardSize); // �⺻���� ����
    }

    // ���� ���� �ʱ�ȭ
    void InitializeBoard()
    {
        if (stoneImages != null || board != null)
        {
            Debug.LogWarning("InitializeBoard: ���� ���尡 �����־� �����մϴ�");
            ClearBoard();
        }

        // ���� �����ŭ�� ��ǥ �迭 �����
        board = new int[boardSize, boardSize];
        stoneImages = new Image[boardSize, boardSize];


        // boardParent�� ũ�⸦ ������� �� ĭ�� ũ�� ���
        // RectTransform�� ũ��� layout�� ���� ���� �� �����Ƿ� �� �� ���
        cellSizeX = boardParent.rect.width / boardSize;
        cellSizeY = boardParent.rect.height / boardSize;
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                // ������ ���� ���� �� �θ� ����
                // stonePrefab �⺻ ���� : �̹��� ������Ʈ ����(���� �̹�����������Ʈ x), ũ�� ����
                // ���� �� ��ŭ �ٵ��� ĭ�� ���� �����ϰ� �ʱ�ȭ ���ִ� ����
                // �̸��ϰ� ������ ��ǥ�� 2���� �迭�� 
                GameObject newStone = Instantiate(stonePrefab, boardParent);
                // �̸� ������ ���� ���� ��� clone���� �߱� ������ 
                // �ν����Ϳ��� Ȯ���ϱ� ���ϵ��� ��ǥ�� ���� �̸� ����
                newStone.name = $"Stone_{x}_{y}";
                Image stoneImg = newStone.GetComponent<Image>();
                RectTransform stoneRect = newStone.GetComponent<RectTransform>();

                // ����� ��ĭ�� ũ��� Rect ����
                stoneRect.sizeDelta = new Vector2(cellSizeX, cellSizeY);
                // boardParent�� �ǹ� ��ġ�� ����� ��Ȯ�� ��ġ ��� �� �迭�� ��ǥ���� �Ҵ� �س���
                float posX = (x * cellSizeX) - (boardParent.rect.width * boardParent.pivot.x) + (cellSizeX / 2f);
                float posY = (y * cellSizeY) - (boardParent.rect.height * boardParent.pivot.y) + (cellSizeY / 2f);
                stoneRect.anchoredPosition = new Vector2(posX, posY);

                stoneImages[x, y] = stoneImg;
                stoneImages[x, y].gameObject.SetActive(false);
                board[x, y] = (int)Stone.None; // �ʱ�ȭ : ���� none
            }
        }
    }
    void Update()
    {
        if (!tcp.IsConnect()) return;

        switch (state)
        {
            case State.Start:
                UpdateStart();
                break;
            case State.Game:
                UpdateGame();
                break;
        }
    }

    // �� ����
    private void UpdateStart()
    {
        if (!bSetTcp)
        {
            // ���� ������ ����
            if (tcp.IsServer())
            {
                int stoneIsWhite = RandStoneColorSet();
                StoneColorSet(stoneIsWhite);
                byte[] data = new byte[2] { (byte)currentMode.boardSize, (byte)stoneIsWhite };
                tcp.Send(data, data.Length);
                bSetTcp = true;
            }
            else
            {
                byte[] data = new byte[2];
                int iSize = tcp.Receive(ref data, data.Length);
                Debug.Log($"iSize = {iSize}");
                if (iSize < 2)
                {
                    return;
                }
                ApplyModeByBoardSize(data[0]);
                StoneColorSet(data[1] == 1 ? 0 : 1);
                bSetTcp = true;
            }
            return;
        }
        state = State.Game;
        InitializeBoard();
        
        stoneTurn = Stone.Black;
        turnIndicator.sprite = spriteBlack;

        //if (tcp.IsServer())
        //{
        //    stoneI = Stone.White;
        //    stoneYou = Stone.Black;
        //}
        //else
        //{
        //    stoneI = Stone.Black;
        //    stoneYou = Stone.White;
        //}

        // ���� rand�� �̿��Ͽ� �������� ����
        if (stoneI == Stone.White) {
            stoneIText.text = "White";
        }
        else
        {
            stoneIText.text = "Black";
        }
        UIManager.ui_Instance.SetGameUI();
    }

    private void UpdateGame()
    {
        if (stoneTurn == stoneYou)
        {
            YourTurn();
        }
    }

    private void StoneColorSet(int color)
    {
        if(color == 0)
        {
            stoneI = Stone.Black;
            stoneYou = Stone.White;
        }
        else
        {
            stoneI = Stone.White;
            stoneYou = Stone.Black;
        }
    }

    private int RandStoneColorSet()
    {
        // ���� ����
        // 0��ȯ : ������ Black
        // 1��ȯ : ������ White
        random = new System.Random();
        int randNum = random.Next(0, 2);
        if(randNum % 2 == 0)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    private void EndGame(Stone winStone)
    {
        state = State.End;
        UIManager.ui_Instance.SetEndUI(winStone == stoneI);

    }

    // ������ �� ����� ��ǥ�� �Ѱ�
    // �ش� ��ġ�� ���� �ִ����� Ȯ�� �� �� board�� ���¸� ����
    private bool SetStone(int x, int y, Stone stone)
    {
        if (board[x, y] != (int)Stone.None)
        {
            Debug.Log($"���� : ��ġ {x}, {y}�� �̹� ���� ����.");
            return false;
        }

        board[x, y] = (int)stone;

        stoneImages[x, y].sprite = (stone == Stone.White) ? spriteWhite : spriteBlack;
        stoneImages[x, y].gameObject.SetActive(true);

        Debug.Log($"���� : ��ġ {x}, {y}�� ���� ����.");
        return true;
    }

    private void YourTurn()
    {
        byte[] data = new byte[2];
        int iSize = tcp.Receive(ref data, data.Length);
        Debug.Log($"iSize = {iSize}");
        if (iSize < 2)
        {
            return;
        }
        int x = data[0];
        int y = data[1];
        Debug.Log($"x : {x}, y : {y} ");
        if (SetStone(x, y, stoneYou))
        {
            if (CheckWin(x, y))
            {
                EndGame(stoneYou);
            }
            else
            {
                SwitchTurn();
            }
        }
    }



    // �ٵ��� ��ư�� ��ġ�Ͽ� Ŭ���̺�Ʈ�� �߻��� �� ����ǵ��� ��
    // ��ġ �� win ���θ� Ȯ��
    public void OnBoardClick()
    {
        if (state != State.Game || stoneTurn != stoneI)
        {
            Debug.Log("���� : ������ �� ���� �ƴմϴ�.");
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardParent, Input.mousePosition, null, out localPoint);

        // ��ȯ�� ���� ��ǥ�� ������ �ε����� ���
        Vector2Int boardPos = LocalPosToBoardIndex(localPoint);
        if (boardPos.x < 0 || boardPos.y < 0)
        {
            Debug.Log("���� : �߸��� ��ġ�� ��ġ �õ�");
            return;
        }
        if (SetStone(boardPos.x, boardPos.y, stoneI))
        {
            // ���� ����
            byte[] data = new byte[2] { (byte)boardPos.x, (byte)boardPos.y };
            Debug.Log($"SendData Length = {data.Length}");
            Debug.Log($"x = {boardPos.x}, y = {boardPos.y}");
            tcp.Send(data, data.Length);

            if (CheckWin(boardPos.x, boardPos.y))
            {
                EndGame(stoneI);
            }
            else
            {
                SwitchTurn();
            }
        }
    }

    // ������ ��ġ�� �� ��ġ�� ���� �̰���� Ȯ��
    private bool CheckWin(int x, int y)
    {
        if (CountStones(x, y, 1, 0) >= winCount) return true;  // ( �� ) ����
        if (CountStones(x, y, 0, 1) >= winCount) return true;  // ( �� ) ����
        if (CountStones(x, y, 1, 1) >= winCount) return true;  // ( \ ) ������
        if (CountStones(x, y, 1, -1) >= winCount) return true;  // ( / ) �����

        return false;
    }

    // Ư�� �������� ���ӵ� ���� �� ���� ������ ����
    private int CountStones(int x, int y, int dx, int dy)
    {
        Stone stoneColor = (Stone)board[x, y];
        int count = 1; // �ڱ� �ڽ�

        // ������ Ž��
        for (int i = 1; i < winCount; i++)
        {
            int nx = x + dx * i; // *�� ����� ����
            int ny = y + dy * i;

            if (IsOutOfBound(nx, ny) || (Stone)board[nx, ny] != stoneColor)
                break;
            count++;
        }
        // ������ Ž��
        for (int i = 1; i < winCount; i++)
        {
            int nx = x - dx * i; // *�� ����� ����
            int ny = y - dy * i;

            if (IsOutOfBound(nx, ny) || (Stone)board[nx, ny] != stoneColor)
                break;
            count++;
        }
        return count;
    }

    private bool IsOutOfBound(int x, int y)
    {
        if (x < 0 || y < 0) return true;
        if (x >= boardSize || y >= boardSize) return true;

        return false;
    }
    private void SwitchTurn()
    {
        if(stoneTurn == Stone.White)
        {
            stoneTurn = Stone.Black;
            turnIndicator.sprite = spriteBlack;
        }
        else
        {
            stoneTurn = Stone.White;
            turnIndicator.sprite = spriteWhite;
        }
        
        Debug.Log($"{stoneTurn}�� ���Դϴ�.");
    }

    // ĵ���� ũ�⿡ ���� ��ǥ �ڵ� �Ҵ� �޼���
    // ȭ�� Ŭ�� ��ǥ�� ������ ���� ��ǥ�� ��ȯ ��, ���� ���� �ε���(x, y)�� ���

    private Vector2Int LocalPosToBoardIndex(Vector2 localPos)
    {
        // �ǹ��� �������� �� ���� ��ǥ�� -> ���� �Ʒ��� (0,0)���� �ϴ� ��ǥ�� ����
        float xAdjusted = localPos.x + boardParent.rect.width * boardParent.pivot.x;
        float yAdjusted = localPos.y + boardParent.rect.height * boardParent.pivot.y;

        int x = Mathf.FloorToInt(xAdjusted / cellSizeX);
        int y = Mathf.FloorToInt(yAdjusted / cellSizeY);

        if (!IsOutOfBound(x, y))
        {
            return new Vector2Int(x, y);
        }
        return new Vector2Int(-1, -1);
    }

    public void ServerStart()
    {
        tcp.StartServer(10000, 10);
        //UIManager.ui_Instance.SetSettingUI(true);
    }
    public void ClientStart()
    {
        tcp.ClientConnect(ip.text, 10000); 
    }

    public void ServerStop()
    {
        if (tcp != null)
        {
            tcp.Disconnect();
        }
        state = State.Start;
        bSetTcp = false;

        stoneI = Stone.None;
        stoneYou = Stone.None;
        stoneTurn = Stone.None;

        ClearBoard();
    }

    public void ClearBoard()
    {
        if (stoneImages != null)
        {
            for (int y = 0; y < stoneImages.GetLength(1); y++)
            {
                for(int x = 0; x < stoneImages.GetLength(0); x++)
                {
                    if (stoneImages[x, y] != null&& stoneImages[x, y].gameObject != null)
                    {
                        Destroy(stoneImages[x, y].gameObject);
                    }
                }
            }
            stoneImages = null;
        }
        board = null;

    }
    // ModeScriptable ������ ����
    private void ApplyModeByBoardSize(int modeSize)
    {
        ModeScriptable mode = Array.Find(gameModes, m=> m.boardSize == modeSize);
        if (mode != null)
        {
            SettingGameMode(mode);
            Debug.Log($"��� ����: {mode.boardSize}");
        }
        else
        {
            Debug.LogError($"��� ID {boardSize}�� ã�� �� �����ϴ�!");
        }
    }

    public void SettingGameMode(ModeScriptable mode)
    {
        currentMode = mode;
        boardSize = mode.boardSize;
        winCount = mode.winCount;
        imageBoard.sprite = mode.spriteBoard;
    }
}
