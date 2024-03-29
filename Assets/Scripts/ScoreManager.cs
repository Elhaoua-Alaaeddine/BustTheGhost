using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    [SerializeField] int score = 10;

    void Start()
    {
        UpdateScoreText();
    }

    public void DecreaseScore()
    {
        score--;
        UpdateScoreText();
        if (score <= 0)
        {
            // Trigger game over
            GameOver();
        }
    }
    public int GetScore()
    {
        return score;
    }
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString();
    }
    void GameOver()
    {
        // Load the game over scene
        SceneManager.LoadScene("GameOverScene");
    }
}
