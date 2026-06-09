using UnityEngine;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo exclusivo del enemigo ranged que mantiene distancia mínima con el jugador.
    /// Si el jugador se acerca demasiado (dentro de MinDistance), el enemigo retrocede.
    /// Tiene prioridad sobre la secuencia de ataque en el Selector del ranged,
    /// así el enemigo nunca deja que el jugador se le pegue demasiado.
    /// Devuelve Failure cuando la distancia es aceptable, cediendo al siguiente nodo.
    /// </summary>
    public class KeepDistanceNode : Node
    {
        private EnemyAI ai;

        public KeepDistanceNode(EnemyAI enemyAI)
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

            // El jugador está demasiado cerca, retrocedemos
            if (distance < ai.MinDistance)
            {
                // Calculamos destino ALEJADO del jugador a MinDistance * 2
                // desde la posición del jugador hacia afuera
                Vector3 dir = (ai.transform.position - ai.CurrentTarget.position).normalized;
                Vector3 targetPos = ai.CurrentTarget.position + dir * (ai.MinDistance * 2f);

                // Verificamos que el destino sea válido en el NavMesh
                if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    ai.Agent.SetDestination(hit.position);
                }

                return state = NodeState.Running;
            }

            return state = NodeState.Failure;
        }
    }
}