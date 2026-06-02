using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    public class PatrolNode : Node
    {
        private NavMeshAgent _agent;
        private EnemyAI _ai;

        private float _waitTimer = 0f;
        private bool _isWaiting = false;
        private bool _hasDestination = false;

        public PatrolNode(NavMeshAgent agent, EnemyAI ai)
        {
            _agent = agent;
            _ai = ai;
        }

        public override NodeState Evaluate()
        {
            // Si el enemigo tiene target activo no patrullamos
            if (_ai.HasTarget)
                return state = NodeState.Failure;

            float distFromHome = Vector3.Distance(
                _agent.transform.position,
                _ai.HomePosition
            );

            // Si está muy lejos de casa, vuelve primero
            if (distFromHome > _ai.PatrolRadius * 1.5f)
            {
                _agent.SetDestination(_ai.HomePosition);
                _hasDestination = false;
                return state = NodeState.Running;
            }

            // Esperando en el punto
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                _agent.ResetPath();
                _hasDestination = false;

                if (_waitTimer <= 0f)
                    _isWaiting = false;

                return state = NodeState.Running;
            }

            // Si llegó al destino, espera
            if (_hasDestination && !_agent.pathPending && _agent.remainingDistance <= 0.5f)
            {
                _isWaiting = true;
                _waitTimer = _ai.PatrolWaitTime;
                _hasDestination = false;
                return state = NodeState.Running;
            }

            // Elige un nuevo punto aleatorio
            if (!_hasDestination)
            {
                Vector3 randomPoint = GetRandomPointInRadius();
                _agent.SetDestination(randomPoint);
                _hasDestination = true;
            }

            return state = NodeState.Running;
        }

        // Resetea el estado interno cuando el árbol cambia de rama
        // Se llama desde EnemyAI cuando detecta un target
        public void Reset()
        {
            _hasDestination = false;
            _isWaiting = false;
            _waitTimer = 0f;
        }

        private Vector3 GetRandomPointInRadius()
        {
            // Intenta hasta 5 veces encontrar un punto válido en el NavMesh
            for (int i = 0; i < 5; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * _ai.PatrolRadius;
                Vector3 candidate = _ai.HomePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    return hit.position;
            }

            return _ai.HomePosition;
        }
    }
}