using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; 
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode;

// 🌟 【修改】改為繼承 NetworkBehaviour
public class TaskListManager : NetworkBehaviour
{
    public static TaskListManager Instance { get; private set; }

    [Header("UI 總開關設定")]
    public GameObject taskListUI;
    public bool isOpenAtStart = false;

    [Header("👥 玩家陣亡狀態設定")]
    public GameObject[] deadCrosses;

    [Header("🎬 動畫設定")]
    [Tooltip("開關動畫需要幾秒鐘？(預設 0.4秒)")]
    public float slideDuration = 0.4f;
    [Tooltip("關閉時要往左邊退到多遠？(數值越大退越遠，預設 500)")]
    public float slideOffset = 500f;

    private RectTransform uiRectTransform;
    private Vector2 shownPosition;  
    private Vector2 hiddenPosition; 
    private Coroutine currentSlideCoroutine;

    [System.Serializable]
    public struct TaskUI
    {
        public string taskName;        
        public Text taskText;          
        public GameObject checkMark;   
        public GameObject strikeLine;  
    }

    [Header("📝 任務進度設定")]
    public TaskUI[] tasks;

    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        foreach (var cross in deadCrosses)
        {
            if (cross != null) cross.SetActive(false);
        }

        foreach (var task in tasks)
        {
            if (task.checkMark != null) task.checkMark.SetActive(false);
            if (task.strikeLine != null) task.strikeLine.SetActive(false);
        }

        if (taskListUI != null)
        {
            uiRectTransform = taskListUI.GetComponent<RectTransform>();
            
            shownPosition = uiRectTransform.anchoredPosition;
            hiddenPosition = new Vector2(shownPosition.x - slideOffset, shownPosition.y);

            isShowing = isOpenAtStart;
            
            if (isShowing)
            {
                uiRectTransform.anchoredPosition = shownPosition;
                taskListUI.SetActive(true);
            }
            else
            {
                uiRectTransform.anchoredPosition = hiddenPosition;
                taskListUI.SetActive(false);
            }
        }
    }

    void Update()
    {
        // 🌟 本地 UI 控制：按 Tab 鍵只會開關「自己」電腦上的清單，不用透過網路
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            isShowing = !isShowing;

            if (currentSlideCoroutine != null) StopCoroutine(currentSlideCoroutine);
            currentSlideCoroutine = StartCoroutine(SlideUI(isShowing));
        }
    }

    private IEnumerator SlideUI(bool show)
    {
        if (show) taskListUI.SetActive(true);

        Vector2 startPos = uiRectTransform.anchoredPosition;
        Vector2 targetPos = show ? shownPosition : hiddenPosition;
        float time = 0;

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;
            t = t * (2f - t); 

            uiRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        uiRectTransform.anchoredPosition = targetPos;
        if (!show) taskListUI.SetActive(false);
    }

    // ==========================================
    // 🌐 網路同步區域 (由 Server 呼叫，通知所有 Client)
    // ==========================================

    /// <summary>
    /// 其他腳本要完成任務時呼叫這個。它會確認是否由伺服器發起廣播。
    /// </summary>
    public void CompleteTask(int taskIndex)
    {
        // 確保只有伺服器有權限發布「任務完成」的廣播
        if (IsServer)
        {
            CompleteTaskClientRpc(taskIndex);
        }
    }

    // 🌟 【ClientRpc】伺服器廣播給所有人：「第幾號任務完成了，大家一起畫刪除線！」
    [ClientRpc]
    private void CompleteTaskClientRpc(int taskIndex)
    {
        if (taskIndex >= 0 && taskIndex < tasks.Length)
        {
            if (tasks[taskIndex].checkMark != null)
                tasks[taskIndex].checkMark.SetActive(true);

            if (tasks[taskIndex].strikeLine != null)
                tasks[taskIndex].strikeLine.SetActive(true);

            if (tasks[taskIndex].taskText != null)
            {
                Color fadedColor = tasks[taskIndex].taskText.color;
                fadedColor.a = 0.5f; 
                tasks[taskIndex].taskText.color = fadedColor;
            }
            
            Debug.Log($"✅ 任務 {taskIndex + 1} 已完成！畫上刪除線！(已同步至本地端)");
        }
    }

    /// <summary>
    /// 其他腳本要標記玩家死亡時呼叫這個。
    /// </summary>
    public void MarkPlayerDead(int playerId)
    {
        // 同樣地，只有伺服器可以判定生死並廣播
        if (IsServer)
        {
            MarkPlayerDeadClientRpc(playerId);
        }
    }

    // 🌟 【ClientRpc】伺服器廣播給所有人：「幾 P 玩家死了，大家一起在他頭像畫叉叉！」
    [ClientRpc]
    private void MarkPlayerDeadClientRpc(int playerId)
    {
        if (playerId >= 0 && playerId < deadCrosses.Length)
        {
            if (deadCrosses[playerId] != null) 
            {
                deadCrosses[playerId].SetActive(true);
                Debug.Log($"💀 玩家 {playerId + 1} 陣亡！(已同步至本地端任務表)");
            }
        }
    }
}