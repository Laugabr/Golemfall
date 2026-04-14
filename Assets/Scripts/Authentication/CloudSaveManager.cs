using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CloudSaveManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField ageInput;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Display")]
    [SerializeField] private TMP_Text greetingText;
    [SerializeField] private TMP_Text statusText;

    // Claves para Cloud Save
    private const string KEY_NAME = "player_name";
    private const string KEY_AGE = "player_age";

    private async void Start()
    {
        // Al entrar a la escena, el usuario ya está autenticado desde AuthManager.
        // Intentamos cargar datos existentes para saludarlo.
        statusText.text = "Cargando datos...";
        await LoadPlayerData();
    }

    // ──────────────────────────────────────────────
    //  GUARDAR
    // ──────────────────────────────────────────────
    public async void OnSaveButton()
    {
        string playerName = nameInput.text.Trim();
        string ageText = ageInput.text.Trim();

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(ageText))
        {
            statusText.text = "Por favor completá nombre y edad.";
            return;
        }

        if (!int.TryParse(ageText, out int playerAge))
        {
            statusText.text = "La edad debe ser un número.";
            return;
        }

        await SavePlayerData(playerName, playerAge);
    }

    private async Task SavePlayerData(string playerName, int playerAge)
    {
        try
        {
            // Creamos el diccionario con los datos a guardar
            var data = new Dictionary<string, object>
            {
                { KEY_NAME, playerName },
                { KEY_AGE, playerAge }
            };

            // Guardamos en Cloud Save (Player Data, sin write lock)
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            statusText.text = "Datos guardados correctamente.";
            Debug.Log($"Cloud Save: Guardado {playerName}, {playerAge}");

            // Actualizamos el saludo
            ShowGreeting(playerName, playerAge);
        }
        catch (CloudSaveValidationException e)
        {
            statusText.text = "Error de validación al guardar.";
            Debug.LogError(e);
        }
        catch (CloudSaveRateLimitedException e)
        {
            statusText.text = "Demasiadas solicitudes, intentá de nuevo.";
            Debug.LogError(e);
        }
        catch (CloudSaveException e)
        {
            statusText.text = "Error al guardar: " + e.Message;
            Debug.LogError(e);
        }
    }

    // ──────────────────────────────────────────────
    //  CARGAR
    // ──────────────────────────────────────────────
    public async void OnLoadButton()
    {
        statusText.text = "Cargando datos...";
        await LoadPlayerData();
    }

    private async Task LoadPlayerData()
    {
        try
        {
            // Pedimos solo las claves que nos interesan
            var keys = new HashSet<string> { KEY_NAME, KEY_AGE };
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (results.TryGetValue(KEY_NAME, out var nameItem) &&
                results.TryGetValue(KEY_AGE, out var ageItem))
            {
                string playerName = nameItem.Value.GetAs<string>();
                int playerAge = ageItem.Value.GetAs<int>();

                Debug.Log($"Cloud Save: Cargado {playerName}, {playerAge}");
                ShowGreeting(playerName, playerAge);
                statusText.text = "Datos cargados.";
            }
            else
            {
                greetingText.text = "No hay datos guardados todavía.";
                statusText.text = "Ingresá tu nombre y edad para comenzar.";
            }
        }
        catch (CloudSaveValidationException e)
        {
            statusText.text = "Error de validación al cargar.";
            Debug.LogError(e);
        }
        catch (CloudSaveRateLimitedException e)
        {
            statusText.text = "Demasiadas solicitudes, intentá de nuevo.";
            Debug.LogError(e);
        }
        catch (CloudSaveException e)
        {
            statusText.text = "Error al cargar: " + e.Message;
            Debug.LogError(e);
        }
    }

    // ──────────────────────────────────────────────
    //  SALUDO
    // ──────────────────────────────────────────────
    private void ShowGreeting(string playerName, int playerAge)
    {
        greetingText.text = $"¡Te damos la bienvenida a Golemfall, {playerName}! Tu edad en este mundo es: {playerAge} años.";
    }
}