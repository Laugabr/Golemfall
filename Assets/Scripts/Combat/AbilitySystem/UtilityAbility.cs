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

    private SphereCollider _sphere;
    private bool _vfxPlayed;
    private HashSet<NetworkObject> _healed = new HashSet<NetworkObject>();

    private void Awake()
    {
        _sphere = GetComponent<SphereCollider>();
        _sphere.isTrigger = true;
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || _healed.Contains(netObj)) return;

        health.Heal(HealAmount);
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