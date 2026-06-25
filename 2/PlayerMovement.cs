using System.Collections;
using Unity.Netcode; 
using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerMovement : NetworkBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 5f;
    private float originalSpeed;

    [Header("狀態監控 (除錯用)")]
    public bool isStunned = false; 

    private SpriteRenderer sr;
    private Animator myAnimator;
    private Coroutine stunCoroutine;
    private Coroutine boostCoroutine; 

    private enum Facing { Left, Right, Front }
    private Facing currentFacing = Facing.Left; 

    private const string ANIM_PARA_STUN_LEFT = "Stun_Left";   
    private const string ANIM_PARA_STUN_FRONT = "Stun_Front"; 
    private const string ANIM_PARA_END_STUN = "EndStun";

    // ==========================================
    // 🔮 主動技能特效與狀態變數
    // ==========================================
    [Header("技能特效與狀態")]
    [Tooltip("請把做好的 ShieldEffect 動畫物件拖進來")]
    public GameObject shieldEffectObject; 
    
    public bool isMagicianInvisible = false;
    public bool isShieldActive = false;

    void Start()
    {
        // 自動往下尋找真正的圖片與動畫控制器，避開空畫布陷阱！
        sr = GetComponentInChildren<SpriteRenderer>();
        myAnimator = GetComponentInChildren<Animator>();
        originalSpeed = moveSpeed;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (isStunned) return;

        float moveX = 0f;
        float moveY = 0f;

        // 全面使用新版 Input System (支援 WASD 與 方向鍵)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
            
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveY -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveY += 1f;
        }

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        transform.Translate(movement * moveSpeed * Time.deltaTime);

        // 自動判斷面向並翻轉圖片
        if (sr != null)
        {
            if (moveX > 0.1f)
            {
                sr.flipX = true; 
                currentFacing = Facing.Right;
            }
            else if (moveX < -0.1f)
            {
                sr.flipX = false; 
                currentFacing = Facing.Left;
            }
            else if (moveY > 0.1f || moveY < -0.1f)
            {
                currentFacing = Facing.Front;
            }
        }
    }

    // ==========================================
    // 👟 加速與暈眩邏輯
    // ==========================================
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

        if (myAnimator != null)
        {
            if (currentFacing == Facing.Right)
                myAnimator.SetTrigger(ANIM_PARA_STUN_LEFT);
            else if (currentFacing == Facing.Left)
                myAnimator.SetTrigger(ANIM_PARA_STUN_LEFT);
            else if (currentFacing == Facing.Front)
                myAnimator.SetTrigger(ANIM_PARA_STUN_FRONT);
        }

        yield return new WaitForSeconds(duration);

        if (myAnimator != null)
        {
            myAnimator.SetTrigger(ANIM_PARA_END_STUN); 
        }

        isStunned = false; 
    }

    // ==========================================
    // 🧙‍♂️ 魔術師：隱身邏輯
    // ==========================================
    public void ActivateInvisibility(float duration)
    {
        if (IsOwner) 
        {
            SetInvisibilityServerRpc(true);
            StartCoroutine(InvisibilityTimer(duration));
        }
    }

    private IEnumerator InvisibilityTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (IsOwner) SetInvisibilityServerRpc(false); 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SetInvisibilityServerRpc(bool isInvisible)
    {
        isMagicianInvisible = isInvisible; 
        SetInvisibilityClientRpc(isInvisible); 
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetInvisibilityClientRpc(bool isInvisible)
    {
        isMagicianInvisible = isInvisible; 
        
        if (sr != null)
        {
            if (isInvisible)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null && localPlayer.CompareTag("BigFish"))
                {
                    sr.color = new Color(1, 1, 1, 0f); 
                }
                else
                {
                    sr.color = new Color(1, 1, 1, 0.5f); 
                }
            }
            else
            {
                sr.color = new Color(1, 1, 1, 1f); 
            }
        }
    }

    // ==========================================
    // 🛠️ 發明家：護罩邏輯
    // ==========================================
    public void ActivateShield(float duration)
    {
        if (IsOwner)
        {
            SetShieldServerRpc(true);
            StartCoroutine(ShieldTimer(duration));
        }
    }

    private IEnumerator ShieldTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (IsOwner) SetShieldServerRpc(false); 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SetShieldServerRpc(bool isActive)
    {
        isShieldActive = isActive; 
        SetShieldClientRpc(isActive);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetShieldClientRpc(bool isActive)
    {
        isShieldActive = isActive;
        
        if (shieldEffectObject != null)
        {
            shieldEffectObject.SetActive(isActive);
        }
    }
}