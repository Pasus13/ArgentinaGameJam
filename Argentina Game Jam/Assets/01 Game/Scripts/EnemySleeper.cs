using System.Collections;
using UnityEngine;

public class EnemySleeper : EnemyUnit
{
    [Header("Sleeper Settings")]
    [Tooltip("Animación o efecto visual cuando está durmiendo")]
    public GameObject sleepEffectPrefab;

    protected override void Awake()
    {
        stepsPerTurn = 0; // ✅ EnemySleeper no se mueve nunca
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Opcional: activar efecto visual de "zzz"
        if (sleepEffectPrefab != null)
        {
            Instantiate(sleepEffectPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
        }

        DebugLog($"EnemySleeper initialized - stepsPerTurn: {stepsPerTurn}");
    }

    protected override IEnumerator DecideBehavior()
    {
        DebugLog("💤 Sleeping... zzz");

        // El enemigo no hace nada, solo espera un momento
        yield return new WaitForSeconds(0.1f);

        // No se mueve, no ataca, solo duerme
    }
}