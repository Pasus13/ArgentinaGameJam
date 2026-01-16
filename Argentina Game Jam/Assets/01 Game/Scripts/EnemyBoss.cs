using System.Collections;
using UnityEngine;

public class EnemyBoss : EnemyUnit
{
    [Header("Boss Settings")]
    [Tooltip("Distancia a la que se activa el aggro del boss")]
    public int aggroRange = 3;

    [Tooltip("Si está en modo aggro (persiguiendo activamente)")]
    public bool isAggro = false;

    [Header("Grab Settings")]
    [Tooltip("El boss puede agarrar al player si termina adyacente (8D) después de su primer movimiento")]
    public GameObject grabEffectPrefab;
    public float grabEffectDuration = 2f;

    protected override void Awake()
    {
        stepsPerTurn = 2; // ✅ EnemyBoss se mueve 2 casillas por turno cuando está aggro
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        isAggro = false; // Asegurar que empieza sin aggro
        DebugLog($"EnemyBoss initialized - AggroRange: {aggroRange}, stepsPerTurn: {stepsPerTurn}");
    }

    protected override void OnResetEnemy()
    {
        isAggro = false; // ✅ Resetear aggro al reiniciar nivel
        DebugLog("Boss aggro reset to false");
    }

    protected override IEnumerator DecideBehavior()
    {
        if (currentTile == null)
        {
            DebugLog("ERROR: currentTile is NULL. Cannot take turn.");
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

        // 1️⃣ COMPROBAR AGARRE AL INICIO DEL TURNO (antes de moverse)
        bool grabbedAtStart = false;
        yield return CheckGrabPlayer((grabbed) => grabbedAtStart = grabbed);

        if (grabbedAtStart)
        {
            DebugLog("💀 Player grabbed at turn start! Game Over.");
            yield break; // El juego terminó, no continuar
        }

        // 2️⃣ CALCULAR DISTANCIA AL JUGADOR
        int distance = GetDistanceToPlayer();
        DebugLog($"Distance to player: {distance}, Aggro: {isAggro}");

        // 3️⃣ ACTIVAR AGGRO SI ESTÁ DENTRO DEL RANGO (solo si aún no está activado)
        if (!isAggro && distance <= aggroRange)
        {
            isAggro = true;
            DebugLog("⚠️ BOSS AGGRO ACTIVATED! (will stay active until level reset)");

            // Reproducir efecto visual/sonido de aggro
            if (AudioManager.Instance != null)
            {
                AudioClip roarClip = AudioManager.Instance.pushEnemyClip; // Usa el clip que prefieras
                AudioManager.Instance.PlaySFXPitchVariability(roarClip);
            }
        }

        // 4️⃣ SI NO ESTÁ AGGRO, NO SE MUEVE
        if (!isAggro)
        {
            DebugLog("😴 Boss is idle (not aggro yet)");
            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        // 5️⃣ SI ESTÁ AGGRO, PERSEGUIR CON MÚLTIPLES PASOS
        int executedSteps = 0;

        for (int step = 0; step < stepsPerTurn; step++)
        {
            if (currentTile == null)
            {
                DebugLog("ERROR: currentTile became NULL mid-turn.");
                break;
            }

            Vector2Int myPos = currentTile.gridPos;
            Vector2Int playerPos = player.currentTile.gridPos;

            // Si ya está adyacente (4D), no seguir moviéndose
            if (BoardManager.Instance.AreAdjacent4D(myPos, playerPos))
            {
                DebugLog($"Step {executedSteps}: Now adjacent to player (4D). Stopping movement.");
                break;
            }

            DebugLog($"👊 Boss Step {step + 1}/{stepsPerTurn}");
            yield return MoveOneStepTowardsPlayer();
            executedSteps++;

            // 6️⃣ COMPROBAR AGARRE DESPUÉS DEL PRIMER MOVIMIENTO (no después del segundo)
            if (executedSteps == 1)
            {
                bool grabbed = false;
                yield return CheckGrabPlayer((result) => grabbed = result);

                if (grabbed)
                {
                    DebugLog("💀 Player grabbed after first movement! Game Over.");
                    yield break; // Terminó el juego, no hacer el segundo movimiento
                }
            }

            // Pausa breve entre pasos
            if (executedSteps < stepsPerTurn)
                yield return new WaitForSeconds(0.1f);
        }

        DebugLog($"Boss turn complete. Steps executed: {executedSteps}/{stepsPerTurn}");
    }

    /// <summary>
    /// Comprueba si el boss puede agarrar al player (distancia 8D adyacente).
    /// Usa un callback para retornar el resultado (true si agarró al player).
    /// </summary>
    private IEnumerator CheckGrabPlayer(System.Action<bool> onComplete)
    {
        var gm = GameManager.Instance;
        var player = gm?.player;

        if (player == null || currentTile == null || player.currentTile == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        Vector2Int myPos = currentTile.gridPos;
        Vector2Int playerPos = player.currentTile.gridPos;

        // Comprobar si está adyacente en 8 direcciones (8D)
        bool isAdjacent8D = BoardManager.Instance.AreAdjacent8D(myPos, playerPos);

        if (isAdjacent8D)
        {
            DebugLog("💀💀💀 BOSS GRABBED THE PLAYER! 💀💀💀");

            // Reproducir animación de agarre
            if (_animController != null)
                _animController.PlayAttack(); // O puedes crear una animación específica "Grab"

            // Efecto visual de agarre
            if (grabEffectPrefab != null)
            {
                var fx = Instantiate(grabEffectPrefab, player.transform.position, Quaternion.identity);
                Destroy(fx, grabEffectDuration);
            }

            yield return new WaitForSeconds(0.5f);

            // ⚠️ ACTIVAR GAME OVER
            gm.Lose("¡El Boss te ha atrapado!");

            onComplete?.Invoke(true); // Agarró al player
        }
        else
        {
            onComplete?.Invoke(false); // No lo agarró
        }
    }
}