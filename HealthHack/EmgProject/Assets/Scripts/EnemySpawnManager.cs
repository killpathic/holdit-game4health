using UnityEngine;
using DG.Tweening;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("Enemy Prefabs")]
    [Tooltip("Prefab for '0' in the sequence (Short Contraction).")]
    public GameObject shortEnemyPrefab;
    [Tooltip("Prefab for '1' in the sequence (Prolonged Contraction).")]
    public GameObject longEnemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Where the enemies will spawn.")]
    public Transform spawnPoint;
    [Tooltip("Time to wait before spawning the first enemy, and the delay between kills.")]
    public float spawnDelay = 1.5f;
    
    [Header("Animation Settings")]
    [Tooltip("How far to the right the enemy spawns before tweening in.")]
    public float spawnOffsetX = 10f;
    [Tooltip("How long it takes the enemy to tween to the spawn point.")]
    public float enterTweenDuration = 0.75f;

    private string levelSequence = "";

    [HideInInspector] public GameObject currentEnemy;

    private int currentIndex = 0;
    private float currentWaitTimer;

    public void SetSequence(string newSequence)
    {
        levelSequence = newSequence;
        currentIndex = 0;
        currentWaitTimer = spawnDelay;
        Debug.Log($"[EnemySpawnManager] Sequence acquired. Beginning level with {levelSequence.Length} enemies.");
    }

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Initialize timer to delay the very first spawn
        currentWaitTimer = spawnDelay;
    }

    void Update()
    {
        if (string.IsNullOrEmpty(levelSequence)) return;

        // Stop if we have completed the whole sequence
        if (currentIndex >= levelSequence.Length)
        {
            // If the very last enemy was destroyed, officially trigger Game Over logic
            if (currentEnemy == null && GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                GameManager.Instance.OnLevelCompleted();
            }
            return;
        }

        // If an enemy is currently alive, we wait for it to be destroyed
        if (currentEnemy != null) return;

        // If no enemy is alive, count down the spawn timer
        currentWaitTimer -= Time.deltaTime;

        if (currentWaitTimer <= 0f)
        {
            SpawnNextEnemy();
            // Reset timer so it's ready for the next wait after this new enemy dies
            currentWaitTimer = spawnDelay; 
        }
    }

    private void SpawnNextEnemy()
    {
        char nextEnemyType = levelSequence[currentIndex];
        GameObject prefabToSpawn = null;

        if (nextEnemyType == '0')
        {
            prefabToSpawn = shortEnemyPrefab;
        }
        else if (nextEnemyType == '1')
        {
            prefabToSpawn = longEnemyPrefab;
        }
        else
        {
            Debug.LogWarning($"[EnemySpawnManager] Unrecognized character '{nextEnemyType}' in sequence at index {currentIndex}");
            currentIndex++;
            return;
        }

        if (prefabToSpawn != null && spawnPoint != null)
        {
            // Calculate off-screen spawn position
            Vector3 offScreenPos = spawnPoint.position + new Vector3(spawnOffsetX, 0, 0);
            
            currentEnemy = Instantiate(prefabToSpawn, offScreenPos, spawnPoint.rotation);
            Debug.Log($"[EnemySpawnManager] Spawned {(nextEnemyType == '0' ? "Short" : "Long")} Enemy. Progress: {currentIndex + 1}/{levelSequence.Length}");
            
            // Tween the enemy to the actual spawn point
            currentEnemy.transform.DOMove(spawnPoint.position, enterTweenDuration).SetEase(Ease.OutBack);
        }
        else
        {
            Debug.LogWarning("[EnemySpawnManager] Prefabs or Spawn Point not assigned!");
        }

        currentIndex++;
    }
}
