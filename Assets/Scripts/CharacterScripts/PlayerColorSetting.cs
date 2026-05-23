using UnityEngine;
using Fusion;


    public class PlayerColorSetting : NetworkBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer playerRenderer;
        [SerializeField] private Material[] playerMaterials;

        [Networked] public int PlayerIndex { get; set; }

        private int _lastAppliedIndex = -1; // -1 para forzar aplicación al inicio

        public override void Spawned()
        {
            if (playerRenderer == null)
                playerRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        public override void Render()
        {
            // Solo aplica si el índice cambió, evita asignar material cada frame
            if (PlayerIndex != _lastAppliedIndex)
            {
                ApplyColor();
                _lastAppliedIndex = PlayerIndex;
            }
        }

        private void ApplyColor()
        {
            if (playerRenderer == null) return;
            if (playerMaterials == null || playerMaterials.Length == 0) return;

            int index = Mathf.Clamp(PlayerIndex, 0, playerMaterials.Length - 1);
            playerRenderer.material = playerMaterials[index];
        }
    }

