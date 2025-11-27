using UnityEngine;

namespace BehaviourTree
{
    public class AttackPlayer : Node
    {
        private EnemyAI _enemyAI;
        private float _nextAttackTime;

        public AttackPlayer(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            // Distancia al jugador
            float dist = Vector3.Distance(_enemyAI.transform.position, _enemyAI.player.position);

            // Si estoy fuera de rango → falla y la secuencia vuelve a MoveToPlayer
            if (dist > _enemyAI.attackRange)
            {
                state = NodeState.Failure;
                return state;
            }

            // Todavía no toca atacar → Running
            if (Time.time < _nextAttackTime)
            {
                state = NodeState.Running;
                return state;
            }

            // Ataca
            _enemyAI.DealDamage();
            _nextAttackTime = Time.time + _enemyAI.attackCooldown;

            state = NodeState.Success;
            return state;
        }
    }
}
