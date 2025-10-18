using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ExperienceManager : MonoBehaviour
{
    [Header("Experience")]
    [SerializeField] AnimationCurve experienceCurve;
    int currentLevel, totalExperience;
    int previousLevelsExperience, nextLevelsExperience;


    [Header("Interface")]
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] Image experienceFill;

    

    private void AddExperience(int amount)
    {
        totalExperience += amount;
        UpdateInterface();
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        if (totalExperience >= nextLevelsExperience)
        {
            currentLevel++;
            UpdateLevel();
        }
    }

    private void UpdateLevel()
    {
        previousLevelsExperience = (int)experienceCurve.Evaluate(currentLevel);
        nextLevelsExperience = (int)experienceCurve.Evaluate(currentLevel + 1);
        BasicEventsManager.OnLevelUp?.Invoke(currentLevel);

        UpdateInterface();
    }

    private void UpdateInterface()
    {
        int start = totalExperience - previousLevelsExperience;
        int end = nextLevelsExperience - previousLevelsExperience;
        levelText.text = currentLevel.ToString();
        experienceFill.fillAmount = (float)start / (float)end;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            BasicEventsManager.OnExperienceGain?.Invoke(4);
        }
    }
    void Awake()
    {
        BasicEventsManager.OnExperienceGain += AddExperience;
    }

    void OnDestroy()
    {
        BasicEventsManager.OnExperienceGain -= AddExperience;

    }

    void Start()
    {
        UpdateLevel();
    }
}
