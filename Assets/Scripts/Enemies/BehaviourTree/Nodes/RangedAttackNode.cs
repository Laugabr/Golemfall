using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo de ataque para el enemigo ranged.
    /// Verifica que el jugador est� dentro del ShootDistance y que la habilidad est� lista.
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
            
            // Bloquea el �rbol mientras se reproduce la animaci�n de ataque
            if (ai.IsInAttackAnimation)
                return state = NodeState.Running;

            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;



            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            if (distance > ai.ShootDistance)
                return state = NodeState.Failure;

            // Dispara solo si el AbilityHolder dice que est� listo
            if (ai.CanAttack())
                ai.RangedAttack();

            return state = NodeState.Success;
        }
    }
}