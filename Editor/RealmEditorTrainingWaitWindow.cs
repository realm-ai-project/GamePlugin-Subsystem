using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RealmAI {
	public class RealmEditorTrainingWaitWindow : EditorWindow {
		private const int EditorTrainingPort = 5004;
		
		private bool _portOpen = false;
		private Task _portCheckTask = null;
		
		public static void ShowWindow() {
			GetWindow(typeof(RealmEditorTrainingWaitWindow));
		}

		private void OnGUI() {
			GUILayout.Label("Waiting for Training to Start");
		}

		private void Update() {
			if (_portOpen) {
				EditorApplication.EnterPlaymode();
				Close();
			} else {
				if (_portCheckTask == null || _portCheckTask.IsCompleted) {
					_portCheckTask = CheckTrainingPortOpen();
				}
			}
		}
		
		private async Task CheckTrainingPortOpen() {
			using (TcpClient tcpClient = new TcpClient()) {
				try {
					await tcpClient.ConnectAsync("127.0.0.1", EditorTrainingPort);
					_portOpen = true;
				} catch (Exception) {
					_portOpen = false;
				}
			}
		}

	}
}
