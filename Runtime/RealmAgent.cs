using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.Events;

namespace RealmAI {
    public class RealmAgent : Agent {
        [SerializeField] private RealmOwl _realmOwl = default;
        [SerializeField] private UnityEvent _episodeReset = default;
        [SerializeField] private FloatDelegate _rewardFunction = default;
        [SerializeField] private BoolDelegate _gameoverFunction = default;
        

        private float _lastReward = 0;
        private float _episodeDuration = 0;

        public override void OnEpisodeBegin() {
            _lastReward = 0;
            _episodeDuration = 0;
            _realmOwl.StartNewEpisode(CompletedEpisodes);
            _episodeReset.Invoke();
        }

        private void Update() {
            UpdateReward();
            _episodeDuration += Time.deltaTime;
            _realmOwl.RecordDuration(_episodeDuration);
            
            if (_gameoverFunction != null) {
                if (_gameoverFunction.Invoke()) {
                    EndEpisode();
                }
            }
        }

        private void UpdateReward() {
            var currentReward = _rewardFunction?.Invoke() ?? 0;
            var diff = currentReward - _lastReward;
            if (Mathf.Abs(diff) > 1e-7) {
                AddReward(diff);
                _lastReward = currentReward;
                _realmOwl.RecordReward(currentReward);
            }
        }
    }
}