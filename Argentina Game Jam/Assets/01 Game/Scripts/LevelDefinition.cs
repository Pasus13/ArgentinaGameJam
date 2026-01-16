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
    [Tooltip("Optional: Enemies that belong to this level. If empty, they will be auto-detected.")]
    public EnemyUnit[] enemies;

    private void Awake()
    {
        // This is NOT game logic, just data convenience
        if  (enemies == null || enemies.Length == 0)
        {
            enemies = GetComponentsInChildren<EnemyUnit>(true);
        }
    }
}

