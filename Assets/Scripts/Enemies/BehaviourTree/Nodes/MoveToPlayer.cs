using UnityEngine;

namespace BehaviourTree
{
    public class MoveToPlayer : Node
    {
        private EnemyAI _enemyAI;

        public MoveToPlayer(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            // Ordenar al NavMeshAgent que se mueva al jugador
            _enemyAI.agent.stoppingDistance = _enemyAI.attackRange * 0.9f;
            _enemyAI.agent.SetDestination(_enemyAI.player.position);

            // Distancia actual
            float distance = Vector3.Distance(
                _enemyAI.transform.position,
                _enemyAI.player.position
            );

            // Si llegamos al rango de ataque → éxito
            if (distance <= _enemyAI.attackRange)
            {
                state = NodeState.Success;
                return state;
            }

            // Mientras vaya avanzando → Running
            state = NodeState.Running;
            return state;
        }
    }
}


