// FakeProjectileRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public static class FakeProjectileRegistry
{
    private static readonly Dictionary<uint, Queue<FakeProjectile>> _registry = new();

    public static void Register(uint ownerId, FakeProjectile fake)
    {
        if (!_registry.TryGetValue(ownerId, out var queue))
        {
            queue = new Queue<FakeProjectile>();
            _registry[ownerId] = queue;
        }
        queue.Enqueue(fake);
    }

    public static FakeProjectile Dequeue(uint ownerId)
    {
        if (!_registry.TryGetValue(ownerId, out var queue)) return null;
        while (queue.Count > 0)
        {
            var fake = queue.Dequeue();
            if (fake != null) return fake;
        }
        return null;
    }

    public static void Clear(uint ownerId)
    {
        if (_registry.TryGetValue(ownerId, out var queue))
        {
            foreach (var f in queue)
                if (f != null) Object.Destroy(f.gameObject);
            queue.Clear();
        }
    }
}