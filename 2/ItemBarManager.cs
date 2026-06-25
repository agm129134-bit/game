using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemBarManager : MonoBehaviour
{
    public static ItemBarManager Instance { get; private set; }
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public enum PlayerRole { Default, Fisherman, Magician, Inventor, TaiKe }
    private PlayerRole currentRole = PlayerRole.Default;

    [System.Serializable]
    public struct RoleUIProfile
    {
        public string displayName;
        public PlayerRole role;
        public Sprite itemBarBackground;
        public Sprite skillButtonIcon;
        public Sprite skillButtonBgSprite;
    }

    [Header("🎭 角色 UI 設定")]
    public Image[] uiItemSlotBackgrounds;
    public Image uiSkillButtonIconImage;
    public Image uiSkillButtonBgImage;
    public Transform skillButtonTransform;
    public RoleUIProfile[] roleProfiles;

    [Header("🔮 職業主動技能設定")]
    public float magicianInvisDuration = 5f;
    public float inventorShieldDuration = 10f;

    [Header("UI 設定")]
    public Image[] itemIcons;
    private Sprite[] currentItems;
    private int currentSelectedIndex = 0;

    [Header("⏳ 加時道具")]
    public Sprite hourglassSprite;
    public float addTimeSeconds = 30f;
    public GameObject timePopupPrefab;
    public RectTransform timeTextReferencePoint;

    [Header("👟 加速道具")]
    public Sprite shoeSprite;
    public float speedBoostDuration = 5f;
    public float speedMultiplier = 1.5f;
    public GameObject playerObject;

    public enum PhotoTarget { FishScreen, HumanScreen, BothScreens }

    [Header("📸 照片道具設定")]
    public Sprite fishPhotoSprite;
    public PhotoTarget targetScreen = PhotoTarget.FishScreen;
    public float photoDuration = 3f;
    public GameObject bigPhotoOnFishScreen;
    public GameObject bigPhotoOnHumanScreen;

    [Header("🎞️ 照片滑入動畫")]
    public float slideInDuration = 0.5f;
    public Vector2 photoStartPos = new Vector2(0, -1200f);
    public Vector2 photoEndPos = new Vector2(0, 0);

    [Header("❄️ 凍結目標")]
    public MonoBehaviour movementScriptToFreeze;

    private Coroutine photoCoroutine;

    void OnEnable()
    {
        currentItems = new Sprite[itemIcons.Length];
        for (int i = 0; i < itemIcons.Length; i++) ClearSlot(i);
        SelectSlot(0);

        if (bigPhotoOnFishScreen != null) bigPhotoOnFishScreen.SetActive(false);
        if (bigPhotoOnHumanScreen != null) bigPhotoOnHumanScreen.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);

        if (Keyboard.current.enterKey.wasPressedThisFrame) UseItem(currentSelectedIndex);
        if (Keyboard.current.rKey.wasPressedThisFrame) TriggerActiveSkill();

        if (Keyboard.current.digit7Key.wasPressedThisFrame) ChangeRoleUI(PlayerRole.Fisherman);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) ChangeRoleUI(PlayerRole.Magician);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) ChangeRoleUI(PlayerRole.Inventor);
        if (Keyboard.current.digit0Key.wasPressedThisFrame) ChangeRoleUI(PlayerRole.TaiKe);
    }

    private void TriggerActiveSkill()
    {
        GameObject targetPlayer = GetLocalPlayerObject();
        if (targetPlayer == null) return;
        
        PlayerMovement pm = targetPlayer.GetComponent<PlayerMovement>();
        if (pm == null) pm = targetPlayer.GetComponentInChildren<PlayerMovement>();

        if (pm == null) return;

        if (currentRole == PlayerRole.Magician)
        {
            pm.ActivateInvisibility(magicianInvisDuration);
        }
        else if (currentRole == PlayerRole.Inventor)
        {
            pm.ActivateShield(inventorShieldDuration);
        }
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= itemIcons.Length) return;
        currentSelectedIndex = index;

        for (int i = 0; i < itemIcons.Length; i++)
        {
            if (itemIcons[i] != null)
            {
                Transform slotParent = itemIcons[i].transform.parent;
                if (slotParent != null)
                {
                    slotParent.localScale = i == currentSelectedIndex ? new Vector3(1.15f, 1.15f, 1f) : Vector3.one;
                }
            }
        }
    }

    public bool AddItem(Sprite newItemSprite)
    {
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (currentItems[i] == null)
            {
                currentItems[i] = newItemSprite;
                itemIcons[i].sprite = newItemSprite;
                itemIcons[i].color = new Color(1, 1, 1, 1);
                return true;
            }
        }
        return false;
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentItems.Length) return;

        Sprite itemToUse = currentItems[slotIndex];
        if (itemToUse == null) return;

        if (itemToUse == hourglassSprite)
        {
            if (GameTimer.Instance != null)
            {
                AddTimeServerRpc(addTimeSeconds);
                TriggerTimePopupEffect(addTimeSeconds);
            }
        }
        else if (itemToUse == shoeSprite)
        {
            GameObject targetPlayer = GetLocalPlayerObject();
            if (targetPlayer != null)
            {
                PlayerMovement pm = targetPlayer.GetComponent<PlayerMovement>();
                if (pm == null) pm = targetPlayer.GetComponentInChildren<PlayerMovement>();

                if (pm != null) pm.ActivateSpeedBoost(speedMultiplier, speedBoostDuration);
            }
        }
        else if (itemToUse == fishPhotoSprite)
        {
            if (photoCoroutine != null) StopCoroutine(photoCoroutine);
            photoCoroutine = StartCoroutine(ShowBigPhotoRoutine());
        }

        ClearSlot(slotIndex);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void AddTimeServerRpc(float amount)
    {
        if (GameTimer.Instance != null) GameTimer.Instance.AddGameTime(amount);
    }

    public void ChangeRoleUI(PlayerRole newRole)
    {
        currentRole = newRole;

        foreach (RoleUIProfile profile in roleProfiles)
        {
            if (profile.role == newRole)
            {
                if (profile.itemBarBackground != null && uiItemSlotBackgrounds != null)
                {
                    foreach (Image slotBg in uiItemSlotBackgrounds)
                    {
                        if (slotBg != null) slotBg.sprite = profile.itemBarBackground;
                    }
                }

                if (uiSkillButtonIconImage != null) uiSkillButtonIconImage.sprite = profile.skillButtonIcon;
                if (uiSkillButtonBgImage != null)
                {
                    uiSkillButtonBgImage.sprite = profile.skillButtonBgSprite;
                    uiSkillButtonBgImage.color = Color.white;
                }
                break;
            }
        }
    }
    
    private IEnumerator ShowBigPhotoRoutine()
    {
        RectTransform fishRect = bigPhotoOnFishScreen != null ? bigPhotoOnFishScreen.GetComponent<RectTransform>() : null;
        RectTransform humanRect = bigPhotoOnHumanScreen != null ? bigPhotoOnHumanScreen.GetComponent<RectTransform>() : null;

        if (targetScreen == PhotoTarget.FishScreen || targetScreen == PhotoTarget.BothScreens)
        {
            if (fishRect != null) fishRect.anchoredPosition = photoStartPos;
            if (bigPhotoOnFishScreen != null) bigPhotoOnFishScreen.SetActive(true);
        }

        if (targetScreen == PhotoTarget.HumanScreen || targetScreen == PhotoTarget.BothScreens)
        {
            if (humanRect != null) humanRect.anchoredPosition = photoStartPos;
            if (bigPhotoOnHumanScreen != null) bigPhotoOnHumanScreen.SetActive(true);
        }

        if (movementScriptToFreeze != null) movementScriptToFreeze.enabled = false;

        float timer = 0f;
        while (timer < slideInDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / slideInDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            if (fishRect != null && bigPhotoOnFishScreen.activeSelf)
                fishRect.anchoredPosition = Vector2.Lerp(photoStartPos, photoEndPos, smoothProgress);

            if (humanRect != null && bigPhotoOnHumanScreen.activeSelf)
                humanRect.anchoredPosition = Vector2.Lerp(photoStartPos, photoEndPos, smoothProgress);

            yield return null;
        }

        if (fishRect != null && bigPhotoOnFishScreen.activeSelf) fishRect.anchoredPosition = photoEndPos;
        if (humanRect != null && bigPhotoOnHumanScreen.activeSelf) humanRect.anchoredPosition = photoEndPos;

        float waitTime = photoDuration - slideInDuration;
        if (waitTime > 0) yield return new WaitForSeconds(waitTime);

        if (bigPhotoOnFishScreen != null) bigPhotoOnFishScreen.SetActive(false);
        if (bigPhotoOnHumanScreen != null) bigPhotoOnHumanScreen.SetActive(false);

        if (movementScriptToFreeze != null) movementScriptToFreeze.enabled = true;

        photoCoroutine = null;
    }

    private void TriggerTimePopupEffect(float amount)
    {
        if (timePopupPrefab == null || timeTextReferencePoint == null) return;

        Vector2 referencePos = timeTextReferencePoint.anchoredPosition;
        Vector2 spawnPosition = referencePos + new Vector2(100f, -50f);

        GameObject popup = Instantiate(timePopupPrefab, timeTextReferencePoint.parent);
        RectTransform popupRect = popup.GetComponent<RectTransform>();

        if (popupRect != null) popupRect.anchoredPosition = spawnPosition;

        Text textComponent = popup.GetComponent<Text>();
        if (textComponent != null) textComponent.text = $"+{Mathf.Ceil(amount)}";
    }

    private void ClearSlot(int index)
    {
        currentItems[index] = null;
        itemIcons[index].sprite = null;
        itemIcons[index].color = new Color(1, 1, 1, 0);
    }

    public void SetActionButtonHighlight(bool isHighlighted)
    {
        if (skillButtonTransform != null)
        {
            skillButtonTransform.localScale = isHighlighted ? new Vector3(1.15f, 1.15f, 1f) : Vector3.one;
        }
    }

    private GameObject GetLocalPlayerObject()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                return NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            }
        }
        return playerObject;
    }
}