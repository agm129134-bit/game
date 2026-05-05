using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))] 
public class PlayerMovement : MonoBehaviour 
{
    [Header("移動設定")]
    public float moveSpeed = 5f;
    private float originalSpeed;

    [Header("狀態監控 (除錯用)")]
    public bool isStunned = false; 
    public bool isHurt = false; 

    [Header("受傷設定")]
    [Tooltip("受傷動畫播放時間 (秒)，這段時間玩家會停在原地抱頭")]
    public float hitAnimDuration = 0.4f;

    private SpriteRenderer sr;
    private Animator myAnimator;
    private Coroutine stunCoroutine;
    private Coroutine boostCoroutine; 

    private enum Facing { Left, Right, Front }
    private Facing currentFacing = Facing.Left; 

    private float lastDirectionX = -1f; 

    private const string ANIM_PARA_STUN_LEFT = "Stun_Left";   
    private const string ANIM_PARA_STUN_RIGHT = "Stun_Right"; // 🌟 新增右邊參數
    private const string ANIM_PARA_STUN_FRONT = "Stun_Front"; 
    private const string ANIM_PARA_END_STUN = "EndStun";

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        myAnimator = GetComponent<Animator>();
        originalSpeed = moveSpeed;
    }

    void Update()
    {
        if (isStunned || isHurt) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        transform.Translate(movement * moveSpeed * Time.deltaTime);

        float currentSpeed = (moveX != 0 || moveY != 0) ? 1f : 0f;

        if (moveX > 0.1f)
        {
            currentFacing = Facing.Right;
            lastDirectionX = 1f;   
            sr.flipX = false;      
        }
        else if (moveX < -0.1f)
        {
            currentFacing = Facing.Left;
            lastDirectionX = -1f;  
            sr.flipX = false;
        }
        else if (moveY > 0.1f || moveY < -0.1f)
        {
            currentFacing = Facing.Front;
        }

        if (myAnimator != null)
        {
            myAnimator.SetFloat("speed", currentSpeed);
            myAnimator.SetFloat("DirectionX", lastDirectionX);
        }
    }

    public void TakeDamage(Transform attackerTransform)
    {
        if (isHurt) return;
        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isHurt = true;

        if (myAnimator != null) myAnimator.SetFloat("speed", 0f);

        if (myAnimator != null)
        {
            string hitAnim = (lastDirectionX > 0) ? "Human_Hit_R" : "Human_Hit_L";
            myAnimator.Play(hitAnim);
        }

        yield return new WaitForSeconds(hitAnimDuration);

        if (myAnimator != null)
        {
            myAnimator.Play("Blend Tree"); 
        }

        isHurt = false; 
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        if (boostCoroutine != null) StopCoroutine(boostCoroutine);
        boostCoroutine = StartCoroutine(BoostRoutine(multiplier, duration));
    }

    IEnumerator BoostRoutine(float multiplier, float duration)
    {
        moveSpeed = originalSpeed * multiplier; 
        yield return new WaitForSeconds(duration); 
        moveSpeed = originalSpeed; 
        boostCoroutine = null;
    }

    public void BeStunned(float duration)
    {
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true; 
        if (myAnimator != null) myAnimator.SetFloat("speed", 0f); 

        if (myAnimator != null)
        {
            // 🌟 修正重點：不再手動切換 flipX，而是直接呼叫正確方向的 Trigger
            if (currentFacing == Facing.Right)
            {
                sr.flipX = false; // 確保圖片沒被反轉
                myAnimator.SetTrigger(ANIM_PARA_STUN_RIGHT); // 呼叫右邊動畫
            }
            else if (currentFacing == Facing.Left)
            {
                sr.flipX = false;
                myAnimator.SetTrigger(ANIM_PARA_STUN_LEFT); // 呼叫左邊動畫
            }
            else if (currentFacing == Facing.Front)
            {
                sr.flipX = false;
                myAnimator.SetTrigger(ANIM_PARA_STUN_FRONT);
            }
        }

        yield return new WaitForSeconds(duration);

        if (myAnimator != null) myAnimator.SetTrigger(ANIM_PARA_END_STUN); 

        sr.flipX = false; 
        isStunned = false; 
    }
}