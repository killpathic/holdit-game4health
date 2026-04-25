using UnityEngine;
using DG.Tweening;
using System.Collections;

public class CameraEffectManager : MonoBehaviour
{
    public static CameraEffectManager Instance { get; private set; }

    [Header("Camera Reference")]
    [Tooltip("The main camera to apply shake effects to.")]
    public Camera mainCamera;

    private Vector3 originalCameraPosition;
    private Coroutine hitstopCoroutine;

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
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
            originalCameraPosition = mainCamera.transform.localPosition;
    }

    /// <summary>
    /// Shakes the camera with the given intensity.
    /// </summary>
    public void ShakeCamera(float intensity, float duration = 0.2f)
    {
        if (mainCamera == null) return;

        // Kill active tweens to prevent the camera position from drifting over multiple consecutive shakes
        mainCamera.transform.DOKill();
        mainCamera.transform.localPosition = originalCameraPosition;

        // Apply DOTween shake using the specified intensity
        mainCamera.transform.DOShakePosition(duration, new Vector3(intensity, intensity, 0), 15, 90, false, true);
    }

    /// <summary>
    /// Briefly pauses the game time to create a heavy impact "hitstop" feel.
    /// </summary>
    public void TriggerHitstop(float stopDuration = 0.05f)
    {
        if (hitstopCoroutine != null)
        {
            StopCoroutine(hitstopCoroutine);
        }
        hitstopCoroutine = StartCoroutine(HitstopRoutine(stopDuration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        // Snapshot time and practically freeze it
        Time.timeScale = 0f;
        
        // Wait using unscaled real-world time so the game doesn't freeze forever
        yield return new WaitForSecondsRealtime(duration);
        
        // Restore time back to normal gameplay speed
        Time.timeScale = 1f;
        hitstopCoroutine = null;
    }
}
