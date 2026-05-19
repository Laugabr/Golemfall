using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }

    private SpawnPoint[] _spawnPoints;

    private void Awake()
    {
        Instance = this;
        _spawnPoints = GetComponentsInChildren<SpawnPoint>();

        if (_spawnPoints.Length == 0)
            Debug.LogError("SpawnPointManager: no SpawnPoints found as children.");
    }

    public Vector3 GetRandomSpawnPoint()
    {
        int index = Random.Range(0, _spawnPoints.Length);
        return _spawnPoints[index].transform.position;
    }
}