using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("TextMeshPro text element for displaying the score.")]
    public TMP_Text scoreText;

    [Header("Animation Settings")]
    [Tooltip("How long it takes the score number to count up to the new value.")]
    public float countTweenDuration = 0.5f;
    [Tooltip("How large the text scales up when points are scored.")]
    public Vector3 popScale = new Vector3(1.5f, 1.5f, 1f);
    [Tooltip("How long the scale pop animation lasts.")]
    public float popDuration = 0.3f;

    private int currentScore = 0;
    private int displayScore = 0;

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
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        currentScore += amount;

        // Tween the visual integer scaling up to the actual currentScore smoothly
        DOTween.To(() => displayScore, x => 
        {
            displayScore = x;
            UpdateScoreText();
        }, currentScore, countTweenDuration).SetEase(Ease.OutQuad);

        // Add a satisfying punch/bounce animation to the text object itself
        if (scoreText != null)
        {
            // Kill any active scale tweens to prevent them from breaking if scored too rapidly
            scoreText.transform.DOKill(true);
            
            // Quick burst of scale then bounce back cleanly to original (1,1,1) size
            Sequence popSeq = DOTween.Sequence();
            popSeq.Append(scoreText.transform.DOScale(popScale, popDuration * 0.5f).SetEase(Ease.OutBack));
            popSeq.Append(scoreText.transform.DOScale(Vector3.one, popDuration * 0.5f).SetEase(Ease.InBack));
        }
    }

    public int GetScore()
    {
        return currentScore;
    }

    public void ResetScore()
    {
        currentScore = 0;
        displayScore = 0;
        
        if (scoreText != null)
        {
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
        }
        
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            // Keep formatting consistent, you can pad with zeros if preferred (e.g. "000")
            scoreText.text = $"{displayScore}";
        }
    }
}
