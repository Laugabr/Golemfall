using UnityEngine;
using System.Collections;

namespace Game.CameraSystem
{
    public class CameraShake : MonoBehaviour
    {
        private Vector3 shakeOffset;
        private Coroutine shakeRoutine;

        public Vector3 Offset => shakeOffset;

        public void Shake(float duration, float intensity)
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                shakeOffset = Random.insideUnitSphere * intensity;
                yield return null;
            }

            // ANTES (bug): esto mueve la cámara DESPUÉS de que termina el shake
            // shakeOffset = (transform.right * Random.Range(-1f,1f) +
            //    transform.up * Random.Range(-1f,1f)) * intensity;
            
            // CORRECTO: resetea el offset y listo
            shakeOffset = Vector3.zero;
            shakeRoutine = null;
        }
    }
}