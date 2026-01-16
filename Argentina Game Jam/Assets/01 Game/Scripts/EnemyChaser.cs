using System.Collections;
using UnityEngine;

public class EnemyChaser : EnemyUnit
{
    protected override void Awake()
    {
        stepsPerTurn = 1; // ✅ EnemyChaser se mueve 1 casilla por turno
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        DebugLog($"EnemyChaser initialized - stepsPerTurn: {stepsPerTurn}");
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

        Vector2Int myPos = currentTile.gridPos;
        Vector2Int playerPos = player.currentTile.gridPos;

        // Comprobar si ya está adyacente (4D) - si lo está, no se mueve
        if (BoardManager.Instance != null && BoardManager.Instance.AreAdjacent4D(myPos, playerPos))
        {
            DebugLog("🎯 Adjacent to player (4D) -> staying still");
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        // Moverse según los pasos configurados
        int executedSteps = 0;

        for (int step = 0; step < stepsPerTurn; step++)
        {
            if (currentTile == null)
            {
                DebugLog("ERROR: currentTile became NULL mid-turn.");
                break;
            }

            // Recalcular posiciones por si cambió algo
            myPos = currentTile.gridPos;
            playerPos = player.currentTile.gridPos;

            // Si ya está adyacente, dejar de moverse
            if (BoardManager.Instance.AreAdjacent4D(myPos, playerPos))
            {
                DebugLog($"Step {executedSteps}: Now adjacent to player. Stopping.");
                break;
            }

            DebugLog($"Step {step + 1}/{stepsPerTurn}");
            yield return MoveOneStepTowardsPlayer();
            executedSteps++;

            // Pausa breve entre pasos si hace múltiples
            if (executedSteps < stepsPerTurn)
                yield return new WaitForSeconds(0.05f);
        }

        DebugLog($"Turn complete. Steps executed: {executedSteps}/{stepsPerTurn}");
    }
}
