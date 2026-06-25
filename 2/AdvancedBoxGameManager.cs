using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 定義拆箱/解謎可用的工具種類
public enum ToolType
{
    LeftOpen,
    RightOpen,
    RemoveTape,
    Flip
}

// 儲存每個步驟需要的資料結構
[System.Serializable]
public struct StepData
{
    public string stepName;         // 步驟名稱，方便在編輯器中辨識
    public Sprite boxSprite;        // 該步驟要顯示的圖片
    public ToolType correctTool;    // 該步驟必須使用的正確工具
}

public class AdvancedBoxGameManager : MonoBehaviour
{
    [Header("Steps")]
    public List<StepData> steps;    // 在 Inspector 中設定的遊戲步驟清單

    [Header("UI")]
    public GameObject gameUI;       // 遊玩中的主介面
    public Image boxImage;          // 顯示目前進度圖片的 UI 元件
    public GameObject finishUI;     // 完成遊戲後顯示的結算或提示介面

    [Header("State")]
    private int currentStep = 0;    // 追蹤目前的步驟進度
    private bool isPlaying = false; // 判斷小遊戲是否正在進行中
    private bool playerInRange = false; // 判斷玩家是否在可互動的範圍內

    void Update()
    {
        // 只有當玩家在範圍內，且按下鍵盤 F 鍵時，才觸發遊戲開關
        if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleGame();
        }
    }

    // 控制遊戲介面的開啟與關閉
    void ToggleGame()
    {
        isPlaying = !isPlaying;

        if (isPlaying)
        {
            // 開啟遊戲時的初始化設定
            if (gameUI != null) gameUI.SetActive(true);
            if (finishUI != null) finishUI.SetActive(false); // 確保完成介面被關閉，避免重疊
            currentStep = 0; // 重置進度
            RefreshStep();
        }
        else
        {
            // 關閉遊戲
            CloseGameUI();
        }
    }

    // 刷新當前步驟的畫面
    void RefreshStep()
    {
        // 防呆機制：確保步驟清單不是空的，避免報錯
        if (steps == null || steps.Count == 0) return;

        // 如果目前步驟已經超過清單總數，代表遊戲完成
        if (currentStep >= steps.Count)
        {
            EndGame();
            return;
        }

        // 更新 UI 上顯示的圖片為當前步驟的圖片
        if (boxImage != null)
        {
            boxImage.sprite = steps[currentStep].boxSprite;
        }
    }

    // 供 UI 工具按鈕呼叫的方法 (傳入 0:LeftOpen, 1:RightOpen, 2:RemoveTape, 3:Flip)
    public void PressTool(int toolIndex)
    {
        // 如果遊戲沒在進行中，不執行任何動作
        if (!isPlaying) return;

        // 防呆機制：檢查傳入的數字是否真的存在於 ToolType 列舉中
        if (!System.Enum.IsDefined(typeof(ToolType), toolIndex))
        {
            Debug.LogWarning("傳入了無效的工具索引值");
            return;
        }

        ToolType selected = (ToolType)toolIndex;

        // 比對玩家選擇的工具是否等於當前步驟所要求的正確工具
        if (selected == steps[currentStep].correctTool)
        {
            NextStep(); // 答對了，進入下一步
        }
        else
        {
            Debug.Log("使用了錯誤的工具");
        }
    }

    // 進入下一個步驟
    void NextStep()
    {
        currentStep++;

        if (currentStep >= steps.Count)
        {
            EndGame(); // 所有步驟完成
        }
        else
        {
            RefreshStep(); // 尚未完成，刷新畫面顯示下一張圖
        }
    }

    // 遊戲完成的處理邏輯
    void EndGame()
    {
        isPlaying = false;
        if (gameUI != null) gameUI.SetActive(false);
        if (finishUI != null) finishUI.SetActive(true);

        Debug.Log("任務完成！");
    }

    // 統一關閉所有相關 UI 的方法 
    // 修改為 public，讓右上角的叉叉按鈕可以呼叫這個方法
    public void CloseGameUI()
    {
        isPlaying = false;
        if (gameUI != null) gameUI.SetActive(false);
        if (finishUI != null) finishUI.SetActive(false);
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