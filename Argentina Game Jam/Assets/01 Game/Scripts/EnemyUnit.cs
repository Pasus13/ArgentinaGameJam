using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyActions))]
public abstract class EnemyUnit : MonoBehaviour, IEnemy
{
    [Header("Stats")]
    public int health;
    public int initialHealth;
    public Vector3 initialPos;

    [Header("Turn Behavior")]
    [Tooltip("Número de pasos que da este enemigo por turno (0 = no se mueve)")]
    public int stepsPerTurn = 1;

    [Header("Attack")]
    public int attackHeatDamage = 5;
    public GameObject attackEffectPrefab;
    public float attackEffectDuration = 5f;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 720f;

    [Header("Tiles")]
    public Tile initalTile;
    public Tile currentTile;

    [Header("Debug")]
    public bool showDebugLogs = true;

    protected GameObject _visualMesh;
    protected EnemyActions _actions;
    protected EnemyAnimationController _animController;
    protected bool _isExecutingTurn;
    protected FootstepEmitter footsStepScript;

    public bool IsDead => health <= 0;

    // ✅ NUEVO: Propiedades de la interfaz
    public int Health => health;
    public int InitialHealth => initialHealth;
    public Tile CurrentTile => currentTile;
    public Tile InitialTile => initalTile;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    protected virtual void Awake()
    {
        _actions = GetComponent<EnemyActions>();
        _animController = GetComponent<EnemyAnimationController>();

        if (_animController == null)
        {
            DebugLog("WARNING: No EnemyAnimationController found.");
        }

        if (transform.childCount > 0)
        {
            _visualMesh = transform.GetChild(0).gameObject;
        }

        initialPos = transform.position;
        health = initialHealth;
    }

    protected virtual void Start()
    {
        footsStepScript = GetComponent<FootstepEmitter>();

        // ✅ Asegurar que el tile está asignado (por si BoardManager no existía en Awake)
        if (currentTile == null || initalTile == null)
        {
            AutoAssignTile();
        }

        DebugLog($"Initial Tile assigned: {initalTile?.gridPos}");
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        DebugLog($"Enemy took damage: {amount}. HP: {health}");

        if (health <= 0)
        {
            this.gameObject.SetActive(false);
        }
    }

    public void ResetEnemy()
    {
        gameObject.SetActive(true);

        health = initialHealth;
        _isExecutingTurn = false;
        transform.position = initialPos;
        currentTile = initalTile;

        if (_animController != null)
        {
            _animController.ResetToIdle();
        }

        // Permitir que las clases derivadas sobrescriban comportamiento adicional
        OnResetEnemy();
    }

    // ✅ Método virtual para que las clases derivadas puedan agregar lógica de reset
    protected virtual void OnResetEnemy()
    {
        // Las clases derivadas pueden sobrescribir esto
    }

    public IEnumerator TakeTurnCoroutine()
    {
        if (_isExecutingTurn)
        {
            DebugLog("WARNING: Turn already executing. Aborting.");
            yield break;
        }

        if (IsDead)
        {
            DebugLog("INFO: Enemy is dead. Skipping turn.");
            yield break;
        }

        _isExecutingTurn = true;

        // Ejecutar comportamiento específico de cada tipo de enemigo
        yield return DecideBehavior();

        _isExecutingTurn = false;
    }

    // ✅ MÉTODO ABSTRACTO: Cada tipo de enemigo lo implementa
    protected abstract IEnumerator DecideBehavior();

    // ✅ MÉTODO HELPER: Mover un paso usando A*
    protected IEnumerator MoveOneStepTowardsPlayer()
    {
        if (currentTile == null)
        {
            DebugLog("ERROR: currentTile is NULL.");
            yield break;
        }

        var gm = GameManager.Instance;
        if (gm == null)
        {
            DebugLog("ERROR: GameManager.Instance is NULL.");
            yield break;
        }

        var player = gm.player;
        if (player == null || player.currentTile == null)
        {
            DebugLog("ERROR: Player or Player.currentTile is NULL.");
            yield break;
        }

        Vector2Int myPos = currentTile.gridPos;
        Vector2Int playerPos = player.currentTile.gridPos;

        bool IsBlocked(Vector2Int pos)
        {
            Tile t = BoardManager.Instance.GetTile(pos);
            return t != null && IsTileOccupiedByOtherEnemy(t);
        }

        if (AStarPathfinder.TryGetNextStepTowardPlayerAdj(
                start: myPos,
                playerPos: playerPos,
                isBlocked: IsBlocked,
                nextStep: out Vector2Int nextStep,
                pathLength: out int pathLen))
        {
            // Seguridad: nunca moverse al tile del player
            if (nextStep == playerPos)
            {
                DebugLog("Safety: nextStep equals playerPos. Aborting movement.");
                yield break;
            }

            Tile nextTile = BoardManager.Instance.GetTile(nextStep);
            if (nextTile != null)
            {
                DebugLog($"Moving to {nextStep} (pathLen={pathLen})");
                yield return _actions.MoveToTileCoroutine(nextTile);
                footsStepScript?.Step();
            }
            else
            {
                DebugLog("ERROR: Next step tile resolved to NULL.");
            }
        }
        else
        {
            DebugLog("No valid A* move found.");
        }
    }

    protected bool IsTileOccupiedByOtherEnemy(Tile tile)
    {
        var gm = GameManager.Instance;
        if (gm == null || tile == null) return false;

        foreach (var enemy in gm.enemies)
        {
            if (enemy == null || enemy.IsDead || enemy == this) continue;
            if (enemy.currentTile == tile) return true;
        }

        return false;
    }

    protected int GetDistanceToPlayer()
    {
        var player = GameManager.Instance?.player;
        if (player == null || currentTile == null || player.currentTile == null)
            return 999;

        Vector2Int myPos = currentTile.gridPos;
        Vector2Int playerPos = player.currentTile.gridPos;

        // Distancia Manhattan (4D)
        return Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y);
    }

    public void AutoAssignTile()
    {
        var bm = BoardManager.Instance;
        if (bm == null)
        {
            DebugLog("AutoAssignTile: BoardManager.Instance is null.");
            return;
        }

        Tile t = bm.FindClosestTile(transform.position);

        if (t == null)
        {
            DebugLog($"AutoAssignTile FAILED: no tile found near position {transform.position}");
            return;
        }

        initalTile = t;
        currentTile = t;

        DebugLog($"AutoAssignTile SUCCESS: assigned to tile {t.gridPos}");
    }

    public void DebugLog(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[{name}] {message}");
    }
}