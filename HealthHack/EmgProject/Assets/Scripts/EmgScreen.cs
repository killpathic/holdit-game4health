using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class EmgScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text element displaying current live EMG value.")]
    public TMP_Text emgValueText;
    
    [Tooltip("Text element for step-by-step calibration instructions.")]
    public TMP_Text instructionsText;

    [Tooltip("Button to initiate calibration process.")]
    public Button calibrateButton;

    [Tooltip("Button to return to the form screen.")]
    public Button backButton;

    private bool isCalibrating = false;

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (calibrateButton != null)
            calibrateButton.onClick.AddListener(StartCalibration);
    }

    private void OnEnable()
    {
        isCalibrating = false;

        if (instructionsText != null)
            instructionsText.text = "Press 'Calibrate' to begin your baseline setup.";
    }

    private void Update()
    {
        if (GameManager.Instance != null && emgValueText != null)
        {
            emgValueText.text = $"Raw EMG: {GameManager.Instance.currentEmgValue:F1}";
        }
    }

    private void OnDisable()
    {
    }

    private void OnBackClicked()
    {
        if (GameManager.Instance != null && !isCalibrating)
        {
            GameManager.Instance.SetState(GameManager.GameState.Form);
        }
    }

    private void StartCalibration()
    {
        if (isCalibrating) return;
        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        isCalibrating = true;
        float pRate = GameManager.Instance != null ? GameManager.Instance.pollRate : 0.2f;
        
        // Lock controls to prevent overlapping calibration sequences safely
        if (calibrateButton != null) calibrateButton.interactable = false;
        if (backButton != null) backButton.interactable = false;

        // --- RELAXED BASELINE PHASE ---
        if (instructionsText != null) instructionsText.text = "Phase 1/2: Please RELAX your muscle completely...\n(3 seconds)";
        
        List<float> relaxedSamples = new List<float>();
        float elapsed = 0f;

        while (elapsed < 3f)
        {
            elapsed += pRate;
            if (GameManager.Instance != null) relaxedSamples.Add(GameManager.Instance.currentEmgValue);
            yield return new WaitForSecondsRealtime(pRate);
        }

        // --- FLEXED CONTRACTED PHASE ---
        if (instructionsText != null) instructionsText.text = "Phase 2/2: Please FLEX your muscle precisely as hard as possible!\n(3 seconds)";
        
        List<float> flexedSamples = new List<float>();
        elapsed = 0f;

        while (elapsed < 3f)
        {
            elapsed += pRate;
            if (GameManager.Instance != null) flexedSamples.Add(GameManager.Instance.currentEmgValue);
            yield return new WaitForSecondsRealtime(pRate);
        }

        // --- CALCULATION PHASE ---
        float relaxedAverage = 0f;
        foreach (float val in relaxedSamples) relaxedAverage += val;
        if (relaxedSamples.Count > 0) relaxedAverage /= relaxedSamples.Count;

        float flexedAverage = 0f;
        foreach (float val in flexedSamples) flexedAverage += val;
        if (flexedSamples.Count > 0) flexedAverage /= flexedSamples.Count;

        // Inject calculated thresholds natively into Global GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveEmgCalibration(relaxedAverage, flexedAverage);
        }

        if (instructionsText != null) 
            instructionsText.text = $"Calibration Complete!\nRelaxed Average: {relaxedAverage:F1}\nFlexed Average: {flexedAverage:F1}";

        // Provide 2 seconds of purely visual feedback showcasing the successful numbers
        yield return new WaitForSecondsRealtime(2f);

        // Resume functionality natively
        if (calibrateButton != null) calibrateButton.interactable = true;
        if (backButton != null) backButton.interactable = true;
        isCalibrating = false;
    }
}
