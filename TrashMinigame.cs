using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrashMinigame : MonoBehaviour
{
    public enum TrashCategory { General, Plastic, Paper }

    [System.Serializable]
    public struct TrashData
    {
        public Sprite sprite;
        public Sprite nextImage;
        public TrashCategory category;
        
        // 🌟 縮放功能：讓筷子變大就靠這格
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
    public Transform playerTransform;
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
        // 確保玩家與腳本都還在才進行邏輯判斷
        if (this == null || playerTransform == null) return;

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

        // 大魚警告邏輯，加上安全檢查
        if (isPlaying && bigFishTransform != null && playerTransform != null && whiteFrameUI != null)
        {
            float distance = Vector2.Distance(playerTransform.position, bigFishTransform.position);
            whiteFrameUI.color = distance <= dangerDistance ? Color.red : Color.white;
        }
    }

    private void StartNewGame()
    {
        if (allTrash == null || allTrash.Length == 0 || minigameUI == null) return;

        minigameUI.SetActive(true);
        isPlaying = true;
        currentTrashIndex = 0; 

        if (currentTrashObj != null)
        {
            currentTrashObj.manager = this; 
        }

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
        // 👻 抓鬼第一站：物件還在嗎？
        if (this == null || currentTrashObj == null) return;

        if (currentTrashIndex < requiredTrashCount)
        {
            TrashData currentData = currentSessionTrash[currentTrashIndex];
            Image trashImg = currentTrashObj.GetComponent<Image>();

            if (trashImg != null)
            {
                trashImg.sprite = currentData.sprite;
                // 🌟 設定縮放 (保護機制：如果忘記設 Scale，自動當作 1)
                float s = currentData.imageScale > 0.01f ? currentData.imageScale : 1f;
                currentTrashObj.transform.localScale = Vector3.one * s;
            }

            currentTrashObj.ResetPosition(trashSpawnPosition); 
            currentTrashObj.gameObject.SetActive(true);

            // 預告圖更新
            if (currentTrashIndex + 1 < requiredTrashCount && nextTrashPreview != null)
            {
                TrashData nextData = currentSessionTrash[currentTrashIndex + 1];
                Sprite nextSp = nextData.nextImage != null ? nextData.nextImage : nextData.sprite;

                nextTrashPreview.sprite = nextSp;
                
                // 🌟 預告圖也跟著縮放，才不會變形
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

        // 安全取得座標
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

    private void FinishMinigame()
    {
        isPlaying = false;
        if (currentTrashObj != null) currentTrashObj.gameObject.SetActive(false);
        
        PlaySound(gameClearSound);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTrashBagSorted();
        }

        // 👻 抓鬼關鍵：使用協程前確保物件還沒被 Destroy
        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(CloseAfterDelay());
        }
    }
    
    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);
        
        // 👻 結束前最後檢查一次，避免報錯
        if (this != null && minigameUI != null) 
        {
            minigameUI.SetActive(false);
            gameObject.SetActive(false); 
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
        
        // 播完後銷毀音效物件
        Destroy(soundPlayer, clip.length + 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (isPlaying) CloseAndResetMinigame();
        }
    }
}