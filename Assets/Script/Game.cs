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
    // 게임 상태에 관한 enum
    enum State
    {
        Start = 0,
        Game,
        End,
    };

    // 차례에 대한 enum
    enum Turn
    {
        I = 0,
        You,
    };

    // 돌이 black인지 white인지에 관한 enum
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
    public RectTransform boardParent; // GridLayoutGroup과 버튼이 부착된 UI패널

    public GameObject lastStonePrefab; // 마지막 착수

    [Header("Game Settings")]
    [SerializeField] private int boardSize = 3; // 판 크기
    [SerializeField] private int winCount = 3; // 승자 기준

    private Image[,] stoneImages; // 동적 생성 바둑돌 Image 저장
    private int[,] board; // 보드의 논리적 상태 (0: None, 1: White, 2: Black)
    private TCP tcp;
    private State state;
    private Stone stoneTurn;
    private Stone stoneI; // 나의 돌 색
    private Stone stoneYou; // 상대의 돌 색
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
        ApplyModeByBoardSize(boardSize); // 기본으로 세팅
    }

    // 보드 상태 초기화
    void InitializeBoard()
    {
        if (stoneImages != null || board != null)
        {
            Debug.LogWarning("InitializeBoard: 기존 보드가 남아있어 정리합니다");
            ClearBoard();
        }

        // 보드 사이즈만큼의 좌표 배열 만들기
        board = new int[boardSize, boardSize];
        stoneImages = new Image[boardSize, boardSize];


        // boardParent의 크기를 기반으로 한 칸의 크기 계산
        // RectTransform의 크기는 layout에 따라 변할 수 있으므로 한 번 계산
        cellSizeX = boardParent.rect.width / boardSize;
        cellSizeY = boardParent.rect.height / boardSize;
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                // 프리팹 동적 생성 후 부모에 연결
                // stonePrefab 기본 설정 : 이미지 컴포넌트 부착(아직 이미지스프라이트 x), 크기 세팅
                // 설정 값 만큼 바둑판 칸을 동적 생성하고 초기화 해주는 과정
                // 이름하고 동일한 좌표의 2차원 배열로 
                GameObject newStone = Instantiate(stonePrefab, boardParent);
                // 이름 설정을 하지 않을 경우 clone으로 뜨기 때문에 
                // 인스펙터에서 확인하기 편하도록 좌표에 따라 이름 변경
                newStone.name = $"Stone_{x}_{y}";
                Image stoneImg = newStone.GetComponent<Image>();
                RectTransform stoneRect = newStone.GetComponent<RectTransform>();

                // 계산한 한칸의 크기로 Rect 설정
                stoneRect.sizeDelta = new Vector2(cellSizeX, cellSizeY);
                // boardParent의 피벗 위치를 고려한 정확한 위치 계산 후 배열에 좌표값을 할당 해놓음
                float posX = (x * cellSizeX) - (boardParent.rect.width * boardParent.pivot.x) + (cellSizeX / 2f);
                float posY = (y * cellSizeY) - (boardParent.rect.height * boardParent.pivot.y) + (cellSizeY / 2f);
                stoneRect.anchoredPosition = new Vector2(posX, posY);

                stoneImages[x, y] = stoneImg;
                stoneImages[x, y].gameObject.SetActive(false);
                board[x, y] = (int)Stone.None; // 초기화 : 상태 none
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

    // 돌 지정
    private void UpdateStart()
    {
        if (!bSetTcp)
        {
            // 보드 사이즈 설정
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

        // 차후 rand를 이용하여 랜덤으로 수정
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
        // 랜덤 지정
        // 0반환 : 서버가 Black
        // 1반환 : 서버가 White
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

    // 현재의 돌 색깔과 좌표를 넘겨
    // 해당 위치에 돌이 있는지를 확인 한 후 board의 상태를 변경
    private bool SetStone(int x, int y, Stone stone)
    {
        if (board[x, y] != (int)Stone.None)
        {
            Debug.Log($"실패 : 위치 {x}, {y}에 이미 돌이 있음.");
            return false;
        }

        board[x, y] = (int)stone;

        stoneImages[x, y].sprite = (stone == Stone.White) ? spriteWhite : spriteBlack;
        stoneImages[x, y].gameObject.SetActive(true);

        Debug.Log($"성공 : 위치 {x}, {y}에 돌을 놓음.");
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



    // 바둑판 버튼에 설치하여 클릭이벤트가 발생할 때 실행되도록 함
    // 설치 후 win 여부를 확인
    public void OnBoardClick()
    {
        if (state != State.Game || stoneTurn != stoneI)
        {
            Debug.Log("실패 : 지금은 내 턴이 아닙니다.");
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardParent, Input.mousePosition, null, out localPoint);

        // 변환된 로컬 좌표를 가지고 인덱스를 계산
        Vector2Int boardPos = LocalPosToBoardIndex(localPoint);
        if (boardPos.x < 0 || boardPos.y < 0)
        {
            Debug.Log("실패 : 잘못된 위치에 설치 시도");
            return;
        }
        if (SetStone(boardPos.x, boardPos.y, stoneI))
        {
            // 정보 전송
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

    // 직전에 설치한 돌 위치에 대해 이겼는지 확인
    private bool CheckWin(int x, int y)
    {
        if (CountStones(x, y, 1, 0) >= winCount) return true;  // ( ㅡ ) 가로
        if (CountStones(x, y, 0, 1) >= winCount) return true;  // ( ㅣ ) 세로
        if (CountStones(x, y, 1, 1) >= winCount) return true;  // ( \ ) 우하향
        if (CountStones(x, y, 1, -1) >= winCount) return true;  // ( / ) 우상향

        return false;
    }

    // 특정 방향으로 연속된 같은 색 돌의 개수를 센다
    private int CountStones(int x, int y, int dx, int dy)
    {
        Stone stoneColor = (Stone)board[x, y];
        int count = 1; // 자기 자신

        // 정방향 탐색
        for (int i = 1; i < winCount; i++)
        {
            int nx = x + dx * i; // *를 해줘야 방향
            int ny = y + dy * i;

            if (IsOutOfBound(nx, ny) || (Stone)board[nx, ny] != stoneColor)
                break;
            count++;
        }
        // 역방향 탐색
        for (int i = 1; i < winCount; i++)
        {
            int nx = x - dx * i; // *를 해줘야 방향
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
        
        Debug.Log($"{stoneTurn}의 턴입니다.");
    }

    // 캔버스 크기에 따라 좌표 자동 할당 메서드
    // 화면 클릭 좌표를 보드의 로컬 좌표로 변환 후, 최종 보드 인덱스(x, y)로 계산

    private Vector2Int LocalPosToBoardIndex(Vector2 localPos)
    {
        // 피벗을 기준으로 한 로컬 좌표를 -> 왼쪽 아래를 (0,0)으로 하는 좌표로 보정
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
    // ModeScriptable 값으로 지정
    private void ApplyModeByBoardSize(int modeSize)
    {
        ModeScriptable mode = Array.Find(gameModes, m=> m.boardSize == modeSize);
        if (mode != null)
        {
            SettingGameMode(mode);
            Debug.Log($"모드 적용: {mode.boardSize}");
        }
        else
        {
            Debug.LogError($"모드 ID {boardSize}를 찾을 수 없습니다!");
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
