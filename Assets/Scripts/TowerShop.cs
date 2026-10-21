using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerShop : MonoBehaviour
{
    [SerializeField] private TowerManager towerManager;
    [SerializeField] private PathManager pathManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerHealth playerHealth;

    private const float PanelMargin = 12f;
    private const float CameraGap = 12f;
    private const float PanelHeight = 454f;
    private TowerKind? selectedKind;
    private string message = "Choose a tower, then click an open tile to place it.";
    private Camera gameplayCamera;
    private Rect originalCameraRect;
    private float originalOrthographicSize;
    private int lastScreenWidth;

    private void Awake()
    {
        if (towerManager == null)
        {
            towerManager = FindAnyObjectByType<TowerManager>();
        }

        if (pathManager == null)
        {
            pathManager = FindAnyObjectByType<PathManager>();
        }

        if (waveManager == null)
        {
            waveManager = FindAnyObjectByType<WaveManager>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        gameplayCamera = Camera.main;
        if (gameplayCamera != null)
        {
            originalCameraRect = gameplayCamera.rect;
            originalOrthographicSize = gameplayCamera.orthographicSize;
            UpdateCameraViewport();
        }
    }

    private void Update()
    {
        if (gameplayCamera != null && lastScreenWidth != Screen.width)
        {
            UpdateCameraViewport();
        }

        if (selectedKind.HasValue &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            selectedKind = null;
            message = "Placement cancelled.";
        }
    }

    private void OnGUI()
    {
        if (Time.timeScale <= 0f ||
            (playerHealth != null && playerHealth.currentHealth <= 0))
        {
            return;
        }

        float uiScale = Mathf.Min(1f, Mathf.Max(0.72f, (Screen.height - 24f) / PanelHeight));
        Rect panelRect = GetPanelRect();
        GUI.Box(panelRect, "Tower Defense");

        int currency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;
        int health = playerHealth != null ? playerHealth.currentHealth : 0;
        int maxHealth = playerHealth != null ? playerHealth.maxHealth : 0;
        int wave = waveManager != null ? waveManager.currentWave : 1;

        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 31f * uiScale, panelRect.width - 28f, 22f * uiScale),
            "$ " + currency);
        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 54f * uiScale, panelRect.width - 28f, 22f * uiScale),
            "Base " + health + "/" + maxHealth);
        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 77f * uiScale, panelRect.width - 28f, 22f * uiScale),
            "Wave " + wave);

        DrawTowerButton(TowerKind.Dart, "tower1", "$100 | Balanced", 106f, uiScale, currency);
        DrawTowerButton(TowerKind.Rapid, "tower2", "$160 | Fast", 174f, uiScale, currency);
        DrawTowerButton(TowerKind.Cannon, "tower3", "$220 | Splash", 242f, uiScale, currency);

        if (waveManager != null && waveManager.IsIntermission)
        {
            int seconds = Mathf.CeilToInt(waveManager.IntermissionRemaining);
            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 312f * uiScale,
                panelRect.width - 28f, 22f * uiScale),
                "Next wave in " + seconds + "s");

            if (GUI.Button(new Rect(panelRect.x + 14f, panelRect.y + 336f * uiScale,
                    panelRect.width - 28f, 30f * uiScale),
                    "Start next wave early (+$20)"))
            {
                waveManager.StartNextWaveEarly();
                message = "Early start bonus earned.";
            }
        }
        else if (waveManager != null)
        {
            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 312f * uiScale,
                panelRect.width - 28f, 22f * uiScale),
                waveManager.IsSpawning
                    ? "Enemies remaining: " + waveManager.enemiesRemaining
                    : "Preparing the next wave...");
        }

        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 376f * uiScale,
            panelRect.width - 28f, 54f * uiScale), message);

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
        {
            selectedKind = null;
            message = "Placement cancelled.";
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDown &&
                 currentEvent.button == 0 &&
                 selectedKind.HasValue &&
                 !panelRect.Contains(currentEvent.mousePosition) &&
                 !IsPointerOverUi())
        {
            PlaceSelectedTower(currentEvent.mousePosition);
            currentEvent.Use();
        }
    }

    private void DrawTowerButton(
        TowerKind kind,
        string name,
        string details,
        float y,
        float uiScale,
        int currency)
    {
        Rect panelRect = GetPanelRect();
        Rect buttonRect = new Rect(
            panelRect.x + 12f,
            panelRect.y + y * uiScale,
            panelRect.width - 24f,
            62f * uiScale
        );
        GUI.enabled = currency >= Tower.GetCost(kind) && Time.timeScale > 0f;
        bool clicked = GUI.Button(buttonRect, GUIContent.none);
        GUI.enabled = true;

        if (selectedKind == kind)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.55f, 0.8f, 1f, 0.55f);
            GUI.Box(buttonRect, GUIContent.none);
            GUI.color = previousColor;
        }

        DrawTowerPreview(kind, new Rect(
            buttonRect.x + 8f,
            buttonRect.y + 4f * uiScale,
            40f,
            54f * uiScale
        ));
        GUI.Label(new Rect(buttonRect.x + 56f, buttonRect.y + 10f * uiScale,
            buttonRect.width - 62f, 22f * uiScale),
            name);
        GUI.Label(new Rect(buttonRect.x + 56f, buttonRect.y + 32f * uiScale,
            buttonRect.width - 62f, 22f * uiScale),
            details);

        if (clicked)
        {
            selectedKind = kind;
            message = "Click an open tile to place. Esc or right-click cancels.";
        }
    }

    private void DrawTowerPreview(TowerKind kind, Rect previewRect)
    {
        Sprite sprite = towerManager != null ? towerManager.TowerSprite : null;
        if (sprite == null)
        {
            return;
        }

        Rect spriteRect = sprite.textureRect;
        Rect textureCoordinates = new Rect(
            spriteRect.x / sprite.texture.width,
            spriteRect.y / sprite.texture.height,
            spriteRect.width / sprite.texture.width,
            spriteRect.height / sprite.texture.height
        );
        Color previousColor = GUI.color;
        GUI.color = GetTowerColor(kind);
        GUI.DrawTextureWithTexCoords(previewRect, sprite.texture, textureCoordinates, true);
        GUI.color = previousColor;
    }

    private void PlaceSelectedTower(Vector2 guiPosition)
    {
        if (Time.timeScale <= 0f)
        {
            message = "Resume the game before placing towers.";
            return;
        }

        if (towerManager == null || pathManager == null ||
            pathManager.pathTilemap == null)
        {
            message = "Tower placement is not configured.";
            Debug.LogError(message);
            return;
        }

        if (gameplayCamera == null)
        {
            message = "A main camera is required to place towers.";
            Debug.LogError(message);
            return;
        }

        Vector2 screenPoint = new Vector2(guiPosition.x, Screen.height - guiPosition.y);
        if (!gameplayCamera.pixelRect.Contains(screenPoint))
        {
            return;
        }

        Vector3 screenPosition = new Vector3(
            screenPoint.x,
            screenPoint.y,
            -gameplayCamera.transform.position.z
        );
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPosition);
        Vector3Int cell = pathManager.pathTilemap.WorldToCell(worldPosition);
        Vector3 snappedPosition = pathManager.pathTilemap.GetCellCenterWorld(cell);
        snappedPosition.z = 0f;

        if (!towerManager.TryPlaceTower(selectedKind.Value, snappedPosition, out string reason))
        {
            message = reason;
            return;
        }

        message = "Tower placed. Select another or keep building.";
    }

    private Rect GetPanelRect()
    {
        float panelWidth = GetPanelWidth();
        float uiScale = Mathf.Min(1f, Mathf.Max(0.72f, (Screen.height - 24f) / PanelHeight));
        return new Rect(
            Screen.width - panelWidth - PanelMargin,
            PanelMargin,
            panelWidth,
            PanelHeight * uiScale
        );
    }

    private void UpdateCameraViewport()
    {
        float panelWidth = GetPanelWidth();
        float availableWidth = Mathf.Max(1f, Screen.width - panelWidth - PanelMargin - CameraGap);
        float widthRatio = availableWidth / Mathf.Max(1f, Screen.width);

        gameplayCamera.rect = new Rect(
            originalCameraRect.x,
            originalCameraRect.y,
            originalCameraRect.width * widthRatio,
            originalCameraRect.height
        );

        if (gameplayCamera.orthographic)
        {
            gameplayCamera.orthographicSize =
                originalOrthographicSize / widthRatio;
        }

        lastScreenWidth = Screen.width;
    }

    private static float GetPanelWidth()
    {
        return Mathf.Min(260f, Mathf.Max(190f, Screen.width * 0.32f));
    }

    private static Color GetTowerColor(TowerKind kind)
    {
        switch (kind)
        {
            case TowerKind.Rapid:
                return new Color(0.45f, 0.85f, 1f);
            case TowerKind.Cannon:
                return new Color(1f, 0.65f, 0.35f);
            default:
                return Color.white;
        }
    }

    private void OnDestroy()
    {
        if (gameplayCamera != null)
        {
            gameplayCamera.rect = originalCameraRect;
            gameplayCamera.orthographicSize = originalOrthographicSize;
        }
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();
    }
}
