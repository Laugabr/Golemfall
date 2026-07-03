using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zona de música (versión por polling). Ya NO usa OnTriggerEnter/Exit: es un
/// marcador pasivo. El MusicDirector le pregunta "¿el jugador está dentro tuyo?"
/// cada tanto. Así los teleports/muertes/respawns no la descolocan: la música
/// siempre refleja dónde está el jugador de verdad.
///
/// Requiere un Collider (Is Trigger para que no choque con el player).
/// El chequeo usa el bounding box del collider: aproximado, pero de sobra para música.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    [Tooltip("Pista que suena mientras el jugador local está dentro.")]
    [SerializeField] private MusicTrack track;

    [Tooltip("Si dos zonas se solapan, gana la de mayor prioridad.")]
    [SerializeField] private int priority = 0;

    private Collider _col;

    public MusicTrack Track => track;
    public int Priority => priority;

    // Lista de zonas activas: el MusicDirector recorre solo esto (nada de FindObjects).
    public static readonly List<MusicZone> Active = new List<MusicZone>();

    private void Awake() => _col = GetComponent<Collider>();
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    /// <summary>True si el punto cae dentro de la zona.</summary>
    public bool Contains(Vector3 point) => _col.bounds.Contains(point);
}