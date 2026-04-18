using System;
using UnityEngine;

namespace Game.CameraSystem
{
    /// <summary>
    /// Representa una configuración de cámara (pitch, yaw, distancia) con su propia
    /// duración y curva de easing. Se usa como destino de una transición suave.
    /// </summary>
    [Serializable]
    public class CameraPreset
    {
        [Tooltip("Nombre identificador del preset. Usalo desde código con TransitionToPreset(name).")]
        public string presetName = "Default";

        [Header("Rotación base (world-space)")]
        [Tooltip("Rotación en X (pitch): cuán 'picada' está la cámara. 90 = top-down puro, 45 = isométrica clásica.")]
        [Range(0f, 90f)]
        public float pitch = 45f;

        [Tooltip("Rotación en Y (yaw): hacia qué dirección mira la cámara en el plano horizontal. 0 = mira al norte (+Z).")]
        [Range(-180f, 180f)]
        public float yaw = 0f;

        [Header("Posicionamiento")]
        [Tooltip("Distancia desde el focus point hasta la cámara. Aumentala para 'alejar' la cámara.")]
        [Min(1f)]
        public float distance = 15f;

        [Header("Transición hacia este preset")]
        [Tooltip("Duración (segundos) de la transición cuando se activa este preset.")]
        [Min(0f)]
        public float transitionDuration = 1.5f;

        [Tooltip("Curva de easing para la transición. Por defecto, ease-in-out suave.")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    /// <summary>
    /// Una entrada de auto-transition que se dispara automáticamente al inicio del juego.
    /// </summary>
    [Serializable]
    public class AutoTransition
    {
        [Tooltip("Segundos desde el Start() del juego en los que se disparará este preset.")]
        [Min(0f)]
        public float triggerAtSeconds = 0f;

        [Tooltip("Nombre del preset al que transicionar. Debe coincidir con un preset de la lista.")]
        public string presetName;
    }
}