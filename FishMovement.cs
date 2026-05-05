using UnityEngine;
using System.Collections; 

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class FishMovement : MonoBehaviour
{
    [Header("移動設定")]
    public float fishSpeed = 3f;
    public float wanderRadius = 10f; 

    [Header("休息設定")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("攻擊設定")]
    public bool enableAutoAttack = true; 
    public float damageCooldown = 2f; 
    private float lastDamageTime = -999f;

    [HideInInspector] 
    public bool canMove = true;

    // 內部運算變數
    private Vector2 startPosition;  
    private Vector2 targetPosition; 
    private bool isWaiting = false; 

    private Animator myAnimator;
    private float lastDirectionX = -1f; // 預設朝左 (-1)
    
    // 🌟 【新增】用來記錄目前正在播放的動畫名字，避免重複呼叫
    private string currentAnimName = ""; 

    void Start()
    {
        myAnimator = GetComponent<Animator>(); 
        startPosition = transform.position;
        PickNewTargetPosition();
    }

    void Update()
    {
        // ==========================================
        // 🌟 暴力點名法：計算下一秒該播什麼動畫！
        // ==========================================
        string nextAnimName = "";

        if (!canMove || isWaiting) 
        {
            // 🛑 休息狀態：根據最後面向的方向，決定播左邊還是右邊的 Idle
            nextAnimName = (lastDirectionX > 0) ? "Fish_Idle_R" : "Fish_Idle_L";
        }
        else
        {
            // 🏊 移動狀態：先計算方向
            Vector2 moveDirection = targetPosition - (Vector2)transform.position;
            if (moveDirection.x > 0.01f) lastDirectionX = 1f;
            else if (moveDirection.x < -0.01f) lastDirectionX = -1f;

            // 根據方向決定播左邊還是右邊的 Run
            nextAnimName = (lastDirectionX > 0) ? "Fish_Run_R" : "Fish_Run_L";

            // 實際移動位置
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, fishSpeed * Time.deltaTime);
            
            // 檢查是否到達目的地
            if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                StartCoroutine(WaitAndPickNewPosition());
            }
        }

        // 🌟 終極命令：如果「該播的動畫」跟「現在正在播的」不一樣，就強制切換！
        if (myAnimator != null && currentAnimName != nextAnimName)
        {
            myAnimator.Play(nextAnimName); // 直接點名播放，跳過所有判斷機制！
            currentAnimName = nextAnimName; // 記下這次的動畫
        }
    }

    private void PickNewTargetPosition()
    {
        float randomX = Random.Range(-wanderRadius, wanderRadius);
        float randomY = Random.Range(-wanderRadius, wanderRadius);
        targetPosition = startPosition + new Vector2(randomX, randomY);
    }

    private IEnumerator WaitAndPickNewPosition()
    {
        isWaiting = true; 
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime); 
        PickNewTargetPosition();
        isWaiting = false; 
    }

    private void OnTriggerEnter2D(Collider2D other) { TryDealDamage(other.gameObject); }
    private void OnTriggerStay2D(Collider2D other) { TryDealDamage(other.gameObject); }
    private void OnCollisionEnter2D(Collision2D collision) { TryDealDamage(collision.gameObject); }
    private void OnCollisionStay2D(Collision2D collision) { TryDealDamage(collision.gameObject); }

    private void TryDealDamage(GameObject hitObject)
    {
        if (!enableAutoAttack) return;
        if (hitObject.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(0);
                    lastDamageTime = Time.time; 
                    Debug.Log("🐟 大魚咬了玩家！扣一滴血！");
                }
            }
        }
    }
}