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


    // ✅ Método público para forzar reasignación de tiles (útil para retry/level change)
    public void ReassignAllEnemyTiles()
    {
        if (enemies == null || enemies.Length == 0) return;

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.AssignCurrentTile();
            }
        }

        Debug.Log($"[LevelDefinition '{name}'] Reassigned tiles to all enemies.");
    }
}