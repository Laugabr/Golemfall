using UnityEngine;

namespace BehaviourTree
{
    public class AttackPlayerNode : Node
    {
        private Transform enemyTransform;
        private Transform playerTransform;
        private float attackRange;

        public AttackPlayerNode(Transform enemy, Transform player, float attackRange)
        {
            this.enemyTransform = enemy;
            this.playerTransform = player;
            this.attackRange = attackRange;
        }

        public override NodeState Evaluate()
        {
            float distance = Vector3.Distance(enemyTransform.position, playerTransform.position);

            if (distance <= attackRange)
            {
                Debug.Log("Atacando al jugador!");
                state = NodeState.Success;
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }
    }
}
