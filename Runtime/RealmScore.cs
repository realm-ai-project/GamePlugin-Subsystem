using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmAI {
    public class RealmScore : MonoBehaviour {
        
        [Serializable]
        public class RewardRegion {
            public bool Enabled = true;
            public Rect[] Rects = null;
            public float EnterReward = 0;
            public float StayRewardPerSecond = 0;
            public Color DebugColor = Color.white;
        }

        [SerializeField] private SerializedFloatFunc _rewardFunction = default;
        [SerializeField] private RewardRegion[] _rewardRegions = default;
        [SerializeField] private float _existentialPenaltyPerSecond = 0;
        [SerializeField] private bool _showGizmos = false;

        public RewardRegion[] RewardRegions {
            get => _rewardRegions;
            set => _rewardRegions = value;
        }
        
        public bool ShowGizmos => _showGizmos;

        private float _additionalScore = 0;
        private float _baseScore = 0;

        private HashSet<RewardRegion> _enteredRewardRegions = new HashSet<RewardRegion>();
        
        public void StartNewEpisode() {
            _baseScore = 0;
            _additionalScore = 0;
            _enteredRewardRegions.Clear();
        }

        public float GetScore() {
            _baseScore = _rewardFunction?.Invoke() ?? _baseScore;
            return _baseScore + _additionalScore;
        }

        private void FixedUpdate() {
            _additionalScore -= _existentialPenaltyPerSecond * Time.fixedDeltaTime;
            
            for (int i = 0; i < _rewardRegions.Length; i++) {
                var region = _rewardRegions[i];
                if (!region.Enabled)
                    continue;

                var hasEnteredRegion = _enteredRewardRegions.Contains(region);
                if (hasEnteredRegion && Mathf.Abs(region.StayRewardPerSecond) < 1e-6f)
                    continue;
                
                for (int j = 0; j < region.Rects.Length; j++) {
                    var rect = region.Rects[j];
                    if (rect.Contains(transform.position, true)) {
                        if (!hasEnteredRegion) {
                            _additionalScore += region.EnterReward;   
                            _enteredRewardRegions.Add(region);
                        } else {
                            _additionalScore += region.StayRewardPerSecond * Time.fixedDeltaTime;
                        }
                        
                        break;
                    }
                }
            }
        }
    }
}
