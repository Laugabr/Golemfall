using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraSystem
{
    /// <summary>
    /// Cámara con:
    ///   - Seguimiento por zona muerta world-space (edge-scrolling).
    ///   - Rotación world-space fija (no rota por el movimiento del jugador).
    ///   - Sistema de presets con transiciones suaves programables.
    ///   - Input manual de rotación con offset aditivo (teclas 8/2 pitch, 4/6 yaw, 0 reset).
    ///
    /// IMPORTANTE: La rotación visible de la cámara es la suma de:
    ///   (preset actual / transición en curso)  +  (offset manual del usuario)
    /// Esto permite que el usuario rote manualmente sin romper las transiciones
    /// programadas, y que el reset con '0' devuelva la cámara al "base" sin más.
    ///
    /// Integración con Photon Fusion 2: este componente debe estar habilitado únicamente
    /// en el cliente que tiene autoridad de input sobre el jugador. Ver SetTarget().
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Target y seguimiento
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Target")]
        [Tooltip("Jugador (u otro objeto) al que la cámara sigue. Si es null al Start, no sigue a nadie.")]
        [SerializeField] private Transform target;

        [Header("Zona muerta world-space")]
        [Tooltip("Mitad del ancho (eje X del mundo) de la zona muerta. El jugador puede moverse libremente dentro de este rectángulo sin que la cámara se mueva.")]
        [Min(0f)]
        [SerializeField] private float deadZoneHalfWidth = 4f;

        [Tooltip("Mitad del largo (eje Z del mundo) de la zona muerta.")]
        [Min(0f)]
        [SerializeField] private float deadZoneHalfLength = 3f;

        [Header("Suavizado del seguimiento")]
        [Tooltip("Tiempo aproximado (segundos) que tarda la cámara en alcanzar al jugador tras salirse de la zona muerta. Más alto = más delay / más 'pesado'.")]
        [Min(0f)]
        [SerializeField] private float followSmoothTime = 0.35f;

        [Tooltip("Velocidad máxima (unidades/s) con la que la cámara se puede mover siguiendo al jugador. Evita tirones con saltos grandes.")]
        [Min(0f)]
        [SerializeField] private float followMaxSpeed = 30f;

        // ─────────────────────────────────────────────────────────────────────────
        // Presets y transiciones
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Presets de cámara")]
        [Tooltip("Lista de configuraciones de cámara (pitch/yaw/distancia). Se invocan por nombre desde código con TransitionToPreset(name).")]
        [SerializeField] private List<CameraPreset> presets = new List<CameraPreset>();

        [Tooltip("Índice del preset inicial que se aplica instantáneamente (sin transición) al arrancar.")]
        [SerializeField] private int initialPresetIndex = 0;

        [Header("Auto-transitions al inicio del juego")]
        [Tooltip("Lista de transiciones que se disparan solas al Start(). Ej: {0s → 'Intro', 10s → 'Normal'}.")]
        [SerializeField] private List<AutoTransition> autoTransitions = new List<AutoTransition>();

        // ─────────────────────────────────────────────────────────────────────────
        // Input manual
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Input manual (teclado numérico)")]
        [Tooltip("Velocidad de rotación manual al mantener las teclas (grados/segundo).")]
        [Min(0f)]
        [SerializeField] private float manualRotationSpeed = 45f;

        [Tooltip("Offset máximo de pitch (grados) que el usuario puede aplicar sobre la rotación base.")]
        [SerializeField] private Vector2 manualPitchRange = new Vector2(-20f, 20f);

        [Tooltip("Offset máximo de yaw (grados) que el usuario puede aplicar sobre la rotación base.")]
        [SerializeField] private Vector2 manualYawRange = new Vector2(-45f, 45f);

        [Tooltip("Tiempo (segundos) que tarda el reset con '0' en devolver el offset a cero.")]
        [Min(0f)]
        [SerializeField] private float manualResetDuration = 0.3f;

        // ─────────────────────────────────────────────────────────────────────────
        // Debug
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Debug")]
        [Tooltip("Dibuja la zona muerta y el focus point en la Scene view.")]
        [SerializeField] private bool drawGizmos = true;

        // ─────────────────────────────────────────────────────────────────────────
        // Estado interno
        // ─────────────────────────────────────────────────────────────────────────

        // Focus point = el "centro" que la cámara intenta seguir en el plano XZ.
        // La cámara real se posiciona a cierta distancia de este punto, según pitch/yaw.
        private Vector3 focusPoint;
        private Vector3 focusVelocity; // usado por SmoothDamp

        // Estado actual de la rotación "base" (lo que dicta el preset actual o la transición en curso).
        private float basePitch;
        private float baseYaw;
        private float baseDistance;

        // Offset manual del usuario (se suma a la base).
        private float manualPitchOffset;
        private float manualYawOffset;

        // Corrutinas activas (las guardamos para poder cancelarlas si se dispara otra transición).
        private Coroutine transitionRoutine;
        private Coroutine manualResetRoutine;

        // ─────────────────────────────────────────────────────────────────────────
        // Ciclo de vida
        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            // Aplicar preset inicial de golpe (sin transición) para no ver un "snap" feo al arrancar.
            if (presets.Count > 0 && initialPresetIndex >= 0 && initialPresetIndex < presets.Count)
            {
                var initial = presets[initialPresetIndex];
                basePitch = initial.pitch;
                baseYaw = initial.yaw;
                baseDistance = initial.distance;
            }

            // Inicializar el focus point en la posición del target (si hay) para que no haya salto.
            if (target != null)
            {
                focusPoint = target.position;
            }

            // Aplicar transform inmediatamente con los valores iniciales.
            ApplyTransform(instant: true);

            // Arrancar las auto-transitions.
            if (autoTransitions.Count > 0)
            {
                StartCoroutine(RunAutoTransitions());
            }
        }

        private void LateUpdate()
        {
            // LateUpdate es el lugar correcto para cámaras: corre después de que todos los
            // Update() del frame movieron a sus entidades, así la cámara ve la posición final.

            UpdateFocusPoint();
            HandleManualInput();
            ApplyTransform(instant: false);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // API pública
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Setea el target al que sigue la cámara. Llamar desde el jugador local en
        /// Fusion 2: if (Object.HasInputAuthority) camera.SetTarget(transform);
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                focusPoint = target.position;
                focusVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Transiciona suavemente al preset indicado (busca por nombre).
        /// Si hay otra transición en curso, la cancela.
        /// </summary>
        public void TransitionToPreset(string presetName, float? overrideDuration = null)
        {
            var preset = presets.Find(p => p.presetName == presetName);
            if (preset == null)
            {
                Debug.LogWarning($"[CameraController] Preset '{presetName}' no encontrado.");
                return;
            }
            TransitionToPreset(preset, overrideDuration);
        }

        /// <summary>
        /// Transiciona suavemente al preset indicado (por índice en la lista).
        /// </summary>
        public void TransitionToPreset(int index, float? overrideDuration = null)
        {
            if (index < 0 || index >= presets.Count)
            {
                Debug.LogWarning($"[CameraController] Índice de preset fuera de rango: {index}");
                return;
            }
            TransitionToPreset(presets[index], overrideDuration);
        }

        private void TransitionToPreset(CameraPreset preset, float? overrideDuration)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }
            float duration = overrideDuration ?? preset.transitionDuration;
            transitionRoutine = StartCoroutine(TransitionRoutine(preset, duration));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Seguimiento: zona muerta + smoothing
        // ─────────────────────────────────────────────────────────────────────────

        private void UpdateFocusPoint()
        {
            if (target == null) return;

            // Calculamos el desvío (en XZ) entre el target y el focus point actual.
            Vector3 targetPos = target.position;
            float dx = targetPos.x - focusPoint.x;
            float dz = targetPos.z - focusPoint.z;

            // Computamos hacia dónde debería ir el focus point: el punto MÁS CERCANO posible
            // al focus actual que meta al target dentro del rectángulo de zona muerta.
            // Equivalente: si el target está fuera del rect, mover el focus lo justo para que
            // el target quede en el borde del rect. Si está dentro, el focus no se mueve.
            float desiredX = focusPoint.x;
            float desiredZ = focusPoint.z;

            if (dx > deadZoneHalfWidth)      desiredX = targetPos.x - deadZoneHalfWidth;
            else if (dx < -deadZoneHalfWidth) desiredX = targetPos.x + deadZoneHalfWidth;

            if (dz > deadZoneHalfLength)      desiredZ = targetPos.z - deadZoneHalfLength;
            else if (dz < -deadZoneHalfLength) desiredZ = targetPos.z + deadZoneHalfLength;

            // Mantenemos la Y del focus point al nivel del target (útil si el terreno tiene altura).
            Vector3 desiredFocus = new Vector3(desiredX, targetPos.y, desiredZ);

            // Smoothing: SmoothDamp da un "delay" natural con critical damping (sin overshoot).
            focusPoint = Vector3.SmoothDamp(
                focusPoint,
                desiredFocus,
                ref focusVelocity,
                followSmoothTime,
                followMaxSpeed
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Input manual (teclado numérico)
        // ─────────────────────────────────────────────────────────────────────────

        private void HandleManualInput()
        {
            float dt = Time.deltaTime;

            // Reset con '0': dispara una corrutina que interpola ambos offsets a cero.
            // Usamos KeyCode.Keypad0 (numpad). Si preferís también la '0' normal, agregá || Input.GetKeyDown(KeyCode.Alpha0).
            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                if (manualResetRoutine != null) StopCoroutine(manualResetRoutine);
                manualResetRoutine = StartCoroutine(ResetManualOffsetsRoutine());
                return; // no procesamos otras teclas en el mismo frame que el reset
            }

            // Si alguna tecla de rotación está presionada, cancelamos un reset en curso
            // (prioridad al input del usuario).
            bool anyKey =
                Input.GetKey(KeyCode.Keypad8) || Input.GetKey(KeyCode.Keypad2) ||
                Input.GetKey(KeyCode.Keypad4) || Input.GetKey(KeyCode.Keypad6);

            if (anyKey && manualResetRoutine != null)
            {
                StopCoroutine(manualResetRoutine);
                manualResetRoutine = null;
            }

            // Pitch: 8 sube la cámara (mira más desde arriba), 2 la baja.
            if (Input.GetKey(KeyCode.Keypad8)) manualPitchOffset += manualRotationSpeed * dt;
            if (Input.GetKey(KeyCode.Keypad2)) manualPitchOffset -= manualRotationSpeed * dt;

            // Yaw: 4 rota a la izquierda, 6 a la derecha.
            if (Input.GetKey(KeyCode.Keypad4)) manualYawOffset -= manualRotationSpeed * dt;
            if (Input.GetKey(KeyCode.Keypad6)) manualYawOffset += manualRotationSpeed * dt;

            // Clamp a los rangos configurados.
            manualPitchOffset = Mathf.Clamp(manualPitchOffset, manualPitchRange.x, manualPitchRange.y);
            manualYawOffset = Mathf.Clamp(manualYawOffset, manualYawRange.x, manualYawRange.y);
        }

        private IEnumerator ResetManualOffsetsRoutine()
        {
            float startPitch = manualPitchOffset;
            float startYaw = manualYawOffset;
            float elapsed = 0f;

            while (elapsed < manualResetDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / manualResetDuration);
                // Ease-out para que el último tramo sea suave (t rápido al principio, lento al final).
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                manualPitchOffset = Mathf.Lerp(startPitch, 0f, eased);
                manualYawOffset = Mathf.Lerp(startYaw, 0f, eased);
                yield return null;
            }

            manualPitchOffset = 0f;
            manualYawOffset = 0f;
            manualResetRoutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Transiciones de preset
        // ─────────────────────────────────────────────────────────────────────────

        private IEnumerator TransitionRoutine(CameraPreset target, float duration)
        {
            float startPitch = basePitch;
            float startYaw = baseYaw;
            float startDistance = baseDistance;
            float elapsed = 0f;

            // Duración de 0 → salto instantáneo.
            if (duration <= 0f)
            {
                basePitch = target.pitch;
                baseYaw = target.yaw;
                baseDistance = target.distance;
                transitionRoutine = null;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = target.transitionCurve.Evaluate(t);

                basePitch = Mathf.Lerp(startPitch, target.pitch, eased);
                baseYaw = Mathf.LerpAngle(startYaw, target.yaw, eased); // LerpAngle: elige el camino más corto
                baseDistance = Mathf.Lerp(startDistance, target.distance, eased);
                yield return null;
            }

            basePitch = target.pitch;
            baseYaw = target.yaw;
            baseDistance = target.distance;
            transitionRoutine = null;
        }

        private IEnumerator RunAutoTransitions()
        {
            // Ordenamos por tiempo ascendente para poder esperar incrementalmente.
            var sorted = new List<AutoTransition>(autoTransitions);
            sorted.Sort((a, b) => a.triggerAtSeconds.CompareTo(b.triggerAtSeconds));

            float timeWaited = 0f;
            foreach (var auto in sorted)
            {
                float waitFor = auto.triggerAtSeconds - timeWaited;
                if (waitFor > 0f) yield return new WaitForSeconds(waitFor);
                timeWaited = auto.triggerAtSeconds;

                if (!string.IsNullOrEmpty(auto.presetName))
                {
                    TransitionToPreset(auto.presetName);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Aplicación del transform final
        // ─────────────────────────────────────────────────────────────────────────

        private void ApplyTransform(bool instant)
        {
            // Rotación final = base (preset/transición) + offset manual del usuario.
            float finalPitch = basePitch + manualPitchOffset;
            float finalYaw = baseYaw + manualYawOffset;

            // Calculamos la posición: partiendo del focus point, nos alejamos "hacia atrás" según
            // pitch/yaw a la distancia configurada. Truco clásico: construir un quaternion con la
            // rotación deseada, multiplicarlo por Vector3.back * distancia, y sumarlo al focus.
            Quaternion rot = Quaternion.Euler(finalPitch, finalYaw, 0f);
            Vector3 offsetFromFocus = rot * Vector3.back * baseDistance;
            Vector3 desiredCamPos = focusPoint + offsetFromFocus;

            transform.position = desiredCamPos;
            transform.rotation = rot;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Gizmos (debug visual en el editor)
        // ─────────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // Dibuja el rectángulo de zona muerta en el plano XZ, a la altura del focus point.
            // En editor (sin target) usamos la posición del transform como referencia.
            Vector3 center = Application.isPlaying ? focusPoint : transform.position;

            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Vector3 size = new Vector3(deadZoneHalfWidth * 2f, 0.05f, deadZoneHalfLength * 2f);
            Gizmos.DrawWireCube(center, size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(center, 0.2f);
        }
    }
}