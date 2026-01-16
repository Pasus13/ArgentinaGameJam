using UnityEngine;

public class HeatFeedback : MonoBehaviour
{
    [Header("Termómetro UI")]
    [Tooltip("El transform del eje que rotará (la aguja del termómetro)")]
    public Transform ejeTermometro;

    [Tooltip("Rotación inicial del termómetro (0% calor)")]
    public float minRotationZ = 0f;

    [Tooltip("Rotación máxima del termómetro (100% calor)")]
    public float maxRotationZ = 180f;

    [Tooltip("Velocidad de suavizado de la rotación")]
    [Range(1f, 20f)]
    public float rotationSpeed = 8f;

    [Header("Color del Personaje")]
    [Tooltip("Material del jugador")]
    public Material playerMaterial;

    [Tooltip("Color cuando el calor está al máximo")]
    public Color overheatColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Tooltip("Velocidad de transición del color")]
    [Range(1f, 20f)]
    public float colorSpeed = 5f;

    [Header("🔥 Fuego Visual (UI)")]
    [Tooltip("GameObject con la animación de fuego")]
    public GameObject fireVfxRoot;

    [Tooltip("CanvasGroup del fuego para fade suave (opcional)")]
    public CanvasGroup fireVfxCanvasGroup;

    [Tooltip("Porcentaje de calor necesario para activar el fuego (0-100%)")]
    [Range(0f, 100f)]
    public float fireActivationPercent = 70f;

    [Tooltip("Velocidad del fade del fuego")]
    [Range(1f, 30f)]
    public float fireVisualFadeSpeed = 10f;

    [Header("🔥 Fuego Audio")]
    [Tooltip("AudioSource con loop de fuego")]
    public AudioSource fireLoopSource;

    [Tooltip("Volumen máximo del loop de fuego")]
    [Range(0f, 1f)]
    public float fireMaxVolume = 0.8f;

    [Tooltip("Velocidad del cambio de volumen")]
    [Range(1f, 30f)]
    public float fireVolumeSpeed = 10f;

    // Shader properties
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Color _originalColor;
    private bool _hasInitializedColor;

    private float _targetRotationZ;
    private float _currentRotationZ;

    private void Awake()
    {
        // Inicializar rotación
        if (ejeTermometro != null)
        {
            _currentRotationZ = minRotationZ;
            Vector3 rot = ejeTermometro.localEulerAngles;
            ejeTermometro.localEulerAngles = new Vector3(rot.x, rot.y, minRotationZ);
        }

        // Guardar color original
        if (playerMaterial != null)
        {
            if (playerMaterial.HasProperty(BaseColorId))
                _originalColor = playerMaterial.GetColor(BaseColorId);
            else if (playerMaterial.HasProperty(ColorId))
                _originalColor = playerMaterial.GetColor(ColorId);
            else
                _originalColor = Color.white;

            _hasInitializedColor = true;
        }

        // Inicializar fuego apagado
        if (fireVfxRoot != null)
            fireVfxRoot.SetActive(false);

        if (fireVfxCanvasGroup != null)
            fireVfxCanvasGroup.alpha = 0f;

        fireLoopSource = AudioManager.Instance.sfxSourceLoop;

        if (fireLoopSource != null)
        {
            fireLoopSource.volume = 0f;
            fireLoopSource.Stop();
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        int currentHeat = GameManager.Instance.heat;
        int maxHeat = GameManager.Instance.maxHeat;

        if (maxHeat <= 0) return;

        // Calcular porcentaje de calor (0.0 a 1.0)
        float heatPercentage = Mathf.Clamp01((float)currentHeat / maxHeat);

        // Actualizar sistemas
        UpdateThermometer(heatPercentage);
        UpdatePlayerColor(heatPercentage);
        UpdateFire(heatPercentage);
    }

    private void UpdateThermometer(float heatPercentage)
    {
        if (ejeTermometro == null) return;

        // Calcular rotación objetivo
        _targetRotationZ = Mathf.Lerp(minRotationZ, maxRotationZ, heatPercentage);

        // Suavizar transición usando LerpAngle para manejar correctamente los ángulos
        _currentRotationZ = Mathf.LerpAngle(_currentRotationZ, _targetRotationZ, Time.deltaTime * rotationSpeed);

        // Aplicar rotación
        Vector3 currentRotation = ejeTermometro.localEulerAngles;
        ejeTermometro.localEulerAngles = new Vector3(
            currentRotation.x,
            currentRotation.y,
            _currentRotationZ
        );
    }

    private void UpdatePlayerColor(float heatPercentage)
    {
        if (playerMaterial == null) return;

        // Color objetivo según porcentaje
        Color targetColor = Color.Lerp(Color.white, overheatColor, heatPercentage);

        // Obtener color actual
        Color currentColor = Color.white;
        if (playerMaterial.HasProperty(BaseColorId))
            currentColor = playerMaterial.GetColor(BaseColorId);
        else if (playerMaterial.HasProperty(ColorId))
            currentColor = playerMaterial.GetColor(ColorId);

        // Suavizar transición
        Color smoothColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorSpeed);

        // Aplicar color
        if (playerMaterial.HasProperty(BaseColorId))
            playerMaterial.SetColor(BaseColorId, smoothColor);
        if (playerMaterial.HasProperty(ColorId))
            playerMaterial.SetColor(ColorId, smoothColor);
    }

    private void UpdateFire(float heatPercentage)
    {
        // Convertir porcentaje de activación a 0-1
        float activationThreshold = fireActivationPercent / 100f;
        
        // Determinar si el fuego debe estar activo
        bool shouldBeActive = heatPercentage >= activationThreshold;

        // ===== VISUAL =====
        if (fireVfxRoot != null)
        {
            if (fireVfxCanvasGroup != null)
            {
                // Con CanvasGroup: fade suave
                float targetAlpha = shouldBeActive ? 1f : 0f;
                fireVfxCanvasGroup.alpha = Mathf.MoveTowards(
                    fireVfxCanvasGroup.alpha,
                    targetAlpha,
                    Time.deltaTime * fireVisualFadeSpeed
                );

                // Activar/desactivar GameObject según alpha
                if (shouldBeActive && !fireVfxRoot.activeSelf)
                {
                    fireVfxRoot.SetActive(true);
                }
                else if (!shouldBeActive && fireVfxCanvasGroup.alpha <= 0.001f && fireVfxRoot.activeSelf)
                {
                    fireVfxRoot.SetActive(false);
                }
            }
            else
            {
                // Sin CanvasGroup: on/off directo
                fireVfxRoot.SetActive(shouldBeActive);
            }
        }

        // ===== AUDIO =====
        if (fireLoopSource != null)
        {
            if (shouldBeActive)
            {
                // Calcular volumen según qué tan lejos estamos del threshold
                float volumeProgress = Mathf.InverseLerp(activationThreshold, 1f, heatPercentage);
                float targetVolume = volumeProgress * fireMaxVolume;

                // Suavizar volumen
                fireLoopSource.volume = Mathf.MoveTowards(
                    fireLoopSource.volume,
                    targetVolume,
                    Time.deltaTime * fireVolumeSpeed
                );

                // Iniciar loop si no está sonando
                if (!fireLoopSource.isPlaying)
                {
                    fireLoopSource.Play();
                }
            }
            else
            {
                // Fade out del volumen
                fireLoopSource.volume = Mathf.MoveTowards(
                    fireLoopSource.volume,
                    0f,
                    Time.deltaTime * fireVolumeSpeed
                );

                // Detener cuando llegue a 0
                if (fireLoopSource.volume <= 0.001f && fireLoopSource.isPlaying)
                {
                    fireLoopSource.Stop();
                }
            }
        }
    }

    public void ResetFeedback()
    {
        // Reset termómetro
        _currentRotationZ = minRotationZ;
        _targetRotationZ = minRotationZ;

        if (ejeTermometro != null)
        {
            Vector3 currentRotation = ejeTermometro.localEulerAngles;
            ejeTermometro.localEulerAngles = new Vector3(
                currentRotation.x,
                currentRotation.y,
                minRotationZ
            );
        }

        // Reset color
        if (playerMaterial != null && _hasInitializedColor)
        {
            if (playerMaterial.HasProperty(BaseColorId))
                playerMaterial.SetColor(BaseColorId, _originalColor);
            if (playerMaterial.HasProperty(ColorId))
                playerMaterial.SetColor(ColorId, _originalColor);
        }

        // Reset fuego
        if (fireVfxRoot != null)
            fireVfxRoot.SetActive(false);

        if (fireVfxCanvasGroup != null)
            fireVfxCanvasGroup.alpha = 0f;

        if (fireLoopSource != null)
        {
            fireLoopSource.volume = 0f;
            fireLoopSource.Stop();
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameReset += OnGameReset;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameReset -= OnGameReset;
    }

    private void OnGameReset()
    {
        ResetFeedback();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minRotationZ = Mathf.Clamp(minRotationZ, 0f, 360f);
        maxRotationZ = Mathf.Clamp(maxRotationZ, 0f, 360f);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        colorSpeed = Mathf.Max(0.1f, colorSpeed);
        fireActivationPercent = Mathf.Clamp(fireActivationPercent, 0f, 100f);
        fireVisualFadeSpeed = Mathf.Max(0.1f, fireVisualFadeSpeed);
        fireVolumeSpeed = Mathf.Max(0.1f, fireVolumeSpeed);
    }
#endif
}