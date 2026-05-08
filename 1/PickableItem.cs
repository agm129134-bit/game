using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode;

// 🌟 【修改】改為繼承 NetworkBehaviour
public class PickableItem : NetworkBehaviour
{
    [Header("這個道具在道具欄要顯示的圖案")]
    public Sprite itemIconSprite; 

    private ItemBarManager itemManager;
    private bool isPlayerInRange = false; 
    private Transform playerTransform; 

    public static List<PickableItem> nearbyItems = new List<PickableItem>();

    // 🌟 【超級防護罩】記錄「上一次按下 F 鍵並成功觸發」是哪一個畫面幀
    public static int lastInteractFrame = -1;

    void Start()
    {
        // 尋找本地的 UI 管理器 (因為每個玩家看自己的畫面，所以找本地的沒問題)
        itemManager = FindAnyObjectByType<ItemBarManager>();
    }

    void Update()
    {
        // 🌟 【NGO 保護】確保網路已經啟動才執行
        if (!IsSpawned) return;

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            // 🛑 鎖頭防呆：防止同一幀重複觸發
            if (lastInteractFrame == Time.frameCount) return;

            PickableItem closestItem = GetClosestItem();

            if (closestItem == this)
            {
                // 🌟 我搶到 F 鍵了！馬上把這一幀「上鎖」
                lastInteractFrame = Time.frameCount;

                if (itemManager != null)
                {
                    // 嘗試把道具加進本地玩家的背包
                    bool isSuccess = itemManager.AddItem(itemIconSprite);
                    if (isSuccess)
                    {
                        nearbyItems.Remove(this); 
                        
                        // 🌟 【NGO 核心修改】
                        // 單機版是 Destroy(gameObject);
                        // 連線版必須「呼叫伺服器」把這個道具從所有人的畫面上抹除！
                        DespawnItemServerRpc(); 
                    }
                }
            }
        }
    }

    // ==========================================
    // 🌐 網路同步邏輯區 (NGO)
    // ==========================================

    // 🌟 【ServerRpc】這是一個傳送給伺服器的請求。
    // RequireOwnership = false 表示「即使我不擁有這個道具的控制權，我也可以呼叫這個函式」
    // (因為地圖上的道具通常是 Server 擁有的)
    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemServerRpc()
    {
        // 只有伺服器有權限執行 Despawn (反生成)
        // 這會把這個道具從所有玩家的遊戲畫面中移除，並且自動銷毀 (Destroy) 它
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); 
        }
    }

    // ==========================================
    // 🔍 距離計算與碰撞偵測 (完全保留你的優良邏輯)
    // ==========================================

    private PickableItem GetClosestItem()
    {
        PickableItem closest = null;
        float minDistance = float.MaxValue;
        
        nearbyItems.RemoveAll(item => item == null);

        foreach (PickableItem item in nearbyItems)
        {
            float distance = Vector2.Distance(playerTransform.position, item.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = item;
            }
        }
        return closest;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 🌟 【NGO 核心修改】：確認這個 Player 是不是「我操作的那個玩家」
            // 避免你的隊友走到道具旁邊，結果你的電腦也判定「可以撿取」
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                isPlayerInRange = true;
                playerTransform = other.transform; 
                if (!nearbyItems.Contains(this)) nearbyItems.Add(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 🌟 【NGO 核心修改】同樣只判斷「我自己」是不是離開了範圍
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                isPlayerInRange = false;
                if (nearbyItems.Contains(this)) nearbyItems.Remove(this);
            }
        }
    }

    private void OnDestroy()
    {
        // 當伺服器 Despawn 這個物件時，確保它從本地名單中移除
        if (nearbyItems.Contains(this)) nearbyItems.Remove(this);
    }
}