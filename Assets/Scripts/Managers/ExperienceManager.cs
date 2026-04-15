using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;


//Hacer que solo el server escuche y cambie la experiencia 

public class ExperienceManager : NetworkBehaviour
{
    [Header("Experience")]
    [SerializeField] private AnimationCurve experienceCurve;
    private int currentLevel = 1;
    [SerializeField] private int maxLevel = 10; // o el nivel mas alto del juego

    private int totalExperience = 0;
    private int previousLevelsExperience = 0;
    private int nextLevelsExperience = 0;

    [Header("Interface")]
    [SerializeField] private TextMeshProUGUI levelText;        // Nuevo: solo nivel
    [SerializeField] private TextMeshProUGUI xpText;           // Progreso XP
    [SerializeField] private Image experienceFill;             // Barra tipo Filled

    private void Awake()
    {
        BasicEventsManager.OnExperienceGain += AddExperience;
    }

    private void OnDestroy()
    {
        BasicEventsManager.OnExperienceGain -= AddExperience;
    }

    private void Start()
    {
        UpdateLevel();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.G))
        {
            BasicEventsManager.OnExperienceGain?.Invoke(4); // Test: ganar 4 XP
        }
    }

    private void AddExperience(int amount)
    {
        totalExperience += amount;
        UpdateInterface();
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        if (currentLevel >= maxLevel) return; // ya no sube más
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

    //Hacer rpc server -) a todos 
    private void UpdateInterface()
    {
        // XP dentro del nivel actual
        int xpCurrentLevel = totalExperience - previousLevelsExperience;
        int xpToNextLevel = nextLevelsExperience - previousLevelsExperience;

        // Barra de experiencia
        experienceFill.fillAmount = (float)xpCurrentLevel / xpToNextLevel;

        // Texto de XP: "45 / 100 XP"
        if (xpText != null)
            xpText.text = $"{xpCurrentLevel}XP";
            //xpText.text = $"{xpCurrentLevel} / {xpToNextLevel} XP";


        // Texto de nivel: "Level 3"
        if (levelText != null)
            levelText.text = $"{currentLevel}";
    }
}

