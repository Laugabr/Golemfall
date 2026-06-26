using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo de patrullaje aleatorio dentro de un radio fijo desde la HomePosition del enemigo.
    /// El enemigo camina a puntos aleatorios, espera unos segundos y repite.
    /// Si se aleja demasiado de casa, vuelve antes de seguir patrullando.
    /// Si se queda trabado en un punto, cambia de destino autom�ticamente.
    /// </summary>
    public class PatrolNode : Node
    {
        private NavMeshAgent _agent;
        private EnemyAI _ai;

        private float _waitTimer = 0f;       // tiempo restante de espera en el punto actual
        private bool _isWaiting = false;     // true cuando el enemigo est� esperando en un punto
        private bool _hasDestination = false; // true cuando el agente tiene un destino activo
        private float _stuckTimer = 0f;      // acumula tiempo para detectar si el enemigo est� trabado
        private const float StuckTimeout = 3f; // segundos antes de considerar que est� trabado

        public PatrolNode(NavMeshAgent agent, EnemyAI ai)
        {
            _agent = agent;
            _ai = ai;
        }

        public override NodeState Evaluate()
        {
            if (_ai.HasTarget)
                return state = NodeState.Failure;

            // Only check "too far from home" while actually moving — not while waiting in place
            if (!_isWaiting)
            {
                float distFromHome = Vector3.Distance(_agent.transform.position, _ai.HomePosition);
                if (distFromHome > _ai.PatrolRadius * 1.5f)
                {
                    _agent.SetDestination(_ai.HomePosition);
                    _hasDestination = false;
                    _stuckTimer = 0f;
                    return state = NodeState.Running;
                }
            }

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                _agent.ResetPath();

                if (_waitTimer <= 0f)
                    _isWaiting = false;

                return state = NodeState.Running;
            }

            if (_hasDestination)
            {
                _stuckTimer += Time.deltaTime;

                if ((!_agent.pathPending && _agent.remainingDistance <= 0.5f) || _stuckTimer >= StuckTimeout)
                {
                    _isWaiting = true;
                    _waitTimer = _ai.PatrolWaitTime;
                    _hasDestination = false;
                    _stuckTimer = 0f;
                }

                return state = NodeState.Running;
            }

            Vector3 randomPoint = GetRandomPointInRadius();
            _agent.SetDestination(randomPoint);
            _hasDestination = true;
            _stuckTimer = 0f;

            return state = NodeState.Running;
        }

        /// <summary>
        /// Resetea el estado interno del nodo.
        /// Se llama desde EnemyAI cuando el enemigo detecta un target,
        /// para que al volver al patrullaje empiece desde cero.
        /// </summary>
        public void Reset()
        {
            _hasDestination = false;
            _isWaiting = false;
            _waitTimer = 0f;
            _stuckTimer = 0f;
        }

        /// <summary>
        /// Genera un punto aleatorio dentro del patrolRadius desde HomePosition
        /// que sea v�lido en el NavMesh. Intenta hasta 5 veces antes de
        /// devolver HomePosition como fallback.
        /// </summary>
        private Vector3 GetRandomPointInRadius()
        {
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