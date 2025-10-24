using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    public class MoveToPlayerNode : Node
    {
        private NavMeshAgent agent;
        private Transform playerTransform;

        public MoveToPlayerNode(NavMeshAgent agent, Transform player)
        {
            this.agent = agent;
            this.playerTransform = player;
        }

        public override NodeState Evaluate()
        {
            agent.SetDestination(playerTransform.position);

            state = (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    ? NodeState.Running
                    : NodeState.Success;

            return state;
        }
    }
}
