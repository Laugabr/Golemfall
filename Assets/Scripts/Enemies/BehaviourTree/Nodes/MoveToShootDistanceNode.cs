using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo que mueve al enemigo ranged hacia el jugador hasta estar dentro del ShootDistance.
    /// Devuelve Success cuando está en rango de disparo.
    /// Devuelve Running mientras se acerca.
    /// Solo recalcula el path si el jugador se movió más de 1 unidad para evitar recálculos constantes.
    /// </summary>
    public class MoveToShootDistance : Node
    {
        private EnemyAI ai;

        public MoveToShootDistance(EnemyAI enemy)
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
            {
                // Solo recalcula si el jugador se movió significativamente
                if (!ai.Agent.hasPath || Vector3.Distance(ai.Agent.destination, ai.CurrentTarget.position) > 1f)
                    ai.Agent.SetDestination(ai.CurrentTarget.position);

                return state = NodeState.Running;
            }

            // Está en rango, detiene el movimiento
            ai.Agent.ResetPath();
            return state = NodeState.Success;
        }
    }
}