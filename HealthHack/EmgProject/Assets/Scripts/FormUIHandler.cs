using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FormUIHandler : MonoBehaviour
{
    [Header("API Config")]
    [Tooltip("The Base API Endpoint to request ML configuration from.")]
    public string apiUrl = "http://localhost:8000/unity";

    [Header("Form Inputs")]
    public Slider ageSlider;
    public TMP_Text ageText;
    
    public Slider durationSlider;
    public TMP_Text durationText;
    
    public TMP_Dropdown therapyDropdown;
    public TMP_Dropdown statusDropdown;
    public Button submitButton;
    public Button calibrateButton;
    public Toggle simulationToggle;

    private List<string> therapyOrder = new List<string> { "parkinson", "stroke", "atrophy", "sports_injury", "healthy" };

    private Dictionary<string, List<string>> conditionStatuses = new Dictionary<string, List<string>>()
    {
        { "parkinson", new List<string> { "mild", "moderate", "severe" } },
        { "stroke", new List<string> { "early", "mid", "advanced" } },
        { "atrophy", new List<string> { "early", "mid", "advanced" } },
        { "sports injury", new List<string> { "early", "mid", "advanced" } },
        { "healthy", new List<string> { "normal", "working out", "advanced" } }
    };

    void Start()
    {
        // Populate the therapy dropdown strictly with our configured ML therapies in order
        if (therapyDropdown != null)
        {
            therapyDropdown.ClearOptions();
            List<string> uiTherapies = new List<string>();
            foreach (string therapy in therapyOrder)
            {
                string formatted = therapy.Replace("_", " ");
                if (formatted.Length > 0)
                {
                    formatted = char.ToUpper(formatted[0]) + formatted.Substring(1);
                }
                uiTherapies.Add(formatted);
            }
            therapyDropdown.AddOptions(uiTherapies);
        }

        // Listen for value changes on UI elements
        if (ageSlider != null)
            ageSlider.onValueChanged.AddListener(UpdateAgeDisplay);
            
        if (durationSlider != null)
            durationSlider.onValueChanged.AddListener(UpdateDurationDisplay);
            
        if (therapyDropdown != null)
            therapyDropdown.onValueChanged.AddListener(OnTherapyChanged);
            
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitForm);

        if (calibrateButton != null)
            calibrateButton.onClick.AddListener(OnCalibrateClicked);

        // Initialize UI Text and Dropdowns
        if (ageSlider != null) UpdateAgeDisplay(ageSlider.value);
        if (durationSlider != null) UpdateDurationDisplay(durationSlider.value);
        if (therapyDropdown != null) OnTherapyChanged(therapyDropdown.value);
    }

    private void UpdateAgeDisplay(float value)
    {
        if (ageText != null)
            ageText.text = value.ToString("0"); // e.g. 70
    }

    private void UpdateDurationDisplay(float value)
    {
        if (durationText != null)
            durationText.text = value.ToString("0.0") + "s"; // e.g. 2.0s
    }

    private void OnTherapyChanged(int index)
    {
        if (therapyDropdown == null || statusDropdown == null) return;

        string condition = therapyDropdown.options[index].text.ToLower();
        // Handle variations like "sports_injury" vs "sports injury"
        condition = condition.Replace("_", " ");

        statusDropdown.ClearOptions();

        if (conditionStatuses.ContainsKey(condition))
        {
            List<string> statuses = conditionStatuses[condition];
            // Capitalize first letter for UI display
            List<string> uiOptions = new List<string>();
            foreach (string s in statuses)
            {
                string formatted = s.Replace("_", " ");
                if (formatted.Length > 0)
                {
                    formatted = char.ToUpper(formatted[0]) + formatted.Substring(1);
                }
                uiOptions.Add(formatted);
            }
            statusDropdown.AddOptions(uiOptions);
        }
        else
        {
            statusDropdown.AddOptions(new List<string> { "Default" });
        }
    }

    private void OnCalibrateClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OpenEmgCalibration();
        }
    }

    private void OnSubmitForm()
    {
        int age = ageSlider != null ? Mathf.RoundToInt(ageSlider.value) : 70;
        float maxDuration = durationSlider != null ? durationSlider.value : 2.0f;
        string condition = "parkinson"; // Fallback
        string status = "mild"; // Fallback
        bool isSim = simulationToggle != null ? simulationToggle.isOn : true;
        
        if (therapyDropdown != null && therapyDropdown.options.Count > 0)
        {
            condition = therapyDropdown.options[therapyDropdown.value].text.ToLower().Replace(" ", "_");
        }

        if (statusDropdown != null && statusDropdown.options.Count > 0)
        {
            status = statusDropdown.options[statusDropdown.value].text.ToLower().Replace(" ", "_");
        }

        Debug.Log($"[FormUIHandler] Submitting Form: Age={age}, Duration={maxDuration}s, Condition={condition}, Status={status}, Sim={isSim}");
        
        // Pass the dynamically collected data over to the central GameManager!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitForm(apiUrl, age, maxDuration, condition, status, isSim);
        }
        else
        {
            Debug.LogError("[FormUIHandler] GameManager is not in the scene!");
        }
    }
}
