using UnityEngine;
using System.Collections.Generic;
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode;

// 🌟 【修改】將 MonoBehaviour 改為 NetworkBehaviour
public class ItemSpawner : NetworkBehaviour
{
    [Header("道具模具 (預製體 Prefabs)")]
    [Tooltip("⚠️ 注意：這些 Prefab 上面必須掛載 NetworkObject 元件！")]
    public GameObject shoePrefab;
    public GameObject clockPrefab;

    [Header("生成數量設定")]
    public int shoeCount = 5;
    public int minClockCount = 0;
    public int maxClockCount = 2;

    [Header("生成範圍設定 (綠色大框框)")]
    public Vector2 spawnRangeX = new Vector2(-7f, 7f); 
    public Vector2 spawnRangeY = new Vector2(-3.5f, 3.5f);

    [Header("🌟 精準生成範圍 (不規則多邊形)")]
    public PolygonCollider2D spawnAreaPolygon;

    [Header("特殊道具設定")]
    public GameObject puzzlePhotoPrefab;
    public Transform lakeSpawnPoint; 

    [Header("防重疊與距離設定")]
    public float minSpawnDistance = 0.5f; 
    public float physicsCheckRadius = 0.5f; 
    public int maxSpawnAttempts = 150;

    private List<Vector2> occupiedPositions = new List<Vector2>();

    // 🌟 【修改】把原本的 Start() 改成 NGO 專用的 OnNetworkSpawn()
    public override void OnNetworkSpawn()
    {
        // 🌟 核心規則：只有「伺服器」可以決定在哪裡生成道具
        // 如果是客戶端 (一般玩家) 執行到這行，會直接 return 退出，乖乖等伺服器同步畫面
        if (!IsServer) return;

        occupiedPositions.Clear();
        if (lakeSpawnPoint != null)
        {
            occupiedPositions.Add(lakeSpawnPoint.position);
            SpawnPuzzlePhoto();
        }
        SpawnItems();
    }

    public void SpawnItems()
    {
        for (int i = 0; i < shoeCount; i++) SpawnSingleItem(shoePrefab);
        int clockCount = Random.Range(minClockCount, maxClockCount + 1);
        for (int i = 0; i < clockCount; i++) SpawnSingleItem(clockPrefab);
    }

    private void SpawnSingleItem(GameObject prefab)
    {
        if (prefab == null) return; 

        Vector2 finalSpawnPosition = Vector2.zero;
        bool positionFound = false;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float randomX = Random.Range(spawnRangeX.x, spawnRangeX.y);
            float randomY = Random.Range(spawnRangeY.x, spawnRangeY.y);
            Vector2 randomPos = new Vector2(randomX, randomY);

            // 1. 確保這個點在草地多邊形範圍內
            if (spawnAreaPolygon != null && !spawnAreaPolygon.OverlapPoint(randomPos)) 
            {
                continue; 
            }

            bool isTooClose = false;

            // 2. 檢查跟其他生成的道具會不會太近
            foreach (Vector2 occupied in occupiedPositions)
            {
                if (Vector2.Distance(randomPos, occupied) < minSpawnDistance)
                {
                    isTooClose = true; 
                    break;             
                }
            }

            // 3. 全範圍掃描雷達
            if (!isTooClose)
            {
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(randomPos, physicsCheckRadius);
                
                foreach (Collider2D hit in hitColliders)
                {
                    if (hit != spawnAreaPolygon)
                    {
                        isTooClose = true;
                        break; 
                    }
                }
            }

            if (!isTooClose)
            {
                finalSpawnPosition = randomPos;
                positionFound = true;
                break; 
            }
        }

        if (positionFound)
        {
            // 伺服器在場景上生成物件
            GameObject newItem = Instantiate(prefab, finalSpawnPosition, Quaternion.identity);
            if (newItem != null) 
            {
                // 🌟 【NGO 關鍵步驟】通知網路把這個剛生成的物件同步給所有連線的玩家！
                NetworkObject netObj = newItem.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn(); // 這個 Spawn 指的是網路同步生成
                }
                else
                {
                    Debug.LogError($"🚨 {prefab.name} 上面沒有掛載 NetworkObject 元件！網路無法同步！");
                }
            }
            occupiedPositions.Add(finalSpawnPosition); 
        }
        else
        {
            Debug.LogWarning($"🚨 找不到空位生成 {prefab.name}！");
        }
    }

    private void SpawnPuzzlePhoto()
    {
        if (puzzlePhotoPrefab == null || lakeSpawnPoint == null) return;
        
        GameObject newPhoto = Instantiate(puzzlePhotoPrefab, lakeSpawnPoint.position, Quaternion.identity);
        if (newPhoto != null) 
        {
            // 🌟 拼圖也要透過網路同步生成
            NetworkObject netObj = newPhoto.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("🚨 拼圖 Prefab 上面沒有掛載 NetworkObject 元件！");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        float centerX = (spawnRangeX.x + spawnRangeX.y) / 2f;
        float centerY = (spawnRangeY.x + spawnRangeY.y) / 2f;
        Vector3 center = new Vector3(centerX, centerY, 0);

        float width = spawnRangeX.y - spawnRangeX.x;
        float height = spawnRangeY.y - spawnRangeY.x;
        Vector3 size = new Vector3(width, height, 0);

        Gizmos.DrawWireCube(center, size);
    }
}