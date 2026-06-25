using UnityEngine;
using UnityEngine.InputSystem; // 必須引入新版輸入系統

public class ToolItem : MonoBehaviour
{
    [Header("工具設定")]
    public string toolID; // 在 Inspector 輸入對應的 ID，例如：Brush, Wrench 等

    private bool isDragging = false;
    private Collider2D myCollider;
    private Camera mainCamera;

    void Start()
    {
        myCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main; // 將攝影機存入變數，避免 Update 中重複耗能尋找
        
        // 防呆檢查：確保場景中有設定 MainCamera 標籤的攝影機
        if (mainCamera == null)
        {
            Debug.LogError("錯誤：場景中沒有 Tag 為 'MainCamera' 的攝影機！");
        }
    }

    void Update()
    {
        // 防呆機制：確保滑鼠與攝影機確實存在於場景中才執行
        if (Mouse.current == null || mainCamera == null) return;

        // 1. 偵測滑鼠左鍵「按下瞬間」
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 透過新版輸入系統取得滑鼠的螢幕座標，並轉換為世界座標
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            // 檢查滑鼠點擊的世界座標位置，是否重疊在目前工具的碰撞器上
            if (myCollider == Physics2D.OverlapPoint(mousePos2D))
            {
                isDragging = true;
                Debug.Log("成功抓到工具：" + gameObject.name);
            }
        }

        // 2. 偵測滑鼠左鍵「放開瞬間」
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        // 3. 執行拖拽邏輯
        if (isDragging)
        {
            // 持續取得最新的滑鼠座標並轉換
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            
            // 更新工具的位置，同時強制將 Z 軸維持在 0，防止工具因為 Z 軸偏移而消失在畫面中
            transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
        }
    }
}