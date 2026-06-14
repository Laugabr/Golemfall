    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using Fusion;

    // Lives on the player prefab alongside PlayerHealth and PlayerStats.
    // Only the server modifies TotalExperience and CurrentLevel.
    // UI updates locally via OnChangedRender, visible only to the InputAuthority.

    public class ExperienceManager : NetworkBehaviour
    {
        [Header("Experience Curve")]
        [SerializeField] private AnimationCurve experienceCurve;
        [SerializeField] private int maxLevel = 10;

        [Header("XP per GameEvent (individual)")]
        [SerializeField] private int xpPerKillEnemy      = 20;
        [SerializeField] private int xpPerBreakBreakable = 3;
        [SerializeField] private int xpPerCollectItem    = 5;
        [SerializeField] private int xpPerCollectSpecial = 10;

        // Networked state — only the server writes these
        [Networked, OnChangedRender(nameof(OnExperienceChanged))]
        public int TotalExperience { get; set; }

        [Networked, OnChangedRender(nameof(OnLevelChanged))]
        public int CurrentLevel { get; set; }

        //  Lifecycle 

        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;

            CurrentLevel = 1;
            TotalExperience = 0;

            // Only the server listens to gameplay events for THIS player
            TrackEvents.OnTrackEvent += ServerHandleEvent;
        }

        private void OnDestroy()
        {
            TrackEvents.OnTrackEvent -= ServerHandleEvent;
        }

        // Server: individual XP from gameplay events 

        // Called on the server when a gameplay event fires on this machine.
        // Because MissionEventBridge already routes client events to the server,
        // we only need to handle it here on the host/server side.
        private void ServerHandleEvent(GameEventType eventType, int amount)
        {
            if (!Object.HasStateAuthority) return;

            // Only grant XP to the LOCAL player on the server (the host's own character).
            // Clients send their events via MissionEventBridge → RPC_ServerAddExperience.
            if (!Object.HasInputAuthority) return;

            int xp = GetXpForEvent(eventType) * amount;
            if (xp > 0) AddExperience(xp);
        }

        private int GetXpForEvent(GameEventType eventType)
        {
            return eventType switch
            {
                GameEventType.KillEnemy       => xpPerKillEnemy,
                GameEventType.BreakBreakable  => xpPerBreakBreakable,
                GameEventType.CollectItem     => xpPerCollectItem,
                GameEventType.CollectSpecialItem => xpPerCollectSpecial,
                _                             => 0
            };
        }

        // Server: add XP (called from MissionController for group XP too)

        public void AddExperience(int amount)
        {
            if (!Object.HasStateAuthority) return;
            if (CurrentLevel >= maxLevel) return;

            TotalExperience += amount;
            CheckLevelUp();
        }

        // RPC so clients can request XP gain for individual events
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_ServerAddExperience(int amount, RpcInfo info = default)
        {
            AddExperience(amount);
        }

        // Server: level-up logic 

        private void CheckLevelUp()
        {
            if (!Object.HasStateAuthority) return;
            if (CurrentLevel >= maxLevel) return;


            int xpForNext = GetXpForLevel(CurrentLevel + 1);

            if (TotalExperience >= xpForNext)
            {
                CurrentLevel++;
                Debug.Log($"[ExperienceManager] Disparando OnLevelUp con level {CurrentLevel}");

                Debug.Log($"[SERVER] {gameObject.name} subió al nivel {CurrentLevel}");

                // Notify PlayerStats so it can apply level bonuses
                BasicEventsManager.OnLevelUp?.Invoke(CurrentLevel);
                GetComponent<PlayerProgressionVisuals>()?.OnPlayerLevelUp(CurrentLevel);

                // Recurse in case multiple levels were gained at once
                CheckLevelUp();
            }
        }

        // Returns cumulative XP needed to reach a given level
        private int GetXpForLevel(int level)
        {
            return (int)experienceCurve.Evaluate(level);
        }

        // Render callbacks (run on all clients when networked values change)

        private void OnExperienceChanged()
        {
            // Only update UI for the local player
            if (!Object.HasInputAuthority) return;
            ExperienceUI.Instance?.UpdateXP(TotalExperience, CurrentLevel, GetXpForLevel(CurrentLevel), GetXpForLevel(CurrentLevel + 1));
        }

        private void OnLevelChanged()
        {
            if (!Object.HasInputAuthority) return;
            ExperienceUI.Instance?.UpdateLevel(CurrentLevel);
        }

        // Public helpers 

        // Called by MissionController when a group mission completes
        public static void GrantMissionXpToAll(int xpAmount)
        {
            // MissionController iterates all ExperienceManagers and calls AddExperience directly
            // This helper is here for documentation purposes
        }
    }