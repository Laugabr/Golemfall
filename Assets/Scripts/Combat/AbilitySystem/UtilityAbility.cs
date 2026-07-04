using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class UtilityAbility : NetworkBehaviour
{
    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private int HealAmount { get; set; }
    [Networked] private float ActiveTime { get; set; }
    [Networked] private NetworkBool Expired { get; set; }
    [Networked] private Vector3 ExpirePosition { get; set; }

    [Header("VFX")]
    [SerializeField] private GameObject expireVFX;

    // ← NUEVO: layer de los players, para que el chequeo manual de overlap
    // (Physics.OverlapSphere) no capture enemigos, nebuflores u otros props.
    // Asignar la layer "Player" en el Inspector del prefab del área de heal.
    [Header("Heal Target Filter")]
    [SerializeField] private LayerMask playerLayerMask;

    private SphereCollider _sphere;
    private bool _vfxPlayed;
    private HashSet<NetworkObject> _healed = new HashSet<NetworkObject>();

    // ← NUEVO: flag para asegurarnos de que el chequeo manual de overlap
    // corra una sola vez, en el primer tick después del spawn.
    private bool _initialOverlapChecked;

    private void Awake()
    {
        _sphere = GetComponent<SphereCollider>();
        _sphere.isTrigger = true;
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // ← NUEVO: chequeo manual de overlap en el primer tick.
        // Motivo: OnTriggerEnter (Unity physics) SOLO dispara cuando dos colliders
        // empiezan a superponerse. Si el área de heal se spawnea centrada en el
        // caster (ver UtilityRuntime.Execute), el collider del propio caster ya
        // está adentro desde el frame 0 → nunca "entra" → OnTriggerEnter nunca
        // se dispara para él. Esto era la causa de que el heal fallara la
        // mayoría de las veces: solo curaba cuando alguien más entraba al área
        // DESPUÉS del spawn, nunca al propio caster parado en el centro.
        if (!_initialOverlapChecked)
        {
            _initialOverlapChecked = true;

            Collider[] hits = Physics.OverlapSphere(transform.position, _sphere.radius, playerLayerMask);
            foreach (var hit in hits)
            {
                TryHeal(hit);
            }
        }

        ActiveTime -= Runner.DeltaTime;

        if (ActiveTime <= 0f && !Expired)
        {
            ExpirePosition = transform.position;
            Expired = true;
        }

        if (Expired && ActiveTime <= -Runner.DeltaTime)
            Runner.Despawn(Object);
    }

    public void Initialize(NetworkObject caster, int healAmount, float radius, float activeTime)
    {
        Owner = caster;
        HealAmount = healAmount;
        ActiveTime = activeTime;
        _sphere.radius = radius;
        _healed.Clear(); // ← limpia al inicializar por si el objeto fue pooled
        _initialOverlapChecked = false; // ← NUEVO: reset por si el objeto es pooled/reusado
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        // ← NUEVO: la lógica de curación se movió a TryHeal() para poder
        // compartirla con el chequeo manual de overlap de FixedUpdateNetwork.
        TryHeal(other);
    }

    // ← NUEVO: método compartido entre OnTriggerEnter (jugadores que entran
    // después del spawn) y el chequeo manual de overlap (jugadores que ya
    // estaban parados en el área, incluido el propio caster).
    private void TryHeal(Collider other)
    {
        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || _healed.Contains(netObj)) return;

        health.Heal(HealAmount);
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.Heal, HealAmount, "");
        _healed.Add(netObj);
    }

    public override void Render()
    {
        if (Expired && !_vfxPlayed)
        {
            _vfxPlayed = true;
            if (expireVFX != null)
            {
                var vfx = Instantiate(expireVFX, ExpirePosition, Quaternion.identity);
                Destroy(vfx, 5f);
            }
        }
    }
}