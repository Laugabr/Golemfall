using UnityEngine;

namespace BehaviourTree
{
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

            // Si ya tiene target lo sigue hasta que UpdateTarget lo pierda
            // por distancia (loseTargetRange) o porque el jugador murió
            return state = NodeState.Success;
        }
    }
}


