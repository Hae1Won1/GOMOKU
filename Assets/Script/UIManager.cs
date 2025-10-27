using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager ui_Instance { get; private set; }

    public GameObject startUI;
    public GameObject gameUI;
    public GameObject endUI;
    public GameObject startSettingUI;
    public GameObject clientSettingUI;

    public Sprite imageLose;
    public Sprite imageWin;
    public Image resaultImage;
    public TextMeshProUGUI resualtTimeTMP;
    public TextMeshProUGUI timeTMP;

    public bool bGame = false;
    private float timer = 0f;
    private int min = 0;
    private int sec = 0;


    private void Awake()
    {
        if (ui_Instance == null)
        {
            ui_Instance = this;
            DontDestroyOnLoad(gameObject); // ¾ÀÀÌ ¹Ù²î¾îµµ ÆÄ±«µÇÁö ¾Êµµ·Ï
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        UIAllOff();
        startUI.SetActive(true);
    }

    public void Update()
    {
        if (bGame)
        {
            timer += Time.deltaTime;
            if(timer >= 60f)
            {
                min += 1;
                timer -= 60f;
            }
            timeTMP.text = min + ":" + (int)timer;
        }
    }

    public void SetStartUI()
    {
        UIAllOff();
        startUI.SetActive(true);
    }

    public void SetSettingUI(bool isServer)
    {
        if(isServer) startSettingUI.SetActive(true);
    }

    public void SetGameUI()
    {
        timer = 0f;
        min = 0;
        UIAllOff();
        bGame = true;
        gameUI.SetActive(true);
    }

    public void SetEndUI(bool win)
    {
        bGame = false;
        endUI.SetActive(true);
        if (win)
        {
            resaultImage.sprite = imageWin;
        }
        else
        {
            resaultImage.sprite = imageLose;
        }
        resualtTimeTMP.text = "Play Time : " + min + "m " + (int)timer + "s";
    }

    private void UIAllOff()
    {
        startUI.SetActive(false);
        endUI.SetActive(false);
        gameUI.SetActive(false);
        startSettingUI.SetActive(false);
        clientSettingUI.SetActive(false);
    }

    public void UIOn(GameObject o)
    {
        o.SetActive(true);
    }
    public void UIOff(GameObject o)
    {
        o.SetActive(false);
    }
}


