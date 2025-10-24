using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    public class PatrolNode : Node
    {
        private NavMeshAgent agent;
        private Vector3[] waypoints;
        private int currentWaypoint = 0;

        public PatrolNode(NavMeshAgent agent)
        {
            this.agent = agent;
            // Ejemplo simple: cuatro puntos alrededor del enemigo
            waypoints = new Vector3[]
            {
                agent.transform.position + Vector3.forward * 5,
                agent.transform.position + Vector3.right * 5,
                agent.transform.position + Vector3.back * 5,
                agent.transform.position + Vector3.left * 5,
            };
        }

        public override NodeState Evaluate()
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypoint]);
            }

            state = NodeState.Running;
            return state;
        }
    }
}
