using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraSystem
{
    /// <summary>
    /// Cámara con:
    ///   - Seguimiento por zona muerta world-space.
    ///   - Rotación orbital con botón derecho del mouse (yaw + pitch), cursor libre.
    ///   - Sistema de presets con transiciones suaves programables.
    ///   - Reset de offset con la tecla End.
    ///   - Propiedad WorldYaw para movimiento WASD relativo a la cámara:
    ///     NetCharacterController rota el input por este valor → W siempre
    ///     empuja al jugador hacia donde mira la cámara.
    ///
    /// ROTACIÓN CON MOUSE:
    ///   Mientras se mantiene apretado el botón derecho, el delta del mouse
    ///   acumula un offset de yaw (horizontal) y pitch (vertical) sobre la
    ///   rotación base del preset actual. El offset se aplica con SmoothDamp
    ///   para que el movimiento tenga inercia natural (estilo BG3 / Last Epoch).
    ///
    /// Integración Photon Fusion 2: habilitar solo en el cliente con input authority.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Target y seguimiento
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Target")]
        [Tooltip("Jugador al que la cámara sigue. Si es null al Start, no sigue a nadie.")]
        [SerializeField] private Transform target;

        [Header("Zona muerta world-space")]
        [Tooltip("Mitad del ancho (eje X del mundo) de la zona muerta.")]
        [Min(0f)]
        [SerializeField] private float deadZoneHalfWidth = 4f;

        [Tooltip("Mitad del largo (eje Z del mundo) de la zona muerta.")]
        [Min(0f)]
        [SerializeField] private float deadZoneHalfLength = 3f;

        [Header("Suavizado del seguimiento")]
        [Tooltip("Tiempo (segundos) que tarda la cámara en alcanzar al jugador. Más alto = más delay.")]
        [Min(0f)]
        [SerializeField] private float followSmoothTime = 0.35f;

        [Tooltip("Velocidad máxima (unidades/s) del seguimiento. Evita tirones con saltos grandes.")]
        [Min(0f)]
        [SerializeField] private float followMaxSpeed = 30f;

        // ─────────────────────────────────────────────────────────────────────────
        // Presets y transiciones
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Presets de cámara")]
        [Tooltip("Lista de configuraciones de cámara (pitch / yaw / distancia).")]
        [SerializeField] private List<CameraPreset> presets = new List<CameraPreset>();

        [Tooltip("Índice del preset inicial aplicado al arrancar (sin transición).")]
        [SerializeField] private int initialPresetIndex = 0;

        [Header("Auto-transitions al inicio del juego")]
        [Tooltip("Transiciones que se disparan automáticamente al Start().")]
        [SerializeField] private List<AutoTransition> autoTransitions = new List<AutoTransition>();

        // ─────────────────────────────────────────────────────────────────────────
        // Input de rotación con mouse
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Rotación con botón derecho del mouse")]
        [Tooltip("Sensibilidad del mouse para rotar la cámara. Rango sugerido: 0.08–0.25.")]
        [Min(0f)]
        [SerializeField] private float mouseSensitivity = 0.15f;

        [Tooltip("Suavizado de la rotación (SmoothDamp). Más alto = más inercia. Rango: 0.05–0.2.")]
        [Min(0f)]
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Tooltip("Offset máximo de pitch sobre la rotación base del preset.\n" +
                 "X = límite hacia abajo (negativo), Y = límite hacia arriba (positivo).")]
        [SerializeField] private Vector2 manualPitchRange = new Vector2(-20f, 20f);

        [Tooltip("Offset máximo de yaw sobre la rotación base del preset.\n" +
                 "X = límite izquierda (negativo), Y = límite derecha (positivo).")]
        [SerializeField] private Vector2 manualYawRange = new Vector2(-60f, 60f);

        [Tooltip("Duración (segundos) de la animación de reset al presionar End.")]
        [Min(0f)]
        [SerializeField] private float manualResetDuration = 0.4f;

        // ─────────────────────────────────────────────────────────────────────────
        // Debug
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Debug")]
        [Tooltip("Dibuja la zona muerta y el focus point en la Scene view.")]
        [SerializeField] private bool drawGizmos = true;

        // ─────────────────────────────────────────────────────────────────────────
        // Estado interno
        // ─────────────────────────────────────────────────────────────────────────

        private Vector3 focusPoint;
        private Vector3 focusVelocity;

        // Rotación "base" dictada por el preset activo o la transición en curso.
        private float basePitch;
        private float baseYaw;
        private float baseDistance;

        // Objetivo del offset manual (acumulado desde el mouse).
        private float targetPitchOffset;
        private float targetYawOffset;

        // Offset suavizado actual (lo que realmente se aplica al transform).
        private float smoothPitchOffset;
        private float smoothYawOffset;

        // Velocidades internas de SmoothDamp para el offset.
        private float pitchOffsetVelocity;
        private float yawOffsetVelocity;

        private Coroutine transitionRoutine;
        private Coroutine manualResetRoutine;

        // ─────────────────────────────────────────────────────────────────────────
        // API pública
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Yaw total actual de la cámara (preset base + offset del usuario, ya suavizado).
        ///
        /// NetCharacterController lo usa para transformar el input WASD al espacio de la cámara:
        ///
        ///   Vector3 camForward = Quaternion.Euler(0, cam.WorldYaw, 0) * Vector3.forward;
        ///   Vector3 camRight   = Quaternion.Euler(0, cam.WorldYaw, 0) * Vector3.right;
        ///   Vector3 worldDir   = camForward * input.Direction.y + camRight * input.Direction.x;
        ///
        /// Solo Y (yaw) importa para movimiento en el plano XZ; el pitch no afecta la dirección
        /// de movimiento del jugador.
        /// </summary>
        public float WorldYaw => baseYaw + smoothYawOffset;

        /// <summary>
        /// Setea el target al que sigue la cámara.
        /// En Fusion 2: llamar solo desde el cliente con HasInputAuthority.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                focusPoint    = target.position;
                focusVelocity = Vector3.zero;
            }
        }

        /// <summary>Transiciona suavemente al preset indicado (busca por nombre).</summary>
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

        /// <summary>Transiciona suavemente al preset indicado (por índice).</summary>
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
                StopCoroutine(transitionRoutine);

            float duration = overrideDuration ?? preset.transitionDuration;
            transitionRoutine = StartCoroutine(TransitionRoutine(preset, duration));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Ciclo de vida
        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (presets.Count > 0 && initialPresetIndex >= 0 && initialPresetIndex < presets.Count)
            {
                var initial = presets[initialPresetIndex];
                basePitch    = initial.pitch;
                baseYaw      = initial.yaw;
                baseDistance = initial.distance;
            }

            if (target != null)
                focusPoint = target.position;

            ApplyTransform(instant: true);

            if (autoTransitions.Count > 0)
                StartCoroutine(RunAutoTransitions());
        }

        private void LateUpdate()
        {
            UpdateFocusPoint();
            HandleMouseInput();
            SmoothOffsets();
            ApplyTransform(instant: false);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Seguimiento: zona muerta + smoothing
        // ─────────────────────────────────────────────────────────────────────────

        private void UpdateFocusPoint()
        {
            if (target == null) return;

            Vector3 targetPos = target.position;
            float dx = targetPos.x - focusPoint.x;
            float dz = targetPos.z - focusPoint.z;

            float desiredX = focusPoint.x;
            float desiredZ = focusPoint.z;

            if      (dx >  deadZoneHalfWidth)  desiredX = targetPos.x - deadZoneHalfWidth;
            else if (dx < -deadZoneHalfWidth)  desiredX = targetPos.x + deadZoneHalfWidth;

            if      (dz >  deadZoneHalfLength) desiredZ = targetPos.z - deadZoneHalfLength;
            else if (dz < -deadZoneHalfLength) desiredZ = targetPos.z + deadZoneHalfLength;

            Vector3 desiredFocus = new Vector3(desiredX, targetPos.y, desiredZ);

            focusPoint = Vector3.SmoothDamp(
                focusPoint, desiredFocus, ref focusVelocity, followSmoothTime, followMaxSpeed
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Input de mouse: botón derecho mantenido → acumular offset
        // ─────────────────────────────────────────────────────────────────────────

        private void HandleMouseInput()
        {
            // Reset suave del offset con End.
            if (Input.GetKeyDown(KeyCode.End))
            {
                if (manualResetRoutine != null) StopCoroutine(manualResetRoutine);
                manualResetRoutine = StartCoroutine(ResetManualOffsetsRoutine());
                return;
            }

            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                // Si el usuario mueve el mouse, cancelamos un reset en curso.
                bool moving = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f;
                if (moving && manualResetRoutine != null)
                {
                    StopCoroutine(manualResetRoutine);
                    manualResetRoutine = null;
                }

                // Mouse X → yaw | Mouse Y → pitch (subir mouse = cámara sube)
                targetYawOffset   += mouseX * mouseSensitivity * 100f * Time.deltaTime;
                targetPitchOffset -= mouseY * mouseSensitivity * 100f * Time.deltaTime;

                targetPitchOffset = Mathf.Clamp(targetPitchOffset, manualPitchRange.x, manualPitchRange.y);
                targetYawOffset   = Mathf.Clamp(targetYawOffset,   manualYawRange.x,   manualYawRange.y);
            }
        }

        /// <summary>
        /// Aplica SmoothDamp sobre el offset para suavizar el movimiento
        /// incluso cuando el mouse se detiene (da inercia natural).
        /// </summary>
        private void SmoothOffsets()
        {
            smoothPitchOffset = Mathf.SmoothDamp(
                smoothPitchOffset, targetPitchOffset, ref pitchOffsetVelocity, rotationSmoothTime
            );
            smoothYawOffset = Mathf.SmoothDamp(
                smoothYawOffset, targetYawOffset, ref yawOffsetVelocity, rotationSmoothTime
            );
        }

        private IEnumerator ResetManualOffsetsRoutine()
        {
            float startPitch = targetPitchOffset;
            float startYaw   = targetYawOffset;
            float elapsed    = 0f;

            while (elapsed < manualResetDuration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / manualResetDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cúbico

                targetPitchOffset = Mathf.Lerp(startPitch, 0f, eased);
                targetYawOffset   = Mathf.Lerp(startYaw,   0f, eased);
                yield return null;
            }

            targetPitchOffset  = 0f;
            targetYawOffset    = 0f;
            manualResetRoutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Transiciones de preset
        // ─────────────────────────────────────────────────────────────────────────

        private IEnumerator TransitionRoutine(CameraPreset target, float duration)
        {
            float startPitch    = basePitch;
            float startYaw      = baseYaw;
            float startDistance = baseDistance;
            float elapsed       = 0f;

            if (duration <= 0f)
            {
                basePitch    = target.pitch;
                baseYaw      = target.yaw;
                baseDistance = target.distance;
                transitionRoutine = null;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float eased = target.transitionCurve.Evaluate(t);

                basePitch    = Mathf.Lerp(startPitch,    target.pitch,    eased);
                baseYaw      = Mathf.LerpAngle(startYaw, target.yaw,      eased);
                baseDistance = Mathf.Lerp(startDistance, target.distance, eased);
                yield return null;
            }

            basePitch    = target.pitch;
            baseYaw      = target.yaw;
            baseDistance = target.distance;
            transitionRoutine = null;
        }

        private IEnumerator RunAutoTransitions()
        {
            var sorted = new List<AutoTransition>(autoTransitions);
            sorted.Sort((a, b) => a.triggerAtSeconds.CompareTo(b.triggerAtSeconds));

            float timeWaited = 0f;
            foreach (var auto in sorted)
            {
                float waitFor = auto.triggerAtSeconds - timeWaited;
                if (waitFor > 0f) yield return new WaitForSeconds(waitFor);
                timeWaited = auto.triggerAtSeconds;

                if (!string.IsNullOrEmpty(auto.presetName))
                    TransitionToPreset(auto.presetName);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Aplicación del transform final
        // ─────────────────────────────────────────────────────────────────────────

        private void ApplyTransform(bool instant)
        {
            float finalPitch = basePitch + smoothPitchOffset;
            float finalYaw   = baseYaw   + smoothYawOffset;

            Quaternion rot          = Quaternion.Euler(finalPitch, finalYaw, 0f);
            Vector3 offsetFromFocus = rot * Vector3.back * baseDistance;
            Vector3 desiredCamPos   = focusPoint + offsetFromFocus;

            transform.position = desiredCamPos;
            transform.rotation = rot;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Vector3 center = Application.isPlaying ? focusPoint : transform.position;

            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireCube(center, new Vector3(deadZoneHalfWidth * 2f, 0.05f, deadZoneHalfLength * 2f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(center, 0.2f);
        }
    }
}