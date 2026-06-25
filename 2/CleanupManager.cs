using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 引入新版輸入系統

public class CleanupManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject gameUI;       // 修馬桶小遊戲的主介面
    public GameObject winEffect;    // 修理完成後的勝利畫面或特效

    [Header("State")]
    private bool isPlaying = false;     // 判斷小遊戲是否正在進行中
    private bool playerInRange = false; // 判斷玩家是否在可互動的範圍內

    // 場景中所有 FixableObject 的清單
    private List<FixableObject> targets = new List<FixableObject>();

    void Start()
    {
        // 遊戲開始時，自動找出所有掛有 FixableObject 腳本的物件並加入清單
        FixableObject[] allObjects = Object.FindObjectsByType<FixableObject>(FindObjectsSortMode.None);
        targets.AddRange(allObjects);

        Debug.Log("場景中共有 " + targets.Count + " 個物件需要清理。");
    }

    void Update()
    {
        // 偵測玩家是否在範圍內，且按下鍵盤 F 鍵時觸發遊戲開關
        if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleGame();
        }

        // 只有在遊戲進行中，才持續檢查是否全部清理完了
        if (isPlaying)
        {
            CheckIfAllCleaned();
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
        }
        else
        {
            // 關閉遊戲
            CloseGameUI();
        }
    }

    // 檢查清單中的物件是否都已經被清理完畢
    void CheckIfAllCleaned()
    {
        // 如果勝利畫面已經顯示，不需要重複檢查
        if (winEffect != null && winEffect.activeSelf) return;

        // 倒序檢查清單中的物件是否都已經不活躍了 (倒序迴圈在移除清單項目時才不會報錯)
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            // 如果物件不存在了(被銷毀) 或 已經被關閉了(隱藏)
            if (targets[i] == null || !targets[i].gameObject.activeInHierarchy)
            {
                targets.RemoveAt(i); // 從清單移除
            }
        }

        // 如果清單數量歸零，代表都清理完了，觸發勝利
        if (targets.Count == 0)
        {
            TriggerWinEffect();
        }
    }

    // 觸發勝利畫面與後續處理
    void TriggerWinEffect()
    {
        Debug.Log("全部清理完成！彈出效果。");
        isPlaying = false; // 停止遊戲狀態

        if (gameUI != null) gameUI.SetActive(false); // 隱藏遊戲主介面
        if (winEffect != null) winEffect.SetActive(true); // 顯示勝利畫面
    }

    // 供 UI 右上角叉叉按鈕呼叫的公開方法
    public void CloseGameUI()
    {
        isPlaying = false;
        if (gameUI != null) gameUI.SetActive(false);
        if (winEffect != null) winEffect.SetActive(false);
    }

    // Unity 內建的物理碰撞觸發事件：玩家進入範圍
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    // Unity 內建的物理碰撞觸發事件：玩家離開範圍
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            CloseGameUI(); // 玩家走遠時，強制關閉介面
        }
    }
}