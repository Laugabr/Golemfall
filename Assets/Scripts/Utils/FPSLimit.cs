using UnityEngine;

public class FPSLimit : MonoBehaviour
{
    private bool modoNetbook = false;

    void Start()
    {
        // Detección automática opcional: activa modo netbook si detecta
        // poca memoria de video o poca RAM del sistema
        if (SystemInfo.systemMemorySize < 8000 || SystemInfo.graphicsMemorySize < 2000)
        {
            AplicarModoNetbook();
        }
        else
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
        {
            modoNetbook = !modoNetbook;
            if (modoNetbook) AplicarModoNetbook();
            else AplicarModoNormal();
        }
    }

    void AplicarModoNetbook()
    {
        modoNetbook = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        // Bajá esto a un Quality Level configurado aparte con:
        // MSAA off/2x, Render Scale ~0.75-0.85, HDR off o 16-bit,
        // sombras de menor resolución, menos cascadas, etc.
        QualitySettings.SetQualityLevel(GetNetbookQualityIndex(), true);

        Debug.Log("Modo Netbook: 30fps + calidad reducida");
    }

    void AplicarModoNormal()
    {
        modoNetbook = false;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        QualitySettings.SetQualityLevel(GetDefaultQualityIndex(), true);
    }

    int GetNetbookQualityIndex() => 0; // ajustá según tu lista de Quality Levels
    int GetDefaultQualityIndex() => QualitySettings.names.Length - 1;
}