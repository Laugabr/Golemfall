using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    /// <summary>
    /// Nodo de patrullaje aleatorio dentro de un radio fijo desde la HomePosition del enemigo.
    /// El enemigo camina a puntos aleatorios, espera unos segundos y repite.
    /// Si se aleja demasiado de casa, vuelve antes de seguir patrullando.
    /// Si se queda trabado en un punto, cambia de destino automáticamente.
    /// </summary>
    public class PatrolNode : Node
    {
        private NavMeshAgent _agent;
        private EnemyAI _ai;

        private float _waitTimer = 0f;       // tiempo restante de espera en el punto actual
        private bool _isWaiting = false;     // true cuando el enemigo está esperando en un punto
        private bool _hasDestination = false; // true cuando el agente tiene un destino activo
        private float _stuckTimer = 0f;      // acumula tiempo para detectar si el enemigo está trabado
        private const float StuckTimeout = 3f; // segundos antes de considerar que está trabado

        public PatrolNode(NavMeshAgent agent, EnemyAI ai)
        {
            _agent = agent;
            _ai = ai;
        }

        public override NodeState Evaluate()
        {
            // Si el enemigo detectó al jugador, el patrullaje cede paso a la persecución
            if (_ai.HasTarget)
                return state = NodeState.Failure;

            float distFromHome = Vector3.Distance(
                _agent.transform.position,
                _ai.HomePosition
            );

            // Si el enemigo se alejó demasiado de su zona (por ejemplo tras un empujón),
            // vuelve a HomePosition antes de seguir patrullando
            if (distFromHome > _ai.PatrolRadius * 1.5f)
            {
                _agent.SetDestination(_ai.HomePosition);
                _hasDestination = false;
                _stuckTimer = 0f;
                return state = NodeState.Running;
            }

            // Fase de espera: el enemigo llegó a un punto y espera PatrolWaitTime segundos
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                _agent.ResetPath(); // detiene el agente mientras espera

                if (_waitTimer <= 0f)
                    _isWaiting = false;

                return state = NodeState.Running;
            }

            // Fase de movimiento: el enemigo se mueve hacia el destino actual
            if (_hasDestination)
            {
                _stuckTimer += Time.deltaTime;

                // Llegó al destino O lleva demasiado tiempo sin llegar (trabado)
                if ((!_agent.pathPending && _agent.remainingDistance <= 0.5f) || _stuckTimer >= StuckTimeout)
                {
                    _isWaiting = true;
                    _waitTimer = _ai.PatrolWaitTime;
                    _hasDestination = false;
                    _stuckTimer = 0f;
                }

                return state = NodeState.Running;
            }

            // Sin destino: elige un punto aleatorio dentro del patrolRadius y se dirige allí
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
        /// que sea válido en el NavMesh. Intenta hasta 5 veces antes de
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