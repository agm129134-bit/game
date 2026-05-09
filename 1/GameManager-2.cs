using UnityEngine;
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode;

// 🌟 【修改】將 MonoBehaviour 改為 NetworkBehaviour
public class GameManager : NetworkBehaviour
{
    // 🌟 單例模式：在連線遊戲中依然適用，方便其他腳本呼叫
    public static GameManager Instance { get; private set; }

    [Header("👥 玩家資料設定")]
    [Tooltip("總玩家人數，確保每台電腦看到的上限一致")]
    public int playerCount = 4;       
    public int maxLivesPerPlayer = 3; 
    
    // ==========================================
    // 🌐 【NGO 核心修改】將普通陣列改為 NetworkList，讓網路自動同步
    // 這樣伺服器一改分數，所有客戶端都會立刻更新！
    // ==========================================
    public NetworkList<int> playerScores; 
    public NetworkList<int> playerLives;  

    [Header("🏆 勝利條件設定")]
    public int targetTrashCount = 60; 
    public int tasksPerPlayer = 3;

    // ==========================================
    // 🌐 【NGO 核心修改】將重要的進度變數改為 NetworkVariable
    // 只有伺服器有權限修改 (Server 寫入)，所有人都可以讀取 (Client 讀取)
    // ==========================================
    [Header("📊 當前進度 (網路同步)")]
    public NetworkVariable<int> currentCollectedTrash = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> alivePlayerCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> remainingTrash = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> completedTasks = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int totalTrashOnMap = 60;  
    public int totalTasks = 12;      

    // 💡 魔法屬性：根據同步的活著人數，自動算出目標
    public int CurrentTargetTasks 
    { 
        get { return alivePlayerCount.Value * tasksPerPlayer; } 
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 初始化 NetworkList，必須在 Awake 執行
        playerScores = new NetworkList<int>();
        playerLives = new NetworkList<int>();
    }

    // 🌟 【NGO 新增】當網路物件生成時呼叫，這是初始化網路變數的最佳時機
    public override void OnNetworkSpawn()
    {
        // 只有伺服器有權限初始化分數和生命值
        if (IsServer)
        {
            playerScores.Clear();
            playerLives.Clear();

            for (int i = 0; i < playerCount; i++)
            {
                playerScores.Add(0);                  // 分數歸零
                playerLives.Add(maxLivesPerPlayer);   // 滿血復活
            }
            
            alivePlayerCount.Value = playerCount; 
            currentCollectedTrash.Value = 0;
            remainingTrash.Value = totalTrashOnMap;
            completedTasks.Value = 0;
            isGameOver.Value = false;
        }

        // 🌟 註冊網路變數的改變事件（當生命值改變時，呼叫更新 UI）
        playerLives.OnListChanged += OnPlayerLivesChanged;
    }

    public override void OnNetworkDespawn()
    {
        // 養成好習慣，銷毀時取消註冊事件
        playerLives.OnListChanged -= OnPlayerLivesChanged;
    }

    // ==========================================
    // 垃圾分類與任務打卡 (這些動作任何人都能觸發，所以用 ServerRpc 丟給伺服器處理)
    // ==========================================

    public void OnTrashBagSorted(int playerId = 0)
    {
        if (isGameOver.Value) return;
        Debug.Log("🗑️ 觸發分類成功！通知伺服器加分！");
        AddScoreServerRpc(playerId, 1); 
    }

    public void CompleteOneTask()
    {
        if (isGameOver.Value) return;
        CompleteTaskServerRpc();
    }

    // 🌟 【ServerRpc】任何人完成任務，都會通知伺服器來 +1
    [ServerRpc(RequireOwnership = false)]
    private void CompleteTaskServerRpc()
    {
        if (isGameOver.Value) return;
        
        completedTasks.Value++;
        Debug.Log($"任務完成打卡！目前任務進度：{completedTasks.Value} / {CurrentTargetTasks}");
        
        CheckWinCondition();
    }

    // ==========================================
    // 🗑️ 玩家撿起垃圾 (同步版)
    // ==========================================
    public void AddScore(int playerId, int amount = 1)
    {
        if (isGameOver.Value) return;
        AddScoreServerRpc(playerId, amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddScoreServerRpc(int playerId, int amount)
    {
        if (isGameOver.Value) return;
        if (playerId < 0 || playerId >= playerCount) return;

        playerScores[playerId] += amount; 
        currentCollectedTrash.Value += amount;  
        remainingTrash.Value -= amount;         

        Debug.Log($"撿到垃圾！目前垃圾進度：{currentCollectedTrash.Value} / {targetTrashCount}");
        CheckWinCondition();
    }

    // ==========================================
    // 🌟 中央大腦判斷勝利條件 (只有伺服器會做判斷)
    // ==========================================
    private void CheckWinCondition()
    {
        if (!IsServer || isGameOver.Value) return;

        if (currentCollectedTrash.Value >= targetTrashCount && completedTasks.Value >= CurrentTargetTasks)
        {
            TriggerGameOverServerRpc(true, $"清湖成功！\n垃圾: {currentCollectedTrash.Value}/{targetTrashCount} | 任務: {completedTasks.Value}/{CurrentTargetTasks}");
        }
    }

    // ==========================================
    // 🐟 玩家被大魚咬 (同步版)
    // ==========================================
    public void TakeDamage(int playerId)
    {
        if (isGameOver.Value) return;
        TakeDamageServerRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int playerId)
    {
        if (isGameOver.Value) return;
        if (playerId < 0 || playerId >= playerCount) return;

        if (playerLives[playerId] > 0)
        {
            playerLives[playerId]--; 
            // 注意：UI 更新已經移到了 OnPlayerLivesChanged 事件中處理

            if (playerLives[playerId] == 0)
            {
                alivePlayerCount.Value--; 
                Debug.Log($"💀 玩家 {playerId + 1} 陣亡！目標任務數降為：{CurrentTargetTasks}");
                
                // 通知所有人該玩家已死亡 (更新 UI 頭像等)
                NotifyPlayerDeathClientRpc(playerId);
            }
        }

        if (alivePlayerCount.Value <= 0)
        {
            TriggerGameOverServerRpc(false, "所有玩家都陣亡了！");
        }
        else
        {
            CheckWinCondition();
        }
    }

    // 當伺服器更改了任何玩家的生命值，所有客戶端都會收到這個事件通知來更新 UI
    private void OnPlayerLivesChanged(NetworkListEvent<int> changeEvent)
    {
        int playerId = changeEvent.Index;
        int newLives = changeEvent.Value;

        if (HealthUIManager.Instance != null)
        {
            HealthUIManager.Instance.UpdateHealth(playerId, newLives);
        }
    }

    [ClientRpc]
    private void NotifyPlayerDeathClientRpc(int playerId)
    {
        // 所有人收到死亡通知後，更新任務清單的頭像
        if (TaskListManager.Instance != null)
        {
            TaskListManager.Instance.MarkPlayerDead(playerId);
        }
    }

    // ==========================================
    // 🛑 遊戲結束判定總機 (由伺服器發起，廣播給所有人)
    // ==========================================
    [ServerRpc(RequireOwnership = false)]
    public void TriggerGameOverServerRpc(bool isWin, string reason)
    {
        if (isGameOver.Value) return;
        isGameOver.Value = true;
        
        TriggerGameOverClientRpc(isWin, reason);
    }

    [ClientRpc]
    private void TriggerGameOverClientRpc(bool isWin, string reason)
    {
        Debug.Log($"<color=orange>【遊戲結束】狀態：{(isWin ? "成功" : "失敗")} | 原因：{reason}</color>");

        if (SummaryUIManager.Instance != null)
        {
            SummaryUIManager.Instance.ShowSummary(isWin);
        }
    }
}