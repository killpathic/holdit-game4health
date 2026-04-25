using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Form, EmgCalibration, Fetching, Playing, GameOver }
    public GameState currentState = GameState.Form;

    [Header("Component References")]
    [Tooltip("The main UI object that holds your form.")]
    public GameObject formUIObject;
    [Tooltip("The UI object that holds your End Screen.")]
    public GameObject endScreenUIObject;
    [Tooltip("The UI object that handles your EMG Calibration.")]
    public GameObject emgScreenUIObject;
    public EndScreenScript endScreenScript;
    public SequenceAIManager sequenceAIManager;
    public CannonScript cannonScript;

    [Header("EMG Data")]
    public string emgPollUrl = "http://192.168.137.98/state";
    public float pollRate = 0.2f;
    [HideInInspector] public float relaxedEmgBaseline = 0f;
    [HideInInspector] public float flexedEmgBaseline = 100f;
    [HideInInspector] public float currentEmgValue = 0f;
    [HideInInspector] public bool isSimulationMode = false;

    // Helper class for parsing JSON
    [System.Serializable]
    private class EmgResponse { public float value; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Enforce the starting state to Form so nothing begins prematurely
        SetState(GameState.Form);
        StartCoroutine(PollEmgRoutine());
    }

    private IEnumerator PollEmgRoutine()
    {
        while (true)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(emgPollUrl))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string response = webRequest.downloadHandler.text;
                    float parsedValue = 0f;

                    if (response.Contains("{"))
                    {
                        try 
                        {
                            EmgResponse parsedObj = JsonUtility.FromJson<EmgResponse>(response);
                            parsedValue = parsedObj.value;
                        } 
                        catch { float.TryParse(response.Trim(), out parsedValue); }
                    }
                    else
                    {
                        float.TryParse(response.Trim(), out parsedValue);
                    }
                    currentEmgValue = parsedValue;
                }
            }
            yield return new WaitForSecondsRealtime(pollRate);
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case GameState.Form:
                if (formUIObject != null) formUIObject.SetActive(true);
                if (endScreenUIObject != null) endScreenUIObject.SetActive(false);
                if (emgScreenUIObject != null) emgScreenUIObject.SetActive(false);
                if (cannonScript != null) cannonScript.enabled = false;
                break;

            case GameState.EmgCalibration:
                if (formUIObject != null) formUIObject.SetActive(false);
                if (endScreenUIObject != null) endScreenUIObject.SetActive(false);
                if (emgScreenUIObject != null) emgScreenUIObject.SetActive(true);
                if (cannonScript != null) cannonScript.enabled = false;
                break;
                
            case GameState.Fetching:
                if (formUIObject != null) formUIObject.SetActive(false);
                if (endScreenUIObject != null) endScreenUIObject.SetActive(false);
                if (emgScreenUIObject != null) emgScreenUIObject.SetActive(false);
                if (cannonScript != null) cannonScript.enabled = false;
                break;
                
            case GameState.Playing:
                if (formUIObject != null) formUIObject.SetActive(false);
                if (endScreenUIObject != null) endScreenUIObject.SetActive(false);
                if (emgScreenUIObject != null) emgScreenUIObject.SetActive(false);
                if (cannonScript != null) cannonScript.enabled = true; // Turn the cannon online!
                break;
                
            case GameState.GameOver:
                if (cannonScript != null) cannonScript.enabled = false; // Turn the cannon off
                if (endScreenUIObject != null) endScreenUIObject.SetActive(true);
                if (emgScreenUIObject != null) emgScreenUIObject.SetActive(false);
                
                // Immediately configure the end screen panel displaying their final score gracefully
                if (endScreenScript != null && ScoreManager.Instance != null)
                {
                    endScreenScript.SetupScreen(ScoreManager.Instance.GetScore());
                }
                
                Debug.Log("[GameManager] Level Complete! The sequence finished.");
                break;
        }
    }

    public void OpenEmgCalibration()
    {
        SetState(GameState.EmgCalibration);
    }

    public void SaveEmgCalibration(float relaxedAvg, float flexedAvg)
    {
        relaxedEmgBaseline = relaxedAvg;
        flexedEmgBaseline = flexedAvg;
        Debug.Log($"[GameManager] EMG Calibrated! Relaxed: {relaxedAvg} | Flexed: {flexedAvg}");
    }

    public void SubmitForm(string apiUrl, int age, float maxDuration, string condition, string status, bool isSim)
    {
        isSimulationMode = isSim;
        SetState(GameState.Fetching);
        
        if (sequenceAIManager != null)
        {
            sequenceAIManager.FetchSequence(apiUrl, age, maxDuration, condition, status);
        }
        else
        {
            Debug.LogError("[GameManager] SequenceAIManager is completely missing!");
        }
    }

    public void OnSequenceFetched(string sequence)
    {
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.SetSequence(sequence);
        }
        
        // Unleash the player
        SetState(GameState.Playing);
    }
    
    public void OnLevelCompleted()
    {
        SetState(GameState.GameOver);
    }

    public void RestartToForm()
    {
        // Reset numeric stats safely via ScoreManager Singleton
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        
        SetState(GameState.Form);
    }
}
