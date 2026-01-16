using System.Collections.Generic;
using UnityEngine;

public class LevelDefinition : MonoBehaviour
{
    [Header("Level Tiles")]
    [Tooltip("Tile where the player starts in this level")]
    public Tile startTile;

    [Tooltip("Tile that ends the level")]
    public Tile goalTile;

    public Transform anchor;

    [Header("Level Enemies")]
    [Tooltip("Enemies that belong to this level")]
    public EnemyUnit[] enemies;

    private void Awake()
    {
        // ✅ Solo detectar si el array está vacío
        if (enemies == null || enemies.Length == 0)
        {
            EnemiesDetection();
        }

        Debug.Log($"✅ [LevelDefinition '{name}'] Awake complete - {enemies.Length} enemies ready");
    }

    private void Start()
    {
        // ✅ IMPORTANTE: Asegurarse de que todos los tiles están asignados
        // Esto se ejecuta DESPUÉS de que BoardManager construya el grid
        EnsureEnemyTilesAssigned();
    }

    public void EnemiesDetection()
    {
        // ✅ Si ya están detectados, no volver a buscar
        if (enemies != null && enemies.Length > 0)
        {
            Debug.Log($"[LevelDefinition '{name}'] Enemies already detected ({enemies.Length}). Skipping search.");
            return;
        }

        List<EnemyUnit> foundEnemies = new List<EnemyUnit>();

        // Buscar cada tipo concreto de enemigo
        foundEnemies.AddRange(GetComponentsInChildren<EnemySleeper>(true));
        foundEnemies.AddRange(GetComponentsInChildren<EnemyChaser>(true));
        foundEnemies.AddRange(GetComponentsInChildren<EnemyBoss>(true));

        enemies = foundEnemies.ToArray();

        Debug.Log($"✅ [LevelDefinition '{name}'] Detected {enemies.Length} enemies:");

        foreach (var enemy in enemies)
        {
            if (enemy != null)
                Debug.Log($"   - {enemy.name} ({enemy.GetType().Name})");
        }
    }

    // ✅ NUEVO MÉTODO: Asegurar que todos los enemigos tienen tiles asignados
    private void EnsureEnemyTilesAssigned()
    {
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning($"[LevelDefinition '{name}'] No enemies to assign tiles to.");
            return;
        }

        Debug.Log($"[LevelDefinition '{name}'] Ensuring tiles are assigned to {enemies.Length} enemies...");

        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                Debug.LogWarning($"[LevelDefinition '{name}'] Found null enemy in array!");
                continue;
            }

            // Si el enemigo no tiene tile asignado, forzar asignación
            if (enemy.currentTile == null || enemy.initalTile == null)
            {
                Debug.Log($"   - Assigning tile to {enemy.name}...");
                enemy.AutoAssignTile();

                if (enemy.currentTile != null)
                {
                    Debug.Log($"   ✅ {enemy.name} assigned to tile {enemy.currentTile.gridPos}");
                }
                else
                {
                    Debug.LogError($"   ❌ {enemy.name} FAILED to assign tile!");
                }
            }
            else
            {
                Debug.Log($"   ✅ {enemy.name} already has tile: {enemy.currentTile.gridPos}");
            }
        }
    }

    // ✅ Método público para forzar reasignación de tiles (útil para retry/level change)
    public void ReassignAllEnemyTiles()
    {
        if (enemies == null || enemies.Length == 0) return;

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.AutoAssignTile();
            }
        }

        Debug.Log($"[LevelDefinition '{name}'] Reassigned tiles to all enemies.");
    }
}