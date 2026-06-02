using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo que verifica si el enemigo tiene un target activo.
    /// La detección real la maneja UpdateTarget() en EnemyAI cada tick.
    /// Este nodo simplemente consulta si ya hay un target asignado.
    /// </summary>
    public class CanSeePlayer : Node
    {
        private EnemyAI ai;

        public CanSeePlayer(EnemyAI enemyAI)
        {
            ai = enemyAI;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            return state = NodeState.Success;
        }
    }
}