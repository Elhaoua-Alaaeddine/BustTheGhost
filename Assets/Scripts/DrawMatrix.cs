using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class DrawMatrix : MonoBehaviour
{
    struct ProbabilityData
    {
        public float priorProbability;
        public float posteriorProbability;
    }
    private bool isBustModeActive = false;
    public GameObject[,] grid; 
    public ScoreManager scoreManager; 
    public BustsManager bustsManager; 

    public GameObject xMarkObject; 

    private SpriteRenderer xMarkRenderer;
    public GameObject N;
    public GameObject S;
    public GameObject W;
    public GameObject E;



    public Color greenColor;
    public Color yellowColor;
    public Color orangeColor;
    public Color redColor;
    bool[,] colorRevealed;

    int columns = 12;
    int rows = 9;
    ProbabilityData[,] probabilities;
    Vector2Int ghostLocation; 
    [SerializeField] GameObject cellPrefab;
    [SerializeField] Button bustButton;
    [SerializeField] Button peepButton;

    void Start()
    {
        ghostLocation = PlaceGhost();
        xMarkRenderer = xMarkObject.GetComponent<SpriteRenderer>();

        grid = new GameObject[columns, rows];
        colorRevealed = new bool[columns, rows];
        probabilities = new ProbabilityData[columns, rows];
        ComputeInitialPriorProbabilities();
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                colorRevealed[i, j] = false;
                GameObject cell = SpawnCell(i, j);
                grid[i, j] = cell;
                TextMeshProUGUI textMesh = cell.GetComponentInChildren<TextMeshProUGUI>();
                textMesh.text = probabilities[i, j].priorProbability.ToString("0.0000");
            }
        }

        if (xMarkRenderer != null)
            xMarkRenderer.enabled = false;
        bustButton.onClick.AddListener(OnBustButtonClick);
        peepButton.onClick.AddListener(OnPeepButtonClick);
    }
    void ComputeInitialPriorProbabilities()
    {
        float initialProbability = 1.0f / (columns * rows);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                probabilities[i, j].priorProbability = initialProbability;
            }
        }
    }

    public Vector2Int PlaceGhost()
    {
        Vector2Int newGhostLocation;
        newGhostLocation = new Vector2Int(UnityEngine.Random.Range(0, columns), UnityEngine.Random.Range(0, rows));
        return newGhostLocation;
    }

    GameObject SpawnCell(int x, int y)
    {
        GameObject cell = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
        cell.transform.SetParent(transform);
        cell.name = "Cell_" + x + "_" + y;

        Button button = cell.GetComponentInChildren<Button>();
        button.onClick.AddListener(() => OnCellButtonClick(x, y));

        return cell;
    }

    void OnCellButtonClick(int x, int y)
    {
        if (isBustModeActive)
        {
            AttemptToBustGhost(x, y);
        }
        else
        {
            if (!colorRevealed[x, y]) 
            {
                Color color = DistanceSense(x, y);
                Button button = grid[x, y].GetComponentInChildren<Button>();
                button.GetComponent<Image>().color = color;
                scoreManager.DecreaseScore();
                UpdatePosteriorGhostLocationProbabilities(color, x, y);
                UpdateProbabilityText();
                colorRevealed[x, y] = true;
                FindHighestProbabilityDirection(x, y);
            }
        }
    }

    Color DistanceSense(int x, int y)
    {
        int distance = Mathf.Abs(ghostLocation.x - x) + Mathf.Abs(ghostLocation.y - y);
        Debug.Log($"Distance from ghost: {distance}");

        Dictionary<Color, double> colorProbabilities = new Dictionary<Color, double>();

        if (distance >= 5)
        {
            colorProbabilities.Add(greenColor, 0.8);
            colorProbabilities.Add(yellowColor, 0.15);
            colorProbabilities.Add(orangeColor, 0.04);
            colorProbabilities.Add(redColor, 0.01);
        }
        else if (distance >= 3)
        {
            colorProbabilities.Add(yellowColor, 0.8);
            colorProbabilities.Add(orangeColor, 0.15);
            colorProbabilities.Add(redColor, 0.04);
            colorProbabilities.Add(greenColor, 0.01);
        }
        else if (distance >= 1)
        {
            colorProbabilities.Add(orangeColor, 0.8);
            colorProbabilities.Add(redColor, 0.15);
            colorProbabilities.Add(greenColor, 0.04);
            colorProbabilities.Add(yellowColor, 0.01);
        }
        else
        {
            colorProbabilities.Add(redColor, 0.8);
            colorProbabilities.Add(orangeColor, 0.15);
            colorProbabilities.Add(greenColor, 0.04);
            colorProbabilities.Add(yellowColor, 0.01);
        }

        double totalWeight = 0;
        foreach (var pair in colorProbabilities)
        {
            totalWeight += pair.Value;
        }

        double randomValue = UnityEngine.Random.value * totalWeight;

        foreach (var pair in colorProbabilities)
        {
            if (randomValue < pair.Value)
            {
                return pair.Key;
            }
            randomValue -= pair.Value;
        }

        return greenColor;
    }

    void UpdatePosteriorGhostLocationProbabilities(Color c, int xclk, int yclk)
    {
        float totalProbability = 0;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                int distance = Mathf.Abs(x - xclk) + Mathf.Abs(y - yclk);
                float likelihood = CalculateLikelihood(c, distance);

                probabilities[x, y].posteriorProbability = probabilities[x, y].priorProbability * likelihood;
                totalProbability += probabilities[x, y].posteriorProbability;
            }
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                probabilities[x, y].posteriorProbability /= totalProbability;
                probabilities[x, y].priorProbability = probabilities[x, y].posteriorProbability;
            }
        }
    }

    float CalculateLikelihood(Color c, int distance)
    {
        switch (distance)
        {
            case 0: return c == redColor ? 0.8f : 0.05f;
            case 1:
            case 2: return c == orangeColor ? 0.8f : 0.05f;
            case 3:
            case 4: return c == yellowColor ? 0.8f : 0.05f;
            default: return c == greenColor ? 0.8f : 0.05f;
        }
    }


    void UpdateProbabilityText()
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject cell = grid[i, j];
                TextMeshProUGUI textMesh = cell.GetComponentInChildren<TextMeshProUGUI>();
                textMesh.text = probabilities[i, j].posteriorProbability.ToString("0.0000");
            }
        }
    }

    void OnBustButtonClick()
    {
        isBustModeActive = !isBustModeActive;
        if (xMarkRenderer != null && xMarkRenderer.enabled == false)
            xMarkRenderer.enabled = true;
        else
            DisableXMark();
        if (isBustModeActive)
        {
            Debug.Log("Bust mode activated. Choose a cell to bust the ghost.");
        }
        else
        {
            Debug.Log("Bust mode deactivated.");
        }
    }
    void DisableXMark()
    {
        if (xMarkRenderer != null)
            xMarkRenderer.enabled = false;
    }

    void AttemptToBustGhost(int x, int y)
    {
        isBustModeActive = false;
        DisableXMark();

        if (x == ghostLocation.x && y == ghostLocation.y)
        {
            Debug.Log("Win! You've busted the ghost.");
            GameWin();
        }
        else
        {
            probabilities[x, y].posteriorProbability = 0;
            probabilities[x, y].priorProbability = 0;

            NormalizeProbabilitiesAfterBust(x, y);
            bustsManager.DecreaseBustAttempt();
            scoreManager.DecreaseScore(); 
            if (bustsManager.GetRemainingBusts() <= 0)
            {
                Debug.Log("Lose! You've run out of bust attempts.");
                GameOver();
            }
            else
            {
                Debug.Log($"Bust attempt failed. {bustsManager.GetRemainingBusts()} attempts remaining.");
            }
        }
    }
    void NormalizeProbabilitiesAfterBust(int bustedX, int bustedY)
    {
        float totalProbability = 0;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                totalProbability += probabilities[x, y].posteriorProbability;
            }
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (totalProbability > 0)
                {
                    probabilities[x, y].posteriorProbability /= totalProbability;
                }
                probabilities[x, y].priorProbability = probabilities[x, y].posteriorProbability;
            }
        }
        UpdateProbabilityText();
    }
    void FindHighestProbabilityDirection(int xclk, int yclk)
    {
        float upProbability = 0, downProbability = 0, leftProbability = 0, rightProbability = 0;
        for (int x = 0; x < columns; x++)
        {
            for (int y = yclk + 1; y < rows; y++)
            {
                if (Mathf.Abs(xclk - x) <= yclk - y)
                {
                    upProbability += probabilities[x, y].posteriorProbability;
                }
            }
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < yclk; y++)
            {
                if (Mathf.Abs(xclk - x) <= y - yclk)
                {
                    downProbability += probabilities[x, y].posteriorProbability;
                }
            }
        }

        for (int x = 0; x < xclk; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (Mathf.Abs(yclk - y) <= xclk - x)
                {
                    leftProbability += probabilities[x, y].posteriorProbability;
                }
            }
        }

        for (int x = xclk + 1; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (Mathf.Abs(yclk - y) <= x - xclk)
                {
                    rightProbability += probabilities[x, y].posteriorProbability;
                }
            }
        }

        string direction = "nowhere";
        float maxProbability = Mathf.Max(upProbability, downProbability, leftProbability, rightProbability);
        if (maxProbability == 0)
        {
            Debug.Log("All directions have zero probability.");
        }
        else if (maxProbability < probabilities[xclk, yclk].posteriorProbability)
        {
            Debug.Log("The highest probability is in the current cell.");
            N.SetActive(true);
            S.SetActive(true);
            W.SetActive(true);
            E.SetActive(true);
        }
        else
        {
            if (maxProbability == upProbability)
            {
                direction = "up";
                N.SetActive(true);
                S.SetActive(false);
                W.SetActive(false);
                E.SetActive(false);
            }
            else if (maxProbability == downProbability)
            {
                direction = "down";
                N.SetActive(false);
                S.SetActive(true);
                W.SetActive(false);
                E.SetActive(false);
            }
            else if (maxProbability == leftProbability)
            {
                direction = "left";
                N.SetActive(false);
                S.SetActive(false);
                W.SetActive(true);
                E.SetActive(false);
            }
            else if (maxProbability == rightProbability)
            {
                direction = "right";
                N.SetActive(false);
                S.SetActive(false);
                W.SetActive(false);
                E.SetActive(true);
            }

            Debug.Log("The highest probability is towards the " + direction + ".");
        }
    }

    void Update()
    {
        if (xMarkRenderer != null && xMarkRenderer.enabled)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            xMarkObject.transform.position = new Vector3(mousePosition.x, mousePosition.y, xMarkObject.transform.position.z);
        }
    }
    void OnPeepButtonClick()
    {
        isBustModeActive = false;
        DisableXMark();
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Button button = grid[i, j].GetComponentInChildren<Button>();
                TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.enabled = !text.enabled;
            }
        }


        Debug.Log("Peep button clicked");
    }
    void GameOver()
    {
        SceneManager.LoadScene("GameOverScene");
    }
    void GameWin()
    {
        int currentScore;
        currentScore = scoreManager.GetScore();
        GameData.FinalScore = currentScore;
        SceneManager.LoadScene("WinScene");
    }
}
