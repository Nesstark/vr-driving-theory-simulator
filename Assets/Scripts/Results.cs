using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Results : MonoBehaviour
{
    [SerializeField] private GameObject finePanel;
    [SerializeField] private TextMeshProUGUI fineText;
    [SerializeField] private GameObject[] penaltyIndicators;

    void Start()
    {
        fineText.text = PenaltyTracker.PenaltyFine.ToString();

        for (int i = 0; i < penaltyIndicators.Length; i++)
        {
            penaltyIndicators[i].SetActive(i < PenaltyTracker.PenaltyPoints);
        }
        
        if (PenaltyTracker.PenaltyFine == 0) {
            finePanel.SetActive(false);
        }
    }
}