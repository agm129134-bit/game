using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;

public class PlayerSkillController : MonoBehaviour
{
    public enum CharacterRole { None, Magician, Inventor }

    [Header("角色設定")]
    public CharacterRole currentRole = CharacterRole.Magician; 

    [Header("技能時間設定")]
    public float magicianDuration = 5f;  
    public float inventorDuration = 10f; 
    public float skillCooldown = 15f;    

    [Header("視覺效果綁定")]
    public SpriteRenderer playerSprite;  
    public GameObject shieldEffect;      
    public Animator shieldAnimator;      

    [Header("當前狀態 (供大魚讀取)")]
    public bool isInvisible = false;     
    public bool isShielded = false;      
    
    private bool isSkillReady = true;    

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isSkillReady)
        {
            CastSkill();
        }
    }

    void CastSkill()
    {
        if (currentRole == CharacterRole.Magician) StartCoroutine(MagicianSkillRoutine());
        else if (currentRole == CharacterRole.Inventor) StartCoroutine(InventorSkillRoutine());
    }

    IEnumerator MagicianSkillRoutine()
    {
        isSkillReady = false;
        isInvisible = true;
        
        if (SkillUIManager.Instance != null)
            SkillUIManager.Instance.TriggerSkillUI(magicianDuration + skillCooldown);

        if (playerSprite != null)
        {
            Color c = playerSprite.color;
            c.a = 0.3f; 
            playerSprite.color = c;
        }

        yield return new WaitForSeconds(magicianDuration);

        isInvisible = false;
        if (playerSprite != null)
        {
            Color c = playerSprite.color;
            c.a = 1f; 
            playerSprite.color = c;
        }

        yield return new WaitForSeconds(skillCooldown);
        isSkillReady = true;
    }

    IEnumerator InventorSkillRoutine()
    {
        isSkillReady = false;
        isShielded = true;

        if (SkillUIManager.Instance != null)
            SkillUIManager.Instance.TriggerSkillUI(inventorDuration + skillCooldown);

        if (shieldEffect != null) shieldEffect.SetActive(true);

        yield return new WaitForSeconds(inventorDuration);

        isShielded = false;
        if (shieldEffect != null) shieldEffect.SetActive(false);

        yield return new WaitForSeconds(skillCooldown);
        isSkillReady = true;
    }
}