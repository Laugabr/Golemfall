using UnityEngine;

// Script TEMPORAL solo para probar el pipeline de audio. Borralo cuando funcione.
public class AudioSmokeTest : MonoBehaviour
{
    [SerializeField] private MusicTrack trackDePrueba;

    private void Start()
    {
        AudioManager.Instance.PlayMusic(trackDePrueba);
    }
}