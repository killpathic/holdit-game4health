using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;

public class CannonScript : MonoBehaviour
{
    [Tooltip("Action for firing. Default is Space key.")]
    public InputAction fireAction = new InputAction("Fire", binding: "<Keyboard>/space");
    
    [Tooltip("Minimum time to hold the button for a standard contraction/shot.")]
    public float minShortContractionTime = 1f;

    [Tooltip("Time in seconds to hold the button for a prolonged/heavy contraction.")]
    public float minHeavyContractionTime = 7f;

    [Tooltip("Target scale to squash to during charge.")]
    public Vector3 squashTargetScale = new Vector3(1.2f, 0.8f, 1f);
    
    [Tooltip("Strength of the shake when reaching the max threshold.")]
    public float shakeStrength = 0.05f;

    [Header("EMG Settings")]
    [Tooltip("Time in seconds the signal must remain below threshold (or at 0) to officially end the contraction. Prevents accidental drops.")]
    public float relaxationGracePeriod = 0.3f;
    
    [Header("Projectiles")]
    [SerializeField] private GameObject standardProjectilePrefab;
    [SerializeField] private GameObject heavyProjectilePrefab;
    [SerializeField] private Transform enemyTarget;
    [Tooltip("Particle system that plays when the cannon fires.")]
    [SerializeField] private ParticleSystem fireParticles;
    [Tooltip("Audio source that plays when the cannon fires.")]
    [SerializeField] private AudioSource fireSound;
    [Tooltip("Particle system prefab that plays when an enemy dies.")]
    [SerializeField] private ParticleSystem enemyDeathParticlePrefab;
    [Tooltip("Minimum pitch variation for the fire sound.")]
    public float minFireSoundPitch = 0.9f;
    [Tooltip("Maximum pitch variation for the fire sound.")]
    public float maxFireSoundPitch = 1.1f;
    [Tooltip("Time in seconds for the projectile to reach the enemy target.")]
    public float projectileTravelTime = 0.5f;
    [Tooltip("Minimum absolute height the arc goes during the projectile's travel.")]
    public float minProjectileArcHeight = 1f;
    [Tooltip("Maximum absolute height the arc goes during the projectile's travel.")]
    public float maxProjectileArcHeight = 3f;
    [Tooltip("Max angle the cannon tilts during the kickback based on the arc height.")]
    public float maxArcTiltAngle = 10f;
    
    [Header("UI Progress Bar")]
    [Tooltip("The image used as a progress bar, scaled on its X axis.")]
    public Image chargeProgressBar;
    [Tooltip("Color when the charge is too low to fire.")]
    public Color insufficientChargeColor = new Color(0.8f, 0.8f, 0.8f); // Dull white/gray
    public Color chargingColor = Color.green;
    public Color maxChargeColor = new Color(1f, 0.5f, 0f); // Orange
    [Tooltip("Shake strength for the progress bar UI.")]
    public float progressBarShakeStrength = 2f;
    
    [Header("Camera Effects on Hit")]
    [Tooltip("Camera shake intensity for a standard short hit.")]
    public float shortHitShakeIntensity = 0.1f;
    [Tooltip("Hitstop duration for a standard short hit.")]
    public float shortHitstopDuration = 0.05f;
    [Tooltip("Camera shake intensity for a heavy prolonged hit.")]
    public float heavyHitShakeIntensity = 0.3f;
    [Tooltip("Hitstop duration for a heavy prolonged hit.")]
    public float heavyHitstopDuration = 0.15f;
    
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isCharging = false;
    private bool isShaking = false;
    private float contractionStartTime;
    private bool wasContracting = false;
    private float relaxationTimer = 0f;

    private Vector3 progressBarOriginalScale;
    private Vector3 progressBarOriginalPosition;
    private bool isProgressBarShaking = false;

    void Start()
    {
        // Bind the Gamepad "A" button (South button) so controller works seamlessly in simulation mode
        fireAction.AddBinding("<Gamepad>/buttonSouth");

        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        originalRotation = transform.rotation;

        if (chargeProgressBar != null)
        {
            progressBarOriginalScale = chargeProgressBar.transform.localScale;
            progressBarOriginalPosition = chargeProgressBar.transform.localPosition;
            
            // Start empty
            chargeProgressBar.transform.localScale = new Vector3(0, progressBarOriginalScale.y, progressBarOriginalScale.z);
            chargeProgressBar.color = insufficientChargeColor;
        }
    }

    void OnEnable()
    {
        fireAction.Enable();
    }

    void OnDisable()
    {
        fireAction.Disable();
    }

    private void StartContraction()
    {
        // Cancel any active scale tween before starting a new charge
        transform.DOKill();
        transform.localPosition = originalPosition;
        transform.rotation = originalRotation;
        
        if (chargeProgressBar != null)
        {
            chargeProgressBar.transform.DOKill();
            chargeProgressBar.transform.localPosition = progressBarOriginalPosition;
            chargeProgressBar.transform.localScale = new Vector3(0, progressBarOriginalScale.y, progressBarOriginalScale.z);
            chargeProgressBar.color = insufficientChargeColor;
            isProgressBarShaking = false;
        }

        contractionStartTime = Time.time;
        isCharging = true;
        isShaking = false;
        Debug.Log("Contraction started... Charging...");
    }

    private void EndContraction()
    {
        isCharging = false;
        
        transform.DOKill(); // Stop shaking
        transform.localPosition = originalPosition; // Snap position back to base
        
        // Spring back to original size using DOTween elastic ease
        transform.DOScale(originalScale, 0.5f).SetEase(Ease.OutElastic); 

        if (chargeProgressBar != null)
        {
            chargeProgressBar.transform.DOKill(); // Stop shaking
            chargeProgressBar.transform.localPosition = progressBarOriginalPosition;
            chargeProgressBar.color = insufficientChargeColor;
            
            // Animate progress bar emptying
            chargeProgressBar.transform.DOScaleX(0f, 0.2f).SetEase(Ease.OutQuad);
        }

        float duration = Time.time - contractionStartTime;
        
        if (duration >= minHeavyContractionTime)
        {
            Debug.Log($"[Cannon] Prolonged Muscle Contraction (Held for {duration:F2}s) - Fired Heavy Shot!");
            FireProjectile(heavyProjectilePrefab, Enemy.ContractionType.Prolonged);
        }
        else if (duration >= minShortContractionTime)
        {
            Debug.Log($"[Cannon] Short Muscle Contraction (Held for {duration:F2}s) - Fired Standard Shot!");
            FireProjectile(standardProjectilePrefab, Enemy.ContractionType.Short);
        }
        else
        {
            Debug.Log($"[Cannon] Contraction too short (Held for {duration:F2}s) - Misfire!");
        }
    }

    private void FireProjectile(GameObject prefab, Enemy.ContractionType attackType)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[Cannon] No projectile prefab assigned!");
            return;
        }

        // Intelligently find the active enemy target - use singleton if available, otherwise fallback to standard field
        Transform activeTarget = enemyTarget;
        if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.currentEnemy != null)
        {
            activeTarget = EnemySpawnManager.Instance.currentEnemy.transform;
        }
        else if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.spawnPoint != null)
        {
            // Default to aiming at the spawn point if an enemy hasn't spawned physically yet
            activeTarget = EnemySpawnManager.Instance.spawnPoint;
        }

        if (activeTarget == null)
        {
            Debug.LogWarning("[Cannon] No active enemy or spawn point available!");
            return;
        }

        // Spawn at cannon position
        GameObject projectile = Instantiate(prefab, transform.position, Quaternion.identity);
        
        if (fireParticles != null)
        {
            fireParticles.Play();
        }

        if (fireSound != null)
        {
            fireSound.pitch = Random.Range(minFireSoundPitch, maxFireSoundPitch);
            fireSound.Play();
        }

        // Calculate a random arc height between min and max
        float randomArc = Random.Range(minProjectileArcHeight, maxProjectileArcHeight);
        
        // Randomly decide if the arc goes up (positive) or down (negative)
        if (Random.value > 0.5f)
        {
            randomArc = -randomArc;
        }

        // Calculate rotation angle based on the random arc's fraction of the max arc
        float arcFraction = Mathf.Abs(randomArc) / maxProjectileArcHeight;
        float targetZAngle = arcFraction * maxArcTiltAngle * Mathf.Sign(randomArc);
        
        // Calculate angle to enemy target (assuming 2D right-facing sprite)
        Vector3 direction = activeTarget.position - transform.position;
        
        // Fast tween to the arc angle, then dynamically bounce back to the original resting rotation
        Sequence rotSeq = DOTween.Sequence();
        rotSeq.Append(transform.DORotate(originalRotation.eulerAngles + new Vector3(0, 0, targetZAngle), 0.1f));
        rotSeq.Append(transform.DORotateQuaternion(originalRotation, 0.4f).SetEase(Ease.OutElastic));

        // Tween towards enemy in an arc using DOJump, then destroy on completion
        projectile.transform.DOJump(activeTarget.position, randomArc, 1, projectileTravelTime)
            .SetEase(Ease.Linear)
            .OnComplete(() => 
            {
                Destroy(projectile);
                
                // Directly pass the attack outcome to the target enemy
                if (activeTarget != null)
                {
                    Enemy enemy = activeTarget.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        bool destroyed = enemy.ProcessHit(attackType);
                        
                        // If successfully destroyed, play global death particles at its location
                        if (destroyed)
                        {
                            if (enemyDeathParticlePrefab != null)
                            {
                                ParticleSystem particles = Instantiate(enemyDeathParticlePrefab, activeTarget.position, Quaternion.identity);
                                particles.Play();
                                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
                            }

                            // Add a satisfying score bump! Give more points for a heavy shot
                            if (ScoreManager.Instance != null)
                            {
                                int points = (attackType == Enemy.ContractionType.Prolonged) ? 300 : 100;
                                ScoreManager.Instance.AddScore(points);
                            }

                            // Trigger camera shake and hitstop effects based on attack type
                            if (CameraEffectManager.Instance != null)
                            {
                                if (attackType == Enemy.ContractionType.Prolonged)
                                {
                                    CameraEffectManager.Instance.ShakeCamera(heavyHitShakeIntensity);
                                    CameraEffectManager.Instance.TriggerHitstop(heavyHitstopDuration);
                                }
                                else
                                {
                                    CameraEffectManager.Instance.ShakeCamera(shortHitShakeIntensity);
                                    CameraEffectManager.Instance.TriggerHitstop(shortHitstopDuration);
                                }
                            }
                        }
                    }
                }
            });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start() is defined above

    // Update is called once per frame
    void Update()
    {
        float currentEmgValue = 0f;
        float actualThreshold = 300f; // Default mock fallback threshold

        // Read physical parameters from GameManager
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isSimulationMode)
            {
                // In Simulation Mode, button presses mimic high/low EMG signals naturally yielding True/False boundaries
                float pressedState = fireAction.IsPressed() ? 1f : 0f;
                currentEmgValue = pressedState * 400f; // E.g., drops a flat mock 400 for a flex
                actualThreshold = 300f;
            }
            else
            {
                // Real Live Mode
                currentEmgValue = GameManager.Instance.currentEmgValue;
                // Calculate dynamic activation threshold directly splitting between calibration relaxed and max flexes. 
                actualThreshold = GameManager.Instance.relaxedEmgBaseline + ((GameManager.Instance.flexedEmgBaseline - GameManager.Instance.relaxedEmgBaseline) * 0.4f);
            }
        }

        // Determine raw contracting boolean 
        bool isCurrentlyContracting = currentEmgValue >= actualThreshold;

        // Perform Edge Detection with Hysteresis (Grace Period)
        if (isCurrentlyContracting)
        {
            relaxationTimer = 0f; // Muscle is engaged, instantly reset the drop timer
            
            if (!wasContracting)
            {
                StartContraction();
                wasContracting = true;
            }
        }
        else if (wasContracting)
        {
            // Muscle signal dropped below threshold. Start grace period timer instead of cancelling immediately.
            relaxationTimer += Time.deltaTime;
            
            // Only officially end the contraction if the signal has been consistently lost/at 0 for the duration of the grace period.
            if (relaxationTimer >= relaxationGracePeriod)
            {
                EndContraction();
                wasContracting = false;
            }
        }

        if (isCharging)
        {
            float duration = Time.time - contractionStartTime;
            float t = duration / minHeavyContractionTime;
            
            // Clamp t between 0 and 1 so it stops squashing once the threshold limit is reached.
            t = Mathf.Clamp01(t);
            
            // Interpolate toward the squash target scale
            transform.localScale = Vector3.Lerp(originalScale, squashTargetScale, t);

            if (chargeProgressBar != null)
            {
                // Scale progress bar based on t
                chargeProgressBar.transform.localScale = new Vector3(
                    Mathf.Lerp(0, progressBarOriginalScale.x, t), 
                    progressBarOriginalScale.y, 
                    progressBarOriginalScale.z
                );
                
                // Update coloring based on how much duration has passed
                if (duration < minShortContractionTime)
                {
                    chargeProgressBar.color = insufficientChargeColor;
                }
                else if (duration >= minShortContractionTime && duration < minHeavyContractionTime)
                {
                    chargeProgressBar.color = chargingColor;
                }
                
                if (t >= 1f && !isProgressBarShaking)
                {
                    isProgressBarShaking = true;
                    chargeProgressBar.color = maxChargeColor;
                    chargeProgressBar.transform.DOShakePosition(1f, new Vector3(progressBarShakeStrength, progressBarShakeStrength, 0), 10, 45, false, false).SetLoops(-1);
                }
            }

            // Start shaking if we reach the threshold and haven't started shaking yet
            if (t >= 1f && !isShaking)
            {
                isShaking = true;
                // Infinite duration loop using DOShakePosition (-1 loops), fadeOut set to false for constant shake 
                transform.DOShakePosition(1f, new Vector3(shakeStrength, shakeStrength, 0), 10, 45, false, false).SetLoops(-1);
            }
        }
    }
}
