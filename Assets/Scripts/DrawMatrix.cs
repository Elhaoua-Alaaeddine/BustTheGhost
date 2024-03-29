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
    public GameObject[,] grid; // 2D array to store references to cell GameObjects
    public ScoreManager scoreManager; // Reference to the ScoreManager script
    public BustsManager bustsManager; // Reference to the BustsManager script

    public GameObject xMarkObject; // Reference to the GameObject containing the X mark sprite

    private SpriteRenderer xMarkRenderer; // Reference to the SpriteRenderer of the X mark GameObject
    public GameObject N;
    public GameObject S;
    public GameObject W;
    public GameObject E;

    // private SpriteRenderer NRenderer;
    // private SpriteRenderer SRenderer;
    // private SpriteRenderer WRenderer;
    // private SpriteRenderer ERenderer;


    public Color greenColor;
    public Color yellowColor;
    public Color orangeColor;
    public Color redColor;
    bool[,] colorRevealed; // 2D array to store whether the color of each cell has been revealed

    int columns = 12;
    int rows = 9;
    ProbabilityData[,] probabilities; // 2D array to store prior and posterior probabilities
    Vector2Int ghostLocation; // Coordinates of the "ghost" cell
    [SerializeField] GameObject cellPrefab;
    [SerializeField] Button bustButton;
    [SerializeField] Button peepButton;

    void Start()
    {
        ghostLocation = PlaceGhost();
        // Get the SpriteRenderer component of the X mark GameObject
        xMarkRenderer = xMarkObject.GetComponent<SpriteRenderer>();
        // NRenderer = N.GetComponent<SpriteRenderer>();
        // SRenderer = S.GetComponent<SpriteRenderer>();
        // WRenderer = W.GetComponent<SpriteRenderer>();
        // ERenderer = E.GetComponent<SpriteRenderer>();

        // Initialize the grid array
        grid = new GameObject[columns, rows];
        colorRevealed = new bool[columns, rows];
        probabilities = new ProbabilityData[columns, rows];
        ComputeInitialPriorProbabilities();
        // Spawn cells
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

        // Deactivate the X mark at the start
        if (xMarkRenderer != null)
            xMarkRenderer.enabled = false;
        // Add click event listeners to bust and peep buttons
        bustButton.onClick.AddListener(OnBustButtonClick);
        peepButton.onClick.AddListener(OnPeepButtonClick);
    }
    void ComputeInitialPriorProbabilities()
    {
        float initialProbability = 1.0f / (columns * rows);

        // Set the same initial prior probability for all locations
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                probabilities[i, j].priorProbability = initialProbability;
            }
        }
    }

    //PlaceGhost function places the ghost ad returns xg and yg
    public Vector2Int PlaceGhost()
    {
        Vector2Int newGhostLocation;
        // Randomly select a location for the ghost
        newGhostLocation = new Vector2Int(UnityEngine.Random.Range(0, columns), UnityEngine.Random.Range(0, rows));
        return newGhostLocation;
    }

    GameObject SpawnCell(int x, int y)
    {
        GameObject cell = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
        cell.transform.SetParent(transform); // Set the cell's parent to the DrawMatrix object
        cell.name = "Cell_" + x + "_" + y;

        // Add a click event listener to the cell's button
        Button button = cell.GetComponentInChildren<Button>();
        button.onClick.AddListener(() => OnCellButtonClick(x, y));

        return cell;
    }

    void OnCellButtonClick(int x, int y)
    {
        if (isBustModeActive)
        {
            // Perform bust logic
            AttemptToBustGhost(x, y);
        }
        else
        {
            if (!colorRevealed[x, y]) // Check if color has not been revealed
            {
                // Determine color based on distance using weighted random selection
                Color color = DistanceSense(x, y);
                // Get the button component of the clicked cell and change its color
                Button button = grid[x, y].GetComponentInChildren<Button>();
                button.GetComponent<Image>().color = color;
                // Disable the button to prevent further clicks
                // button.interactable = false;
                // Decrease the score using the ScoreManager
                scoreManager.DecreaseScore();
                // Update posterior probabilities based on the sensed color
                UpdatePosteriorGhostLocationProbabilities(color, x, y);
                // Update the probability text for all cells
                UpdateProbabilityText();
                // Mark the cell as having its color revealed
                colorRevealed[x, y] = true;
                FindHighestProbabilityDirection(x, y);
            }
        }
    }

    Color DistanceSense(int x, int y)
    {
        // Calculate the Manhattan distance between the clicked cell and the ghost cell
        int distance = Mathf.Abs(ghostLocation.x - x) + Mathf.Abs(ghostLocation.y - y);
        // Log distance from ghost
        Debug.Log($"Distance from ghost: {distance}");

        // Define color probabilities based on distance
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

        // Perform weighted random selection
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

        // Should not reach here, but return default color just in case
        return greenColor;
    }

    void UpdatePosteriorGhostLocationProbabilities(Color c, int xclk, int yclk)
    {
        float totalProbability = 0;

        // Update the probabilities based on the sensed color and distance to each cell
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

        // Normalize the probabilities
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                probabilities[x, y].posteriorProbability /= totalProbability;
                // Update the prior probabilities for the next round
                probabilities[x, y].priorProbability = probabilities[x, y].posteriorProbability;
            }
        }
    }

    // Example likelihood calculation based on color and distance
    // This function needs to be implemented based on the game's design
    float CalculateLikelihood(Color c, int distance)
    {
        // This should be replaced with the actual logic to calculate the likelihood
        // based on the distance and sensed color. Here's a simplified version:
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
        // Update the probability text for all cells
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
        // Enable the SpriteRenderer to show the X mark
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
        // Disable the SpriteRenderer to hide the X mark
        if (xMarkRenderer != null)
            xMarkRenderer.enabled = false;
    }

    void AttemptToBustGhost(int x, int y)
    {
        // Deactivate bust mode for the next action
        isBustModeActive = false;
        DisableXMark();

        // Check if the clicked cell contains the ghost
        if (x == ghostLocation.x && y == ghostLocation.y)
        {
            Debug.Log("Win! You've busted the ghost.");
            GameWin();
        }
        else
        {
            // Set the busted cell's probability to zero
            probabilities[x, y].posteriorProbability = 0;
            probabilities[x, y].priorProbability = 0;

            // Normalize the probabilities for all other cells
            NormalizeProbabilitiesAfterBust(x, y);
            // the ghost is not here so the probability in this cell should be 0
            bustsManager.DecreaseBustAttempt(); // Decrease the number of bust attempts
            scoreManager.DecreaseScore(); // Decrease the score
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

        // Normalize probabilities of all cells except the busted one
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (totalProbability > 0) // Other cells
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
        // If X mark is active, move it with the cursor
        if (xMarkRenderer != null && xMarkRenderer.enabled)
        {
            // Get the mouse position in world coordinates
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Set the position of the X mark object
            xMarkObject.transform.position = new Vector3(mousePosition.x, mousePosition.y, xMarkObject.transform.position.z);
        }
    }
    void OnPeepButtonClick()
    {
        isBustModeActive = false;
        DisableXMark();
        //switch the text on and off
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Button button = grid[i, j].GetComponentInChildren<Button>();
                TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.enabled = !text.enabled;
            }
        }


        // Log peep action
        Debug.Log("Peep button clicked");
    }
    void GameOver()
    {
        // Load the game over scene
        SceneManager.LoadScene("GameOverScene");
    }
    void GameWin()
    {
        int currentScore;
        currentScore = scoreManager.GetScore();
        GameData.FinalScore = currentScore;
        // Load the game win scene
        SceneManager.LoadScene("WinScene");
    }
}
