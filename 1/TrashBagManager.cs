using UnityEngine;
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode;

// 🌟 【修改】將 MonoBehaviour 改為 NetworkBehaviour
public class TrashBagManager : NetworkBehaviour
{
    [Header("地圖上的所有垃圾袋位置")]
    public GameObject[] allTrashBags;

    [Header("每次遊戲要隨機出現幾個？")]
    public int bagsToSpawn = 3;

    // 🌟 【修改】使用 Awake 讓所有玩家在一開始先乖乖把垃圾袋都藏起來
    void Awake()
    {
        if (allTrashBags == null || allTrashBags.Length == 0) return;

        // 1. 所有人（不管是不是伺服器）一開始都先隱藏所有垃圾袋
        for (int i = 0; i < allTrashBags.Length; i++)
        {
            if (allTrashBags[i] != null)
            {
                allTrashBags[i].SetActive(false);
            }
        }
    }

    // 🌟 【NGO 核心】當網路載入完成時執行
    public override void OnNetworkSpawn()
    {
        // 2. 只有「伺服器」可以負責洗牌，避免大家洗出來的結果不一樣！
        if (IsServer)
        {
            DetermineAndSpawnBags();
        }
    }

    private void DetermineAndSpawnBags()
    {
        // 建立一個跟垃圾袋數量一樣大的「編號清單」 [0, 1, 2, 3...]
        int[] bagIndices = new int[allTrashBags.Length];
        for (int i = 0; i < allTrashBags.Length; i++)
        {
            bagIndices[i] = i;
        }

        // 熟悉的洗牌魔法：打亂「編號清單」的順序！
        for (int i = 0; i < bagIndices.Length; i++)
        {
            int randomIndex = Random.Range(0, bagIndices.Length);
            int temp = bagIndices[i];
            bagIndices[i] = bagIndices[randomIndex];
            bagIndices[randomIndex] = temp;
        }

        // 挑選前 N 個編號，當作這次的「中獎號碼」
        int spawnCount = Mathf.Min(bagsToSpawn, allTrashBags.Length);
        int[] chosenIndices = new int[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            chosenIndices[i] = bagIndices[i];
        }

        // 3. 伺服器抽出號碼後，用大聲公 (ClientRpc) 告訴所有玩家：「把這些號碼的垃圾袋顯示出來！」
        ShowSelectedBagsClientRpc(chosenIndices);
    }

    // 🌟 【ClientRpc】這是伺服器下達的命令，所有玩家收到後都會執行
    [ClientRpc]
    private void ShowSelectedBagsClientRpc(int[] chosenIndices)
    {
        // 大家根據伺服器傳來的「中獎號碼」，把對應的垃圾袋打開
        foreach (int index in chosenIndices)
        {
            // 防呆檢查：確保編號在陣列範圍內，且該垃圾袋物件還存在
            if (index >= 0 && index < allTrashBags.Length && allTrashBags[index] != null)
            {
                allTrashBags[index].SetActive(true);
            }
        }

        Debug.Log($"🗑️ 垃圾袋生成完畢！本次共出現 {chosenIndices.Length} 個垃圾袋，所有人畫面皆已同步。");
    }
}