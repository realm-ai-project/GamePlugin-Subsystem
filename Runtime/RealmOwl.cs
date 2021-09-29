using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class RealmOwl : MonoBehaviour
{
	[SerializeField] private string _filePrefix = "data";
	[SerializeField] private string _fileExtension = "dat";
	[SerializeField] private float _flushPeriod = 60;
	private JsonTextWriter _writer = null;
	
	private bool _isAlive = true;
	private bool _firstEpisode = true;
	private int _episodeNum = -1;
	private float _duration = 0;
	private float _reward = 0;
	private List<Vector2> _positions = new List<Vector2>();
	
	private void Start() {
		StartCoroutine(Flusher());
	}
	
	private void InitializeFile() {
		if (_writer != null) {
			_writer.Close();
			_writer = null;
		}

		// TODO: make multiple processsing saving to a file more robust
		// TODO: consider policy for overwriting
		// TODO: consider if we want separate threads for writing
		var count = 0;
		while (count < 256) {
			try {
				var path = $"{_filePrefix}-{count}.{_fileExtension}";
				var streamWriter = File.CreateText(path);
				_writer = new JsonTextWriter(streamWriter);
				_writer.AutoCompleteOnClose = true;
				_writer.CloseOutput = true;
				Debug.Log($"Data saving to {path}");
				
				
				_writer.WriteStartObject();
				_writer.WritePropertyName("episodes");
				_writer.WriteStartArray();
				break;
			} catch (IOException e) {
				// exception for file sharing violation
				if (e.HResult != -2147024864) {
					Debug.LogException(e);
					break;
				}
			}
			count++;
		}
	}

	public void StartNewEpisode(int episodeNum) {
		if (_firstEpisode) {
			_firstEpisode = false;
		} else {
			WriteEpisode();
		}
		
		_episodeNum = episodeNum;
		_duration = 0;
		_reward = 0;
		_positions.Clear();
	}

	public void RecordDuration(float duration) {
		_duration = duration;
	}
	
	public void RecordReward(float reward) {
		_reward = reward;
	}
	
	public void RecordPosition(Vector2 position) {
		_positions.Add(position);
	}

	public void WriteEpisode() {
		if (!_isAlive) {
			return;
		}

		if (_writer == null) {
			InitializeFile();
			if (_writer == null) {
				return;
			}
		}

		_writer.WriteStartObject();
		
		_writer.WritePropertyName("episode_number");
		_writer.WriteValue(_episodeNum);
		_writer.WritePropertyName("duration");
		_writer.WriteValue(_duration);
		_writer.WritePropertyName("reward");
		_writer.WriteValue(_reward);
		
		_writer.WritePropertyName("pos_x");
		_writer.WriteStartArray();
		for (int i = 0; i < _positions.Count; i++) {
			_writer.WriteValue(_positions[i].x);
		}
		_writer.WriteEndArray();
		
		_writer.WritePropertyName("pos_y");
		_writer.WriteStartArray();
		for (int i = 0; i < _positions.Count; i++) {
			_writer.WriteValue(_positions[i].y);
		}
		_writer.WriteEndArray();
		
		_writer.WriteEndObject();
	}


	private void OnDestroy() {
		WriteEpisode();
		
		_isAlive = false;
		if (_writer != null) {
			_writer.Close();
			_writer = null;
		}
	}

	private void OnApplicationQuit() {
		WriteEpisode();
		
		_isAlive = false;
		if (_writer != null) {
			_writer.Close();
			_writer = null;
		}
	}

	private IEnumerator Flusher() {
		while (true) {
			yield return new WaitForSeconds(_flushPeriod);
			if (_writer != null) {
				_writer.Flush();
			}
		}
	}
}
