using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo de ataque para el enemigo ranged.
    /// Verifica que el jugador esté dentro del ShootDistance y respeta el AttackCooldown.
    /// Devuelve Success mientras el jugador está en rango, permitiendo que el árbol
    /// no evalúe nodos de menor prioridad mientras el ranged puede disparar.
    /// </summary>
    public class RangedAttackNode : Node
    {
        private EnemyAI ai;
        private float lastAttackTime;

        public RangedAttackNode(EnemyAI enemy)
        {
            ai = enemy;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            if (distance > ai.ShootDistance)
                return state = NodeState.Failure;

            // Dispara si el cooldown pasó
            if (Time.time >= lastAttackTime + ai.AttackCooldown)
            {
                ai.RangedAttack();
                lastAttackTime = Time.time;
            }

            return state = NodeState.Success;
        }
    }
}