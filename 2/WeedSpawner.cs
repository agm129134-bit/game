using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 引入新版輸入系統

public class WeedSpawner : MonoBehaviour
{
    [Header("UI 設定")]
    public GameObject gameUI;       // 除草小遊戲的主介面
    public GameObject winEffect;    // 除草完成後的勝利畫面或特效

    [Header("雜草生成設定")]
    public GameObject[] weedPrefabs; // 雜草的預製體清單
    public float spawnRate = 2f;     // 每隔幾秒生成一株
    public Vector2 spawnRangeX = new Vector2(-5f, 5f); // X 軸生成範圍
    public Vector2 spawnRangeY = new Vector2(-3f, 2f); // Y 軸生成範圍
    public int maxSpawn = 20;        // 最大生成總數量

    [Header("狀態追蹤")]
    private int spawnedCount = 0;       // 記錄目前已經生成的數量
    private float spawnTimer = 0f;      // 生成計時器
    private bool isPlaying = false;     // 判斷小遊戲是否正在進行中
    private bool playerInRange = false; // 判斷玩家是否在可互動的範圍內

    // 追蹤場上還活著的雜草，用來判定是否全部清空
    private List<GameObject> activeWeeds = new List<GameObject>();

    void Update()
    {
        // 偵測玩家是否在範圍內，且按下鍵盤 F 鍵時觸發遊戲開關
        if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleGame();
        }

        // 只有在遊戲進行中，才執行雜草生成與過關檢查
        if (isPlaying)
        {
            HandleSpawning();
            CheckWinCondition();
        }
    }

    // 控制遊戲介面的開啟與關閉
    void ToggleGame()
    {
        isPlaying = !isPlaying;

        if (isPlaying)
        {
            // 開啟遊戲時的顯示設定
            if (gameUI != null) gameUI.SetActive(true);
            if (winEffect != null) winEffect.SetActive(false);

            // 修正：同步開啟 GameManager 的點擊偵測與工具游標轉換
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenGameUI();
            }
        }
        else
        {
            // 關閉遊戲
            CloseGameUI();
        }
    }

    // 處理雜草生成的計時與邏輯
    void HandleSpawning()
    {
        if (spawnedCount >= maxSpawn) return;

        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= spawnRate)
        {
            spawnTimer = 0f; 
            SpawnWeed();
        }
    }

    // 執行生成單一雜草的動作
    void SpawnWeed()
    {
        if (weedPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, weedPrefabs.Length);
        Vector3 randomPos = new Vector3(
            Random.Range(spawnRangeX.x, spawnRangeX.y),
            Random.Range(spawnRangeY.x, spawnRangeY.y),
            0
        );

        GameObject newWeed = Instantiate(weedPrefabs[randomIndex], randomPos, Quaternion.identity);
        activeWeeds.Add(newWeed);
        
        spawnedCount++; 
    }

    // 檢查是否達成除草過關條件
    void CheckWinCondition()
    {
        if (winEffect != null && winEffect.activeSelf) return;

        for (int i = activeWeeds.Count - 1; i >= 0; i--)
        {
            if (activeWeeds[i] == null || !activeWeeds[i].activeInHierarchy)
            {
                activeWeeds.RemoveAt(i); 
            }
        }

        if (spawnedCount >= maxSpawn && activeWeeds.Count == 0)
        {
            TriggerWinEffect();
        }
    }

    // 觸發勝利畫面與後續處理
    void TriggerWinEffect()
    {
        Debug.Log("除草任務完成！彈出結算畫面。");
        isPlaying = false; 

        if (gameUI != null) gameUI.SetActive(false); 
        if (winEffect != null) winEffect.SetActive(true); 

        // 修正：通關時強制恢復普通系統滑鼠指標，避免過關後游標樣式卡住
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 供 UI 右上角叉叉按鈕呼叫的公開方法
    public void CloseGameUI()
    {
        isPlaying = false;
        if (gameUI != null) gameUI.SetActive(false);
        if (winEffect != null) winEffect.SetActive(false);

        // 修正：關閉小遊戲時，自動通知 GameManager 停止運作並還原普通滑鼠指標
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseGameUI();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            CloseGameUI(); 
        }
    }
}