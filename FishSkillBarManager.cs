using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 🌟 要求這個腳本所在的物件一定要有 AudioSource 元件，用來發出聲音
[RequireComponent(typeof(AudioSource))]
public class FishSkillBarManager : MonoBehaviour
{
    // ==========================================
    // 🌟 左下角 UI 狀態列設定
    // ==========================================
    [Header("左下角技能欄 UI 設定")]
    public Image[] skillIcons;
    public Image[] cooldownMasks;
    public Text[] cooldownTexts; 

    // ==========================================
    // 🌟 右下角「罰！」大按鈕 UI 設定
    // ==========================================
    [Header("右下角「罰！」攻擊按鈕 UI 設定")]
    public Button attackButton;           
    public Image buttonBaseImage;         
    public Image buttonCooldownOverlay;   
    public Sprite readySprite;            
    public Sprite cooldownSprite;         

    // ==========================================
    // 🐟 技能 1 設定 (雷達追蹤)
    // ==========================================
    [Header("🐟 技能 1 設定 (雷達追蹤)")]
    public float skill1Cooldown = 60f; 
    public float skill1Duration = 8f;  
    
    public GameObject trackingArrow;   
    public Transform humanTransform;   
    public Transform fishTransform;    
    public float orbitDistance = 2.0f; 

    [Header("✨ 技能 1 附加效果：人類高亮")]
    public bool enableHumanHighlight = true; 
    public SpriteRenderer humanSpriteRenderer; 
    public Color highlightColor = Color.red;
    private Color originalHumanColor; 

    private float skill1CurrentCD = 0f; 
    private bool isTracking = false;    

    // ==========================================
    // 💦 技能 2 設定 (原汁原味的口水彈)
    // ==========================================
    [Header("💦 技能 2 設定 (口水彈)")]
    public GameObject spitPrefab; 
    public Transform firePoint;   
    public bool reverseShootDirection = false; 
    public float skill2Cooldown = 20f; 
    public AudioClip spitSound; 

    private float skill2CurrentCD = 0f; 
    private int currentSelectedIndex = 0; 

    // ==========================================
    // ⚡ 額外攻擊設定 (獨立的「罰！」按鈕)
    // ==========================================
    [Header("⚡ 額外攻擊設定 (右下角『罰』)")]
    [Tooltip("🌟 罰的冷卻時間 (10 秒)")]
    public float punishCooldown = 10f;
    
    [Tooltip("🌟 罰的攻擊範圍 (大魚跟玩家距離多近才能打到？)")]
    public float punishRange = 2.5f; 
    
    [Tooltip("🌟 攻擊動畫播放時間 (秒)，這段時間大魚會停在原地吐舌頭")]
    public float punishAnimDuration = 0.5f;

    [Tooltip("請把大魚『罰』的攻擊音效拖進來")]
    public AudioClip punishSound;

    private float punishCurrentCD = 0f; 
    private AudioSource audioSource; 

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        SelectSlot(0);
        
        if (cooldownMasks.Length > 0 && cooldownMasks[0] != null) cooldownMasks[0].fillAmount = 0;
        if (cooldownMasks.Length > 1 && cooldownMasks[1] != null) cooldownMasks[1].fillAmount = 0;
        if (cooldownTexts.Length > 0 && cooldownTexts[0] != null) cooldownTexts[0].text = "";
        if (cooldownTexts.Length > 1 && cooldownTexts[1] != null) cooldownTexts[1].text = "";

        if (buttonCooldownOverlay != null) buttonCooldownOverlay.fillAmount = 0;
        if (buttonBaseImage != null && readySprite != null) buttonBaseImage.sprite = readySprite;

        if (trackingArrow != null) trackingArrow.SetActive(false);
    }

    void Update()
    {
        // ----------------------------------------------------
        // 🌟 衛星環繞魔法 (技能1)
        // ----------------------------------------------------
        if (isTracking && trackingArrow != null && humanTransform != null && fishTransform != null)
        {
            Vector2 direction = humanTransform.position - fishTransform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            trackingArrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            trackingArrow.transform.position = fishTransform.position + (Vector3)direction.normalized * orbitDistance;
        }

        // ----------------------------------------------------
        // ⏳ 處理冷卻時間倒數與所有 UI 更新
        // ----------------------------------------------------
        
        // 【左下角技能 1 (雷達)】
        if (skill1CurrentCD > 0)
        {
            skill1CurrentCD -= Time.deltaTime; 
            if (cooldownMasks.Length > 0 && cooldownMasks[0] != null) 
                cooldownMasks[0].fillAmount = skill1CurrentCD / skill1Cooldown; 
            if (cooldownTexts.Length > 0 && cooldownTexts[0] != null)
                cooldownTexts[0].text = Mathf.Ceil(skill1CurrentCD).ToString(); 
        }
        else if (cooldownTexts.Length > 0 && cooldownTexts[0] != null) cooldownTexts[0].text = ""; 

        // 【左下角技能 2 (口水彈)】
        if (skill2CurrentCD > 0)
        {
            skill2CurrentCD -= Time.deltaTime;
            if (cooldownMasks.Length > 1 && cooldownMasks[1] != null) 
                cooldownMasks[1].fillAmount = skill2CurrentCD / skill2Cooldown;
            if (cooldownTexts.Length > 1 && cooldownTexts[1] != null)
                cooldownTexts[1].text = Mathf.Ceil(skill2CurrentCD).ToString();
        }
        else if (cooldownTexts.Length > 1 && cooldownTexts[1] != null) cooldownTexts[1].text = "";

        // 【右下角「罰」大按鈕的獨立冷卻】
        if (punishCurrentCD > 0)
        {
            punishCurrentCD -= Time.deltaTime;
            
            if (buttonCooldownOverlay != null) buttonCooldownOverlay.fillAmount = punishCurrentCD / punishCooldown;
            if (attackButton != null) attackButton.interactable = false; 
        }
        else 
        {
            if (buttonCooldownOverlay != null) buttonCooldownOverlay.fillAmount = 0;
            if (attackButton != null) attackButton.interactable = true;
            if (buttonBaseImage != null && readySprite != null) buttonBaseImage.sprite = readySprite; 
        }

        // ----------------------------------------------------
        // ⌨️ 鍵盤操作 (控制左下角的技能)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectSlot(0); 
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectSlot(1); 
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) UseSkill(currentSelectedIndex); 
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= skillIcons.Length) return;
        currentSelectedIndex = index;
        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (skillIcons[i] != null)
            {
                skillIcons[i].transform.localScale = i == currentSelectedIndex ? new Vector3(1.15f, 1.15f, 1f) : new Vector3(1f, 1f, 1f);
            }
        }
    }

    public void UseSkill(int slotIndex)
    {
        if (slotIndex == 0 && skill1CurrentCD <= 0)
        {
            skill1CurrentCD = skill1Cooldown; 
            StartCoroutine(TrackHumanRoutine()); 
        }
        else if (slotIndex == 1 && skill2CurrentCD <= 0) 
        {
            CastSpitSkill(); 
        }
    }

    private IEnumerator TrackHumanRoutine()
    {
        if (trackingArrow != null)
        {
            isTracking = true;
            trackingArrow.SetActive(true); 
        }

        if (enableHumanHighlight && humanSpriteRenderer != null)
        {
            originalHumanColor = humanSpriteRenderer.color; 
            humanSpriteRenderer.color = highlightColor;     
        }

        yield return new WaitForSeconds(skill1Duration); 

        if (trackingArrow != null)
        {
            trackingArrow.SetActive(false); 
            isTracking = false;
        }

        if (enableHumanHighlight && humanSpriteRenderer != null)
        {
            humanSpriteRenderer.color = originalHumanColor; 
        }
    }

    private void CastSpitSkill()
    {
        if (spitPrefab != null && firePoint != null)
        {
            GameObject projectile = Instantiate(spitPrefab, firePoint.position, Quaternion.identity);
            
            SpriteRenderer fishSprite = firePoint.GetComponentInParent<SpriteRenderer>();
            Vector2 shootDirection = Vector2.right; 
            
            if (fishSprite != null && fishSprite.flipX) shootDirection = Vector2.left; 
            if (reverseShootDirection) shootDirection = -shootDirection; 

            SpitProjectile sp = projectile.GetComponent<SpitProjectile>();
            if (sp != null) sp.SetDirection(shootDirection);
            
            skill2CurrentCD = skill2Cooldown;

            if (spitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(spitSound);
            }
            
            Debug.Log("💦 大魚發射了一發口水彈！");
        }
    }

    // ==========================================
    // ⚡ 直接「罰！」扣血的專用接口 
    // ==========================================
    public void ClickToPunish()
    {
        // 1. 檢查罰的冷卻時間到了沒
        if (punishCurrentCD <= 0)
        {
            // 只要按了按鈕，就一定會發動招式 (進入冷卻、變換圖片、播放音效)
            punishCurrentCD = punishCooldown;
            if (buttonBaseImage != null && cooldownSprite != null) buttonBaseImage.sprite = cooldownSprite;
            if (punishSound != null && audioSource != null) audioSource.PlayOneShot(punishSound);

            // 呼叫播放大魚攻擊動畫的協程
            StartCoroutine(PlayPunishAnimationRoutine());

            // 2. 判斷人類有沒有在攻擊範圍內
            if (humanTransform != null && fishTransform != null)
            {
                float distance = Vector2.Distance(fishTransform.position, humanTransform.position);
                
                if (distance <= punishRange)
                {
                    if (GameManager.Instance != null)
                    {
                        int targetPlayerId = 0; // 預設攻擊 1P
                        GameManager.Instance.TakeDamage(targetPlayerId);
                        Debug.Log($"⚡ 大魚發動了『罰』！命中玩家，扣除一滴血！(目前距離: {distance})");
                    }

                    // ==========================================
                    // 🌟 【新增】呼叫人類身上的 TakeDamage，觸發受傷動畫！
                    // ==========================================
                    PlayerMovement playerMov = humanTransform.GetComponent<PlayerMovement>();
                    if (playerMov != null)
                    {
                        // 把大魚的位置 (fishTransform) 傳過去，讓人知道從哪邊被打
                        playerMov.TakeDamage(fishTransform);
                    }
                }
                else
                {
                    Debug.Log($"💨 大魚發動了『罰』！但是揮空了...(目前距離 {distance}，超過了攻擊範圍 {punishRange})");
                }
            }
            else
            {
                 Debug.LogWarning("⚠️ 找不到玩家或大魚的座標，無法判斷距離！請確認 Inspector 的 Transform 都有拉進去！");
            }
        }
        else
        {
             Debug.Log("『罰』還在冷卻中！");
        }
    }

    // ==========================================
    // 🎬 播放攻擊動畫與時間暫停魔法
    // ==========================================
    private IEnumerator PlayPunishAnimationRoutine()
    {
        if (fishTransform == null) yield break;

        Animator fishAnimator = fishTransform.GetComponent<Animator>();
        MonoBehaviour fishMovement = fishTransform.GetComponent("FishMovement") as MonoBehaviour;

        // 1. 🛑 鎖定狀態：暫時關閉大魚的移動腳本，讓牠停下來專心咬人！
        if (fishMovement != null) fishMovement.enabled = false;

        // 2. 判斷方向並播放動畫
        if (fishAnimator != null)
        {
            string animName = "Fish_Attack_L"; // 預設向左咬
            
            // 如果玩家在大魚的右邊，就向右咬
            if (humanTransform != null && humanTransform.position.x > fishTransform.position.x)
            {
                animName = "Fish_Attack_R";
            }
            
            // 強制大腦播放吐舌頭動畫
            fishAnimator.Play(animName);
        }

        // 3. ⏳ 等待動畫播完
        yield return new WaitForSeconds(punishAnimDuration);

        // 4. 🟢 解除鎖定：重新開啟大魚的移動腳本，繼續追殺！
        if (fishMovement != null) fishMovement.enabled = true;
    }
}