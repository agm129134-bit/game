using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// 🌟 【新增】引入 Netcode 命名空間
using Unity.Netcode;

// 🌟 【修改】將 MonoBehaviour 改為 NetworkBehaviour
public class TrashMinigame : NetworkBehaviour
{
    public enum TrashCategory { General, Plastic, Paper }

    [System.Serializable]
    public struct TrashData
    {
        public Sprite sprite;
        public Sprite nextImage;
        public TrashCategory category;
        
        [Range(0.1f, 5f)] 
        public float imageScale; 
    }

    [Header("UI 與物件設定")]
    public GameObject minigameUI;
    public Canvas mainCanvas;
    public DraggableTrash currentTrashObj; 
    public Image nextTrashPreview;          
    public Vector2 trashSpawnPosition = new Vector2(0, -250f); 

    [Header("隱形判定區 (垃圾桶位置)")]
    public RectTransform generalBinTarget; 
    public RectTransform plasticBinTarget; 
    public RectTransform paperBinTarget;   
    public float snapDistance = 100f; 

    [Header("自訂垃圾庫與數量")]
    public int requiredTrashCount = 10; 
    public TrashData[] allTrash;      

    private TrashData[] currentSessionTrash;
    private int currentTrashIndex = 0;

    [Header("大魚警告設定")]
    public Image whiteFrameUI;
    public Transform playerTransform; // 🌟 這裡會動態抓取「本地玩家」的位置
    public Transform bigFishTransform;
    public float dangerDistance = 5f;

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip correctSound;   
    public AudioClip wrongSound;     
    public AudioClip gameClearSound; 

    private bool isPlayerInRange = false;
    private bool isPlaying = false;

    void Update()
    {
        // 確保網路已經連線且玩家存在才執行
        if (!IsSpawned || playerTransform == null) return;

        // 這裡的輸入只有「本地玩家」能觸發，因為 isPlayerInRange 已經被保護了
        if (isPlayerInRange && !isPlaying && Input.GetKeyDown(KeyCode.F))
        {
            if (PickableItem.lastInteractFrame == Time.frameCount) return;

            bool amIClosest = true;
            float myDistance = Vector2.Distance(playerTransform.position, transform.position);
            
            Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(playerTransform.position, 2f);
            
            foreach (Collider2D obj in nearbyObjects)
            {
                if (obj != null && obj.isTrigger && obj.gameObject != this.gameObject)
                {
                    float otherDistance = Vector2.Distance(playerTransform.position, obj.transform.position);
                    if (otherDistance < myDistance)
                    {
                        amIClosest = false;
                        break;
                    }
                }
            }

            if (amIClosest)
            {
                PickableItem.lastInteractFrame = Time.frameCount;
                StartNewGame();
            }
        }

        if (isPlaying && bigFishTransform != null && playerTransform != null && whiteFrameUI != null)
        {
            float distance = Vector2.Distance(playerTransform.position, bigFishTransform.position);
            whiteFrameUI.color = distance <= dangerDistance ? Color.red : Color.white;
        }
    }

    // ==========================================
    // 🎮 小遊戲本體邏輯 (純本地執行，不吃網路效能)
    // ==========================================

    private void StartNewGame()
    {
        if (allTrash == null || allTrash.Length == 0 || minigameUI == null) return;

        minigameUI.SetActive(true);
        isPlaying = true;
        currentTrashIndex = 0; 

        if (currentTrashObj != null) currentTrashObj.manager = this; 
        if (whiteFrameUI != null) whiteFrameUI.color = Color.white;

        currentSessionTrash = new TrashData[requiredTrashCount];
        for (int i = 0; i < requiredTrashCount; i++)
        {
            int randomIndex = Random.Range(0, allTrash.Length);
            currentSessionTrash[i] = allTrash[randomIndex]; 
        }

        LoadTrash(); 
    }

    public void CloseAndResetMinigame()
    {
        isPlaying = false;
        if (minigameUI != null) minigameUI.SetActive(false);
        currentTrashIndex = 0;
    }

    private void LoadTrash()
    {
        if (this == null || currentTrashObj == null) return;

        if (currentTrashIndex < requiredTrashCount)
        {
            TrashData currentData = currentSessionTrash[currentTrashIndex];
            Image trashImg = currentTrashObj.GetComponent<Image>();

            if (trashImg != null)
            {
                trashImg.sprite = currentData.sprite;
                float s = currentData.imageScale > 0.01f ? currentData.imageScale : 1f;
                currentTrashObj.transform.localScale = Vector3.one * s;
            }

            currentTrashObj.ResetPosition(trashSpawnPosition); 
            currentTrashObj.gameObject.SetActive(true);

            if (currentTrashIndex + 1 < requiredTrashCount && nextTrashPreview != null)
            {
                TrashData nextData = currentSessionTrash[currentTrashIndex + 1];
                Sprite nextSp = nextData.nextImage != null ? nextData.nextImage : nextData.sprite;

                nextTrashPreview.sprite = nextSp;
                float ns = nextData.imageScale > 0.01f ? nextData.imageScale : 1f;
                nextTrashPreview.transform.localScale = Vector3.one * ns;
                
                if (nextTrashPreview.transform.parent != null)
                    nextTrashPreview.transform.parent.gameObject.SetActive(true);
                nextTrashPreview.gameObject.SetActive(true);
            }
            else if (nextTrashPreview != null)
            {
                if (nextTrashPreview.transform.parent != null)
                    nextTrashPreview.transform.parent.gameObject.SetActive(false);
                else
                    nextTrashPreview.gameObject.SetActive(false);
            }
        }
        else
        {
            FinishMinigame(); 
        }
    }

    public void CheckDrop(DraggableTrash trashObj, Vector2 dropPos, Vector2 startPos)
    {
        if (currentSessionTrash == null || currentTrashIndex >= currentSessionTrash.Length) return;

        TrashCategory requiredCategory = currentSessionTrash[currentTrashIndex].category;
        bool isCorrect = false;
        bool isDroppedInAnyBin = false;

        if (trashObj == null || generalBinTarget == null || plasticBinTarget == null || paperBinTarget == null) return;

        Vector2 trashPos = trashObj.transform.localPosition;

        if (Vector2.Distance(trashPos, generalBinTarget.localPosition) <= snapDistance)
        {
            isDroppedInAnyBin = true;
            if (requiredCategory == TrashCategory.General) isCorrect = true;
        }
        else if (Vector2.Distance(trashPos, plasticBinTarget.localPosition) <= snapDistance)
        {
            isDroppedInAnyBin = true;
            if (requiredCategory == TrashCategory.Plastic) isCorrect = true;
        }
        else if (Vector2.Distance(trashPos, paperBinTarget.localPosition) <= snapDistance)
        {
            isDroppedInAnyBin = true;
            if (requiredCategory == TrashCategory.Paper) isCorrect = true;
        }

        if (isCorrect)
        {
            PlaySound(correctSound); 
            currentTrashIndex++; 
            LoadTrash();         
        }
        else if (isDroppedInAnyBin)
        {
            PlaySound(wrongSound);   
            trashObj.ResetPosition(startPos); 
        }
        else
        {
            trashObj.ResetPosition(startPos); 
        }
    }

    // ==========================================
    // 🌐 網路同步邏輯 (過關結算)
    // ==========================================

    private void FinishMinigame()
    {
        isPlaying = false;
        if (currentTrashObj != null) currentTrashObj.gameObject.SetActive(false);
        
        PlaySound(gameClearSound);

        // 🌟 【NGO 核心】完成任務後，抓出「自己的玩家編號」，傳送給伺服器處理！
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            int myPlayerId = (int)NetworkManager.Singleton.LocalClientId;
            CompleteTrashBagServerRpc(myPlayerId);
        }

        // 本地端先把自己畫面上的小遊戲關掉
        StartCoroutine(CloseAfterDelay());
    }

    // 🌟 【ServerRpc】伺服器接收到某人完成任務的通知
    [ServerRpc(RequireOwnership = false)]
    private void CompleteTrashBagServerRpc(int playerId)
    {
        if (GameManager.Instance != null)
        {
            // 讓 GameManager 幫該玩家加分
            GameManager.Instance.OnTrashBagSorted(playerId);
        }

        // 廣播給所有人：「這個垃圾袋被撿走了，大家把它隱藏起來！」
        DisableTrashBagClientRpc();
    }

    // 🌟 【ClientRpc】所有人收到伺服器指令，隱藏垃圾袋
    [ClientRpc]
    private void DisableTrashBagClientRpc()
    {
        // 🛑 防呆：如果剛好有其他玩家(隊友)也正在玩這同一袋垃圾，強制中斷他的遊戲！
        if (isPlaying) 
        {
            CloseAndResetMinigame();
            Debug.Log("被隊友搶先分完這袋垃圾了！");
        }

        // 讓垃圾袋從場景上消失
        gameObject.SetActive(false); 
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);
        if (this != null && minigameUI != null) 
        {
            minigameUI.SetActive(false);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        GameObject soundPlayer = new GameObject("TempSoundPlayer");
        AudioSource tempSource = soundPlayer.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.pitch = Random.Range(0.9f, 1.1f);
        tempSource.spatialBlend = 0f; 
        tempSource.volume = 1f;
        tempSource.Play();
        Destroy(soundPlayer, clip.length + 0.1f);
    }

    // ==========================================
    // 🛡️ 玩家碰撞偵測 (嚴格區分自己還是別人)
    // ==========================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            // 🌟 【NGO 保護】只有「本地玩家」靠近，才會觸發可互動狀態
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                isPlayerInRange = true;
                playerTransform = other.transform; // 抓到自己的座標，拿來計算大魚警告距離
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                isPlayerInRange = false;
                if (isPlaying) CloseAndResetMinigame();
            }
        }
    }
}