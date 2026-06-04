using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo de ataque para el enemigo ranged.
    /// Verifica que el jugador esté dentro del ShootDistance y que la habilidad esté lista.
    /// El cooldown lo maneja el AbilityHolder via el ScriptableObject de la habilidad.
    /// </summary>
    public class RangedAttackNode : Node
    {
        private EnemyAI ai;

        public RangedAttackNode(EnemyAI enemy)
        {
            ai = enemy;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            // Bloquea el árbol mientras se reproduce la animación de ataque
            if (ai.IsInAttackAnimation)
                return state = NodeState.Running;

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            if (distance > ai.ShootDistance)
                return state = NodeState.Failure;

            // Dispara solo si el AbilityHolder dice que está listo
            if (ai.CanAttack())
                ai.RangedAttack();

            return state = NodeState.Success;
        }
    }
}