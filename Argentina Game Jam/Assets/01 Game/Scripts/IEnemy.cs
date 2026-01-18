using UnityEngine;

/// <summary>
/// Interfaz que todos los enemigos deben implementar
/// </summary>
public interface IEnemy
{
    // Propiedades básicas
    int Health { get; }
    int InitialHealth { get; }
    bool IsDead { get; }

    // Tiles
    Tile CurrentTile { get; }
    Tile InitialTile { get; }

    // Transform (para acceder a la posición)
    Transform Transform { get; }

    // Métodos principales
    void TakeDamage(int amount);
    void ResetEnemy();
    void AssignInitialTile();
    void AssignCurrentTile();

    // GameObject reference
    GameObject GameObject { get; }
}
