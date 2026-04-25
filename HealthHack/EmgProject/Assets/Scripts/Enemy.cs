using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum ContractionType
    {
        Short,
        Prolonged
    }

    [Header("Enemy Settings")]
    [Tooltip("The type of contraction needed to defeat this enemy.")]
    public ContractionType requiredAttackType;

    // Called directly by the CannonScript when the projectile tween finishes
    public bool ProcessHit(ContractionType attackType)
    {
        // Check if the received attack matches what this enemy requires
        if (attackType == requiredAttackType)
        {
            Destroy(gameObject);
            return true;
        }
        else
        {
            Debug.Log($"[Enemy] Attack failed! Required: {requiredAttackType}, Received: {attackType}");
            return false;
        }
    }
}
