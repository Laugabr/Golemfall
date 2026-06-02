using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo de ataque para el enemigo melee.
    /// Verifica que el jugador esté dentro del AttackRange y que el cooldown haya pasado.
    /// Usa CanAttack() y RegisterAttack() de EnemyAI para el cooldown determinístico con Fusion.
    /// </summary>
    public class AttackPlayer : Node
    {
        private EnemyAI ai;

        public AttackPlayer(EnemyAI enemyAI)
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

            if (distance > ai.AttackRange)
                return state = NodeState.Failure;

            // Solo ataca si el cooldown pasó
            if (ai.CanAttack())
            {
                ai.RangedAttack();
                ai.RegisterAttack();
            }

            return state = NodeState.Success;
        }
    }
}