using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance {  get; private set; }

    [SerializeField] private AudioSource sfxSource; // È¿°úÀ½ ÀüÄO
    [SerializeField] private AudioSource bgmSource; // ¹è°æÀ½¾Ç


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public enum GameSound
    {

    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
