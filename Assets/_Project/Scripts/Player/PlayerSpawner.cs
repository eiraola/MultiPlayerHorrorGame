using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectScare.Player
{
    /// <summary>
    /// Creates one player per connected client once everybody has loaded the scene it lives in.
    /// Only the host spawns; Netcode removes a player when its client leaves.
    /// </summary>
    public sealed class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private NetworkSceneManager sceneManager;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            // Waits for every client: spawning while they still load the scene would place
            // the players in a scene they do not have yet.
            sceneManager = NetworkManager.SceneManager;
            sceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        }

        public override void OnNetworkDespawn() => StopListening();

        private void StopListening()
        {
            if (sceneManager == null)
                return;

            sceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            sceneManager = null;
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != gameObject.scene.name)
                return;

            StopListening();

            int spawnIndex = 0;

            foreach (ulong clientId in completed)
            {
                // A client that finished loading may have been dropped by the time this runs.
                if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
                    continue;

                SpawnPlayer(clientId, spawnPoints[spawnIndex % spawnPoints.Length]);
                spawnIndex++;
            }
        }

        private void SpawnPlayer(ulong clientId, Transform spawnPoint)
        {
            NetworkObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // The active scene is not this one yet, and Netcode keeps the scene the object is in when spawned.
            SceneManager.MoveGameObjectToScene(player.gameObject, gameObject.scene);
            player.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }
    }
}
