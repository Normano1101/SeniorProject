using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerShop : MonoBehaviour
{
    [SerializeField] private TowerManager towerManager;
    [SerializeField] private PathManager pathManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerHealth playerHealth;

    private static readonly Rect PanelRect = new Rect(12f, 12f, 294f, 292f);
    private TowerKind? selectedKind;
    private string message = "Choose a tower, then click an open tile to place it.";

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
    }

    private void Update()
    {
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

        GUI.Box(PanelRect, "Tower Defense");

        int currency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;
        int health = playerHealth != null ? playerHealth.currentHealth : 0;
        int maxHealth = playerHealth != null ? playerHealth.maxHealth : 0;
        int wave = waveManager != null ? waveManager.currentWave : 1;

        GUI.Label(new Rect(PanelRect.x + 14f, 43f, 250f, 22f), "$ " + currency);
        GUI.Label(new Rect(PanelRect.x + 150f, 43f, 130f, 22f),
            "Base " + health + "/" + maxHealth);
        GUI.Label(new Rect(PanelRect.x + 14f, 66f, 250f, 22f), "Wave " + wave);

        DrawTowerButton(TowerKind.Dart, "tower1  $100", 92f, currency);
        DrawTowerButton(TowerKind.Rapid, "tower2  $160", 130f, currency);
        DrawTowerButton(TowerKind.Cannon, "tower3  $220", 168f, currency);

        if (waveManager != null && waveManager.IsIntermission)
        {
            int seconds = Mathf.CeilToInt(waveManager.IntermissionRemaining);
            GUI.Label(new Rect(PanelRect.x + 14f, 207f, 250f, 22f),
                "Next wave in " + seconds + "s");

            if (GUI.Button(new Rect(PanelRect.x + 14f, 231f, 264f, 30f),
                    "Start next wave early (+$20)"))
            {
                waveManager.StartNextWaveEarly();
                message = "Early start bonus earned.";
            }
        }
        else if (waveManager != null)
        {
            GUI.Label(new Rect(PanelRect.x + 14f, 207f, 250f, 22f),
                waveManager.IsSpawning
                    ? "Enemies remaining: " + waveManager.enemiesRemaining
                    : "Preparing the next wave...");
        }

        GUI.Label(new Rect(PanelRect.x + 14f, 264f, 264f, 40f), message);

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
                 !PanelRect.Contains(currentEvent.mousePosition) &&
                 !IsPointerOverUi())
        {
            PlaceSelectedTower(currentEvent.mousePosition);
            currentEvent.Use();
        }
    }

    private void DrawTowerButton(TowerKind kind, string label, float y, int currency)
    {
        GUI.enabled = currency >= Tower.GetCost(kind) && Time.timeScale > 0f;
        bool clicked = GUI.Button(
            new Rect(PanelRect.x + 14f, y, 264f, 32f),
            label
        );
        GUI.enabled = true;

        if (clicked)
        {
            selectedKind = kind;
            message = "Click an open tile to place. Esc or right-click cancels.";
        }
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

        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            message = "A main camera is required to place towers.";
            Debug.LogError(message);
            return;
        }

        Vector3 screenPosition = new Vector3(
            guiPosition.x,
            Screen.height - guiPosition.y,
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

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();
    }
}
