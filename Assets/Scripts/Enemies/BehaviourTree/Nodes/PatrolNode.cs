using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    public class PatrolNode : Node
    {
        private NavMeshAgent _agent;
        private Transform[] _points;
        private int _currentIndex = 0;

        public PatrolNode(
            NavMeshAgent agent,
            Transform[] patrolPoints
        )
        {
            _agent = agent;
            _points = patrolPoints;
        }

        public override NodeState Evaluate()
        {
            if (_points == null ||
                _points.Length == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            _agent.stoppingDistance = 0.2f;

            _agent.SetDestination(
                _points[_currentIndex].position
            );

            if (
                !_agent.pathPending &&
                _agent.remainingDistance <= 0.2f
            )
            {
                _currentIndex =
                    (_currentIndex + 1) %
                    _points.Length;
            }

            state = NodeState.Running;
            return state;
        }
    }
}


