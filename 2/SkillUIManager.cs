using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SkillUIManager : MonoBehaviour
{
    public static SkillUIManager Instance;

    [Header("UI 元件綁定")]
    public RectTransform buttonRect; 
    public Image cooldownOverlay;    

    private Vector3 originalScale;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (buttonRect != null) originalScale = buttonRect.localScale;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0; 
    }

    public void TriggerSkillUI(float totalCooldownTime)
    {
        StartCoroutine(ButtonPopEffect());
        StartCoroutine(CooldownRoutine(totalCooldownTime));
    }

    IEnumerator ButtonPopEffect()
    {
        if (buttonRect == null) yield break;

        buttonRect.localScale = originalScale * 1.2f;
        yield return new WaitForSeconds(0.1f);
        buttonRect.localScale = originalScale;
    }

    IEnumerator CooldownRoutine(float cooldownTime)
    {
        if (cooldownOverlay == null) yield break;

        float timer = cooldownTime;
        cooldownOverlay.fillAmount = 1f; 

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            cooldownOverlay.fillAmount = timer / cooldownTime; 
            yield return null; 
        }

        cooldownOverlay.fillAmount = 0f; 
    }
}