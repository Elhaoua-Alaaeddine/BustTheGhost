using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class WinSceneController : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Assign this in the Inspector

    private void Start()
    {
        scoreText.text = "Score: " + GameData.FinalScore.ToString();
    }
}

