using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode; 

// 🌟 【修改】將 MonoBehaviour 改為 NetworkBehaviour，這樣腳本才能使用網路功能
public class SummaryUIManager : NetworkBehaviour
{
    public static SummaryUIManager Instance { get; private set; }

    [Header("🛠️ 開發測試設定")]
    [Tooltip("打勾：開啟 Y 鍵(勝利)與 N 鍵(失敗)的測試功能 / 取消打勾：關閉測試按鍵")]
    public bool enableTestKeys = true;

    [System.Serializable]
    public struct PlayerProfile
    {
        public string playerName;
        public Sprite playerAvatar;
    }

    [Header("👥 玩家圖文資料庫 (請依 ID 順序填寫 1P~4P)")]
    public PlayerProfile[] playerProfiles;

    [Header("🎮 遊戲進行中 UI (結算時會自動隱藏)")]
    public GameObject inGameTimerUI;      

    [Header("🛑 結算時強制關閉的視窗 (把小遊戲介面拖進來)")]
    public GameObject[] panelsToCloseOnSummary;

    [Header("🎬 階段一：5秒全螢幕提示")]
    public GameObject phase1_Panel;       
    public GameObject successBigImage;    
    public GameObject failBigImage;       
    public float showTime = 5f;           

    [Header("📰 階段二：大魚快報 (滑動面板)")]
    public RectTransform newspaperPanel;  
    public GameObject successNewsGroup;   
    public GameObject failNewsGroup;      

    [Header("🏆 成功版 UI 元件綁定")]
    public Image bestPlayerAvatar;
    public Text bestPlayerName;
    public Text bestPlayerScore;
    public Image lazyPlayerAvatar;
    public Text lazyPlayerName;
    public Text lazyPlayerScore;

    [Header("💀 失敗版 UI 元件綁定")]
    public Text remainingTrashText;
    public Text failedPlayersText;

    [Header("動畫設定")]
    public Vector2 newspaperStartPos = new Vector2(0, -1500f); 
    public Vector2 newspaperEndPos = new Vector2(0, 0);        
    public float slideDuration = 1f;                           

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (phase1_Panel != null) phase1_Panel.SetActive(false);
        if (newspaperPanel != null) newspaperPanel.anchoredPosition = newspaperStartPos;
    }

    void Update()
    {
        // 🌟 【NGO 保護】確保網路已經啟動，否則不執行
        if (!IsSpawned) return;

        // 🌟 【NGO 修改】為了防止一般玩家亂按測試鍵結束遊戲，我們限制「只有伺服器(主機)」可以觸發測試
        if (enableTestKeys && IsServer)
        {
            if (Input.GetKeyDown(KeyCode.Y)) 
            {
                // 呼叫 ServerRpc，告訴伺服器「遊戲勝利了，請廣播給所有人」
                TriggerSummaryServerRpc(true); 
            }
            if (Input.GetKeyDown(KeyCode.N)) 
            {
                TriggerSummaryServerRpc(false); 
            }
        }
    }

    // ==========================================
    // 🌐 網路同步邏輯區 (NGO 核心)
    // ==========================================

    /// <summary>
    /// 其他腳本 (例如 GameManager) 要呼叫結算畫面時，請呼叫這個函式。
    /// 無論是誰呼叫，都會向伺服器發出請求。
    /// </summary>
    public void ShowSummary(bool isWin)
    {
        // RequireOwnership = false 允許任何人呼叫這個 RPC，但我們會在這裡確保它傳給伺服器
        TriggerSummaryServerRpc(isWin);
    }

    // 🌟 【ServerRpc】這段程式碼「只會在伺服器端」執行。
    // 客戶端呼叫它時，會把要求傳給伺服器。
    [ServerRpc(RequireOwnership = false)]
    private void TriggerSummaryServerRpc(bool isWin)
    {
        // 伺服器收到請求後，立刻廣播給「所有連線的玩家」
        ShowSummaryClientRpc(isWin);
    }

    // 🌟 【ClientRpc】這段程式碼是伺服器下達的命令，所有玩家 (包含主機) 都會同時執行這段程式碼！
    [ClientRpc]
    private void ShowSummaryClientRpc(bool isWin)
    {
        // 大家一起暫停時間 (Time.timeScale 是本地生效的，所以要用 ClientRpc 讓大家一起暫停)
        Time.timeScale = 0f;
        
        // 大家一起播放結算動畫
        StartCoroutine(SummarySequence(isWin));
    }

    // ==========================================
    // 🎨 UI 演出邏輯區 (完全保留你的原本寫法)
    // ==========================================

    private IEnumerator SummarySequence(bool isWin)
    {
        if (inGameTimerUI != null) inGameTimerUI.SetActive(false);

        if (panelsToCloseOnSummary != null)
        {
            foreach (GameObject panel in panelsToCloseOnSummary)
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        // 注意：這裡依賴 GameManager.Instance，請確保你的 GameManager 也已經改成 NGO 並同步好分數了！
        PrepareData(isWin);

        if (phase1_Panel != null) phase1_Panel.SetActive(true);
        if (successBigImage != null) successBigImage.SetActive(isWin);
        if (failBigImage != null) failBigImage.SetActive(!isWin);
        
        yield return new WaitForSecondsRealtime(showTime);

        if (phase1_Panel != null) phase1_Panel.SetActive(false);

        if (successNewsGroup != null) successNewsGroup.SetActive(isWin);
        if (failNewsGroup != null) failNewsGroup.SetActive(!isWin);
        
        if (newspaperPanel != null)
        {
            float timer = 0f;
            while (timer < slideDuration)
            {
                timer += Time.unscaledDeltaTime; 
                
                float progress = timer / slideDuration;
                float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f); 
                
                newspaperPanel.anchoredPosition = Vector2.Lerp(newspaperStartPos, newspaperEndPos, smoothProgress);
                yield return null;
            }
            
            newspaperPanel.anchoredPosition = newspaperEndPos;
        }
    }

    private void PrepareData(bool isWin)
    {
        if (GameManager.Instance == null) return;

        if (isWin)
        {
            int bestIndex = 0;
            int lazyIndex = 0;
            int maxScore = -1;
            int minScore = 9999;

            for (int i = 0; i < GameManager.Instance.playerCount; i++)
            {
                int score = GameManager.Instance.playerScores[i];
                if (score > maxScore) { maxScore = score; bestIndex = i; }
                if (score < minScore) { minScore = score; lazyIndex = i; }
            }

            if (playerProfiles.Length > bestIndex)
            {
                if (bestPlayerName != null) bestPlayerName.text = playerProfiles[bestIndex].playerName;
                if (bestPlayerAvatar != null) bestPlayerAvatar.sprite = playerProfiles[bestIndex].playerAvatar;
                if (bestPlayerScore != null) bestPlayerScore.text = $"撿了 {maxScore} 個垃圾!!";
            }
            
            if (playerProfiles.Length > lazyIndex)
            {
                if (lazyPlayerName != null) lazyPlayerName.text = playerProfiles[lazyIndex].playerName;
                if (lazyPlayerAvatar != null) lazyPlayerAvatar.sprite = playerProfiles[lazyIndex].playerAvatar;
                if (lazyPlayerScore != null) lazyPlayerScore.text = $"只撿 {minScore} 個垃圾...";
            }
        }
        else
        {
            if (remainingTrashText != null) remainingTrashText.text = GameManager.Instance.remainingTrash.ToString() + " 個";
            
            int uncompletedTasks = GameManager.Instance.totalTasks - GameManager.Instance.completedTasks;
            if (uncompletedTasks < 0) uncompletedTasks = 0; 
            
            if (failedPlayersText != null) failedPlayersText.text = $"{uncompletedTasks} 個任務未完成...";
        }
    }

    // ==========================================
    // 🔄 場景切換邏輯區 (NGO 同步切換)
    // ==========================================

    public void Button_RestartGame()
    {
        // 點擊按鈕後，呼叫伺服器去執行場景切換
        RestartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartGameServerRpc()
    {
        // 🌟 【NGO 場景切換】
        // 在連線遊戲中，不能用原生的 SceneManager，必須呼叫 NetworkManager 帶領所有玩家一起換場景！
        Time.timeScale = 1f; 
        NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void Button_MainMenu()
    {
        MainMenuServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void MainMenuServerRpc()
    {
        Time.timeScale = 1f; 
        // 帶領所有連線玩家一起回到主選單
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}