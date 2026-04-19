using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CloudSaveManager : MonoBehaviour
{
    [Header("Input Fields (dentro de PanelStart)")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField ageInput;

    [Header("Textos (fuera de PanelStart)")]
    [SerializeField] private TMP_Text greetingText;
    [SerializeField] private TMP_Text statusText;

    [Header("Saludo temporal")]
    [SerializeField] private float greetingDuration = 5f;

    private const string KEY_NAME = "player_name";
    private const string KEY_AGE = "player_age";

    private async void Start()
    {
        greetingText.text = "";
        statusText.text = "";

        await LoadPlayerData();
    }


    public async void OnSaveButton()
    {
        string playerName = nameInput.text.Trim();
        string ageText = ageInput.text.Trim();

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(ageText))
        {
            statusText.text = "Completá nombre y edad.";
            return;
        }

        if (!int.TryParse(ageText, out int playerAge))
        {
            statusText.text = "La edad debe ser un número.";
            return;
        }

        try
        {
            var data = new Dictionary<string, object>
            {
                { KEY_NAME, playerName },
                { KEY_AGE, playerAge }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log($"[CloudSave] Guardado: {playerName}, {playerAge}");

            statusText.text = "Datos guardados.";
            ShowGreeting(playerName, playerAge);
        }
        catch (CloudSaveException e)
        {
            statusText.text = "Error al guardar.";
            Debug.LogError(e);
        }
    }

    // ── CARGAR (automático al entrar) ────────────
    private async Task LoadPlayerData()
    {
        try
        {
            var keys = new HashSet<string> { KEY_NAME, KEY_AGE };
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (results.TryGetValue(KEY_NAME, out var nameItem) &&
                results.TryGetValue(KEY_AGE, out var ageItem))
            {
                string playerName = nameItem.Value.GetAs<string>();
                int playerAge = ageItem.Value.GetAs<int>();

                Debug.Log($"[CloudSave] Cargado: {playerName}, {playerAge}");
                ShowGreeting(playerName, playerAge);
            }
            else
            {
                statusText.text = "Ingresá tu nombre y edad para comenzar.";
            }
        }
        catch (CloudSaveException e)
        {
            statusText.text = "Ingresá tu nombre y edad para comenzar.";
            Debug.Log("[CloudSave] Sin datos previos: " + e.Message);
        }
    }

    // ── SALUDO (aparece y desaparece solo) ────────
    private void ShowGreeting(string playerName, int playerAge)
    {
        greetingText.text = $"¡Te damos la bienvenida a Golemfall, {playerName}!\nTu edad en este mundo es: {playerAge} años.";
        StartCoroutine(HideGreetingAfterDelay());
    }

    private IEnumerator HideGreetingAfterDelay()
    {
        yield return new WaitForSeconds(greetingDuration);
        greetingText.text = "";
    }
}