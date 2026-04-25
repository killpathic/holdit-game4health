using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SequenceAIManager : MonoBehaviour
{
    // Defines the layout we expect back from the JSON response
    [System.Serializable]
    private class SequenceResponse
    {
        public int[] sequence;
    }

    public void FetchSequence(string apiUrl, int age, float maxDuration, string condition, string status)
    {
        StartCoroutine(GetSequenceFromAPI(apiUrl, age, maxDuration, condition, status));
    }

    private IEnumerator GetSequenceFromAPI(string apiUrl, int age, float maxDuration, string condition, string status)
    {
        string requestUrl = $"{apiUrl}?age={age}&condition={condition}&status={status}&max_duration={maxDuration.ToString("F1")}";
        
        Debug.Log($"[SequenceAIManager] Requesting ML sequence from API: {requestUrl}");
        
        // Send GET request to API with parameters in the URL string.
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SequenceAIManager] API Error: {webRequest.error}");
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"[SequenceAIManager] Successful API Response: {jsonResponse}");

                // Parse the JSON string into our serializable C# class
                SequenceResponse response = JsonUtility.FromJson<SequenceResponse>(jsonResponse);

                if (response != null && response.sequence != null)
                {
                    // We must convert the raw int array [0,1,0,0] to the string format "0100" EnemySpawnManager requires
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < response.sequence.Length; i++)
                    {
                        sb.Append(response.sequence[i]);
                    }

                    string parsedSequenceString = sb.ToString();
                    
                    // Directly feed it into the GameManager
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnSequenceFetched(parsedSequenceString);
                    }
                    else
                    {
                        Debug.LogWarning("[SequenceAIManager] Sequence was fetched, but GameManager singleton could not be found!");
                    }
                }
                else
                {
                    Debug.LogError("[SequenceAIManager] Failed to parse the 'sequence' integer array from JSON data.");
                }
            }
        }
    }
}
