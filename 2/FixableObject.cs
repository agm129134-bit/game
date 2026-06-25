using UnityEngine;
using System.Collections; 

public class FixableObject : MonoBehaviour
{
    [Header("工具與修復設定")]
    public string needToolID;       // 觸發此物件所需對應的工具 ID
    public GameObject repairResult; // 修理成功後要顯示的替換物件或圖案
    
    [Header("音效設定")]
    public AudioClip fixSound;      // 修理時播放的音效檔案
    private AudioSource audioSource;// 負責播放音效的來源元件

    // 靜音變數：全遊戲共用，標記目前是否正在播放音效，避免多個物件同時發出聲音
    public static bool IsPlayingSound = false;

    void Start()
    {
        // 遊戲開始時，自動在場景中尋找名為 AudioManager 的物件並取得其 AudioSource
        GameObject manager = GameObject.Find("AudioManager");
        if (manager != null)
        {
            audioSource = manager.GetComponent<AudioSource>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 取得碰撞對象身上的 ToolItem 腳本
        ToolItem tool = other.GetComponent<ToolItem>();
        
        // 觸發條件判斷：
        // 1. 碰到的是工具
        // 2. 工具的 ID 與本物件需要的 ID 相同
        // 3. 目前全域沒有其他修復音效正在播放
        if (tool != null && tool.toolID == needToolID && !IsPlayingSound)
        {
            StartCoroutine(FixSequence());
        }
    }

    // 使用協程來控制時間先後順序，確保視覺與聽覺同步
    IEnumerator FixSequence()
    {
        // 1. 進入鎖定狀態，防止玩家連續快速觸發多個物件導致音效重疊
        IsPlayingSound = true;
        Debug.Log("正在播放音效，暫時鎖定清理功能...");

        // 2. 顯示修復結果 (如果有的話)
        if (repairResult != null) 
        {
            repairResult.SetActive(true);
        }

        // 3. 隱藏目前的髒汙/損壞物件，並關閉碰撞器避免重複觸發
        if (GetComponent<SpriteRenderer>() != null)
        {
            GetComponent<SpriteRenderer>().enabled = false;
        }
        
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }

        // 4. 處理音效播放與等待時間
        if (audioSource != null && fixSound != null)
        {
            audioSource.PlayOneShot(fixSound);
            
            // 程式在此暫停，等待音效檔案的長度時間播完，再繼續往下執行
            yield return new WaitForSeconds(fixSound.length);
        }
        else
        {
            // 防呆機制：如果忘記放音效檔，給予一個預設的短暫緩衝時間，避免程式卡死
            yield return new WaitForSeconds(0.5f);
        }

        // 5. 解除鎖定狀態，允許下一個物件被觸發
        IsPlayingSound = false;
        Debug.Log("音效播放結束，可以清理下一個物件了！");

        // 6. 徹底關閉此物件
        // 當此物件被關閉時，CleanupManager 就會偵測到它已被清理並將其從清單移除
        gameObject.SetActive(false);
    }
}