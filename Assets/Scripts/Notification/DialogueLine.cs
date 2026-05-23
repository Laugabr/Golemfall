using UnityEngine;

// Una sola línea dentro de un diálogo.
// speakerName y portrait son opcionales — si están vacíos, la UI usa los del NPC.
[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;

    public Sprite portrait;             // opcional: foto del hablante
    public string speakerNameOverride;  // opcional: reemplaza el nombre del NPC
}
