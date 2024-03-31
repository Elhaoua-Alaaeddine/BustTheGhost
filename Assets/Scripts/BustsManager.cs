using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BustsManager : MonoBehaviour
{
    public TextMeshProUGUI bustsText;
    [SerializeField] int busts = 10;
    void Start()
    {
         UpdateBustsText();
    }

    public void DecreaseBustAttempt()
    {
        busts--;
        UpdateBustsText();
      
    }
    public int GetRemainingBusts()
    {
        return busts;
    }
    void UpdateBustsText()
    {
        bustsText.text = "Busts Remaining: " + busts.ToString();
    }
    void Update()
    {

    }
}
