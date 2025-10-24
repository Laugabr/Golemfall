using UnityEngine;
using UnityEngine.AI;

/*[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [Header("Comportamiento")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private Transform player;
    private float lastAttackTime = -999f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Buscar jugador por tag

    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("found plaeyr");
            }
        }

        if (player == null)
        {
            agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            // Atacar
            agent.isStopped = true;
            TryAttack();
        }
        else if (distance <= detectionRange)
        {
            // Perseguir
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // Fuera de rango
            agent.isStopped = true;
        }
    }
   

    private void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;


                Vector3 direction = (player.position - transform.position);
                direction.y = 0f;
                direction.Normalize();

                // Girar al personaje hacia el punto
                transform.forward = direction;

                // Lanzar proyectil (si tenés uno)
                GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));

                // 🚀 importante: asignar el dueño
                proj.GetComponentInChildren<Proyectil>().SetOwner(gameObject, attackDamage);

                Rigidbody rb = proj.GetComponent<Rigidbody>();
                Debug.Log($"{gameObject.name} atacó al jugador");
            }
        }
    }*/



