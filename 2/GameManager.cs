using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // 必須引入新版輸入系統

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum Tool { Glove, Scissors }
    
    [Header("目前選用的工具")]
    public Tool currentTool = Tool.Glove;

    [Header("UI 設定")]
    public GameObject gameUI;       // 小遊戲的主介面
    public GameObject winEffect;    // 通關後的勝利畫面或特效

    [Header("遊戲統計")]
    public int score = 0;
    private int clearedCount = 0; // 玩家已點掉的數量
    public int targetCount = 20;  // 通關目標
    public TextMeshProUGUI scoreText;

    [Header("音效設定")]
    public AudioSource audioSource; 
    public AudioClip weedSound;     
    public AudioClip finishSound;   

    [Header("游標設定")]
    public Texture2D gloveCursor;    
    public Texture2D scissorsCursor; 
    public Vector2 hotSpot = new Vector2(32, 32); 

    // 狀態管理
    private Camera mainCamera;
    private bool isPlaying = false;

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        mainCamera = Camera.main;
        UpdateUI();
    }

    void Update()
    {
        // 只有在遊戲開啟狀態下才允許點擊偵測
        if (!isPlaying) return;

        // 防呆機制：確保滑鼠與攝影機存在
        if (Mouse.current == null || mainCamera == null) return;

        // 點擊偵測：使用新版輸入系統偵測滑鼠左鍵點擊瞬間
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 將螢幕座標轉換為 2D 世界座標
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            if (hit != null)
            {
                HandleClick(hit.gameObject);
            }
        }
    }

    // 處理點擊目標的邏輯
    void HandleClick(GameObject target)
    {
        // 判斷工具與標籤是否匹配：手套拔草、剪刀剪樹枝
        if (target.CompareTag("Weed") && currentTool == Tool.Glove)
        {
            ProcessElimination(target);
        }
        else if (target.CompareTag("Branch") && currentTool == Tool.Scissors)
        {
            ProcessElimination(target);
        }
    }

    // 執行消除物件、計分與檢查通關
    void ProcessElimination(GameObject target)
    {
        PlaySfx(weedSound);
        Destroy(target);
        score += 10;
        clearedCount++;
        UpdateUI();

        // 檢查是否達成通關目標數量
        if (clearedCount >= targetCount)
        {
            PlaySfx(finishSound);
            Debug.Log("通關！彈出結算畫面...");
            // 延遲1秒關閉遊戲畫面並顯示勝利，讓通關音效有時間播完
            Invoke("TriggerWinEffect", 1.0f); 
        }
    }

    // 播放音效的共用方法
    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // 更新分數介面
    void UpdateUI()
    {
        if (scoreText != null) 
        {
            scoreText.text = "Score: " + score;
        }
    }

    // 更新滑鼠游標圖案
    void UpdateCursor()
    {
        Texture2D activeTexture = (currentTool == Tool.Glove) ? gloveCursor : scissorsCursor;
        if (activeTexture != null)
        {
            Cursor.SetCursor(activeTexture, hotSpot, CursorMode.Auto);
        }
    }

    // 恢復預設的系統滑鼠游標
    void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 供 UI 按鈕呼叫：切換為手套
    public void SelectGlove() 
    { 
        currentTool = Tool.Glove; 
        UpdateCursor();
        Debug.Log("已切換為：手套");
    }

    // 供 UI 按鈕呼叫：切換為剪刀
    public void SelectScissors() 
    { 
        currentTool = Tool.Scissors; 
        UpdateCursor();
        Debug.Log("已切換為：剪刀");
    }

    // 供外部觸發開啟遊戲 (例如玩家靠近按下 F 鍵時可呼叫此方法)
    public void OpenGameUI()
    {
        isPlaying = true;
        if (gameUI != null) gameUI.SetActive(true);
        if (winEffect != null) winEffect.SetActive(false);
        UpdateCursor(); // 開啟遊戲時換成工具游標
    }

    // 供 UI 右上角叉叉按鈕呼叫的公開方法
    public void CloseGameUI()
    {
        isPlaying = false;
        if (gameUI != null) gameUI.SetActive(false);
        if (winEffect != null) winEffect.SetActive(false);
        ResetCursor(); // 關閉遊戲時恢復普通滑鼠指標
    }

    // 觸發勝利畫面
    void TriggerWinEffect()
    {
        isPlaying = false;
        if (gameUI != null) gameUI.SetActive(false);
        if (winEffect != null) winEffect.SetActive(true);
        ResetCursor(); // 通關後恢復普通滑鼠指標
    }
}