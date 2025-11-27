using UnityEngine;

namespace BehaviourTree
{
    public class CanSeePlayer : Node
    {
        private EnemyAI _enemyAI;

        public CanSeePlayer(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            // Distancia al jugador
            float distance = Vector3.Distance(
                _enemyAI.transform.position,
                _enemyAI.player.position
            );

            // Si está dentro del rango de visión → Success
            if (distance <= _enemyAI.visionRange)
            {
                state = NodeState.Success;
                return state;
            }

            // Si no → Failure
            state = NodeState.Failure;
            return state;
        }
    }
}



