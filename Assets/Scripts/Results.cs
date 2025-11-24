
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Results : MonoBehaviour
{
    [SerializeField] private GameObject finePanel;
    [SerializeField] private TextMeshProUGUI fineText;
    [SerializeField] private GameObject[] penaltyIndicators;

    [Header("Per-question panels (assign one per question)")]
    [Tooltip("Panels for each question. Set QuestionId in each element to match the saved QuestionResult.questionId")]
    [SerializeField]
    private QuestionPanel[] questionPanels = new QuestionPanel[0];

    // Options
    [Header("Options")]
    [Tooltip("If > 0, override the font size for all answer TextMeshPro fields in the panels")]
    [SerializeField] private float overrideAnswerFontSize = 0f;
    [Tooltip("Color used for correct answers (applied to the active indicator)")]
    [SerializeField] private Color correctIndicatorColor = new Color(0.2f, 0.8f, 0.2f);
    [Tooltip("Color used for incorrect answers (applied to the active indicator)")]
    [SerializeField] private Color incorrectIndicatorColor = new Color(0.9f, 0.2f, 0.2f);
    [Tooltip("Color used for missed correct answers (correct but not selected)")]
    [SerializeField] private Color missedCorrectIndicatorColor = new Color(1f, 0.85f, 0.2f);
    // The prefab/sprite approach was removed — use existing GameObjects in the scene for indicators.

    void Start()
    {
        SetFineAndPenalty();
        SetResultDisplay();
    }

    void SetFineAndPenalty()
    {
        fineText.text = PenaltyTracker.PenaltyFine.ToString();

        for (int i = 0; i < penaltyIndicators.Length; i++)
        {
            penaltyIndicators[i].SetActive(i < PenaltyTracker.PenaltyPoints);
        }

        if (PenaltyTracker.PenaltyFine == 0)
        {
            finePanel.SetActive(false);
        }
    }

    void SetResultDisplay()
    {
        // Populate each configured question panel from saved QuestionResults
        var allResults = QuestionResults.GetAllResults();

        // Build a lookup by questionId for quick access
        var map = new System.Collections.Generic.Dictionary<int, QuestionResult>();
        foreach (var r in allResults) map[r.questionId] = r;

        for (int p = 0; p < questionPanels.Length; p++)
        {
            var panel = questionPanels[p];
            if (panel == null || panel.root == null) continue;

            // Try to get saved result for this panel's question id
            if (!map.TryGetValue(panel.questionId, out QuestionResult result))
            {
                // No saved result: hide indicators and leave texts as-is
                if (panel.checkObjects != null)
                {
                    foreach (var go in panel.checkObjects) if (go != null) go.SetActive(false);
                }
                if (panel.crossObjects != null)
                {
                    foreach (var go in panel.crossObjects) if (go != null) go.SetActive(false);
                }
                continue;
            }

            // Populate texts from saved QuestionResult.buttonTexts.
            // Discover TextMeshProUGUI children under the panel root at runtime so we don't need
            // to assign them manually in the inspector.
            TMPro.TextMeshProUGUI[] discoveredTexts = new TMPro.TextMeshProUGUI[0];
            if (panel.root != null)
            {
                discoveredTexts = panel.root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            }

            int textsToSet = 0;
            if (result.buttonTexts != null) textsToSet = result.buttonTexts.Length;
            int maxTextCount = Mathf.Min(discoveredTexts.Length, textsToSet);
            for (int i = 0; i < maxTextCount; i++)
            {
                if (discoveredTexts[i] == null) continue;
                discoveredTexts[i].text = result.buttonTexts[i] ?? string.Empty;
            }

            // Apply global font size override if requested to discovered texts
            if (overrideAnswerFontSize > 0f && discoveredTexts != null)
            {
                for (int i = 0; i < discoveredTexts.Length; i++)
                {
                    if (discoveredTexts[i] == null) continue;
                    discoveredTexts[i].fontSize = overrideAnswerFontSize;
                }
            }

            // Determine how many answer entries to iterate: prefer saved texts length, otherwise use indicator lengths
            int textsLen = (result.buttonTexts != null) ? result.buttonTexts.Length : 0;
            int maxLen = Mathf.Max(Mathf.Max(panel.checkObjects?.Length ?? 0, panel.crossObjects?.Length ?? 0), textsLen);
            for (int i = 0; i < maxLen; i++)
            {
                bool isCorrect = result.correctButtonIndices != null && System.Array.IndexOf(result.correctButtonIndices, i) >= 0;
                bool isSelected = result.selectedButtonIndices != null && System.Array.IndexOf(result.selectedButtonIndices, i) >= 0;

                // Indicator handling using simple GameObjects (check / cross) assigned in the panel
                GameObject checkObj = (panel.checkObjects != null && i < panel.checkObjects.Length) ? panel.checkObjects[i] : null;
                GameObject crossObj = (panel.crossObjects != null && i < panel.crossObjects.Length) ? panel.crossObjects[i] : null;

                // New requested logic (swapped):
                // - ICON/SHAPE is based on correctness: correct -> check, incorrect -> cross
                // - COLOR is based on selection state:
                //     selected correct -> green
                //     missed correct (correct but not selected) -> yellow
                //     incorrect -> red
                Color indicatorColor;
                if (isCorrect)
                {
                    indicatorColor = isSelected ? correctIndicatorColor : missedCorrectIndicatorColor;

                    // show check icon for correct answers
                    if (checkObj != null)
                    {
                        checkObj.SetActive(true);
                        SetIndicatorColor(checkObj, indicatorColor);
                    }
                    if (crossObj != null) crossObj.SetActive(false);
                }
                else
                {
                    indicatorColor = incorrectIndicatorColor;

                    // show cross icon for incorrect answers
                    if (crossObj != null)
                    {
                        crossObj.SetActive(true);
                        SetIndicatorColor(crossObj, indicatorColor);
                    }
                    if (checkObj != null) checkObj.SetActive(false);
                }
            }
        }
    }

    [System.Serializable]
    private class QuestionPanel
    {
        [Tooltip("The questionId that corresponds to this panel (matches QuestionResult.questionId)")]
        public int questionId = 0;
        [Tooltip("Root GameObject for the panel (optional)")]
        public GameObject root;
        [Tooltip("GameObjects for check indicators (one per answer). These will be enabled/disabled by the script.")]
        public GameObject[] checkObjects = new GameObject[4];
        [Tooltip("GameObjects for cross indicators (one per answer). These will be enabled/disabled by the script.")]
        public GameObject[] crossObjects = new GameObject[4];
    }

    // Try to set the color on a variety of common indicator types (UI Image, SpriteRenderer, Renderer).
    private void SetIndicatorColor(GameObject obj, Color color)
    {
        if (obj == null) return;

        // First try UI Image
        var img = obj.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
            return;
        }

        // Try TextMeshProUGUI (if icon is implemented as text glyph)
        var tmp = obj.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = color;
            return;
        }

        // SpriteRenderer for world-space sprites
        var spr = obj.GetComponent<SpriteRenderer>();
        if (spr != null)
        {
            spr.color = color;
            return;
        }

        // Fallback to generic Renderer (materials)
        var rend = obj.GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            try
            {
                rend.material.color = color;
            }
            catch { }
        }
        else
        {
            // Try children
            var childImg = obj.GetComponentInChildren<Image>();
            if (childImg != null) { childImg.color = color; return; }
            var childSpr = obj.GetComponentInChildren<SpriteRenderer>();
            if (childSpr != null) { childSpr.color = color; return; }
        }
    }
}