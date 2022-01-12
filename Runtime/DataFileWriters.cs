using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace RealmAI {
	
		internal interface IDataFileWriter {
			void Initialize(Stream stream);
			void WriteEpisode(int episodeNum, float duration, float reward, List<Vector2> positions);
			void Flush();
			void Close();
		}
		
		internal  class BinaryDataFileWriter : IDataFileWriter {
			private BinaryWriter _writer = null;
			
			public void Initialize(Stream stream) {
				if (_writer != null) {
					_writer.Close();
					_writer = null;
				}

				_writer = new BinaryWriter(stream);
			}
			
			public void WriteEpisode(int episodeNum, float duration, float reward, List<Vector2> positions){
				if (_writer == null) {
					return;
				}
				
				_writer.Write(episodeNum);
				_writer.Write(duration);
				_writer.Write(reward);
				_writer.Write(positions.Count);
				
				for (int i = 0; i < positions.Count; i++) {
					_writer.Write(positions[i].x);
					_writer.Write(positions[i].y);
				}
			}
			
			public void Flush(){
				_writer?.Flush();
			}
			
			public void Close(){
				_writer?.Close();
			}
		}
		
		internal  class JsonDataFileWriter : IDataFileWriter {
			private JsonTextWriter _writer = null;
			
			public void Initialize(Stream stream) {
				if (_writer != null) {
					_writer.Close();
					_writer = null;
				}

				_writer = new JsonTextWriter(new StreamWriter(stream)) {AutoCompleteOnClose = true, CloseOutput = true};
				_writer.WriteStartObject();
				_writer.WritePropertyName("episodes");
				_writer.WriteStartArray();
				
			}
			
			public void WriteEpisode(int episodeNum, float duration, float reward, List<Vector2> positions){
				if (_writer == null) {
					return;
				}
				
				_writer.WriteStartObject();

				_writer.WritePropertyName("episode_number");
				_writer.WriteValue(episodeNum);
				_writer.WritePropertyName("duration");
				_writer.WriteValue(duration);
				_writer.WritePropertyName("reward");
				_writer.WriteValue(reward);

				_writer.WritePropertyName("pos_x");
				_writer.WriteStartArray();
				for (int i = 0; i < positions.Count; i++) {
					_writer.WriteValue(positions[i].x);
				}

				_writer.WriteEndArray();

				_writer.WritePropertyName("pos_y");
				_writer.WriteStartArray();
				for (int i = 0; i < positions.Count; i++) {
					_writer.WriteValue(positions[i].y);
				}

				_writer.WriteEndArray();

				_writer.WriteEndObject();
			}
			
			public void Flush(){
				_writer?.Flush();
			}
			
			public void Close(){
				_writer?.Close();
			}
		}
}