using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo que mueve al enemigo melee hacia el jugador.
    /// Devuelve Success cuando está dentro del AttackRange.
    /// Devuelve Running mientras se está acercando.
    /// Actualiza el destino cada tick para seguir al jugador aunque se mueva.
    /// </summary>
    public class MoveToPlayer : Node
    {
        private EnemyAI ai;

        public MoveToPlayer(EnemyAI enemyAI)
        {
            ai = enemyAI;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            // Llegó al rango de ataque, detiene el agente
            if (distance <= ai.AttackRange)
            {
                ai.Agent.ResetPath();
                ai.Agent.velocity = Vector3.zero;
                return state = NodeState.Success;
            }

            // Actualiza el destino cada tick para seguir al jugador
            ai.Agent.SetDestination(ai.CurrentTarget.position);

            return state = NodeState.Running;
        }
    }
}