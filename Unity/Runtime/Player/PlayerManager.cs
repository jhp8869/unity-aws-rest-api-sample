using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Portfolio.Game.Player
{
    /// <summary>
    /// 실제 MiningWarrior의 PlayerManager처럼 로그인 결과를 보관하는 현재 플레이어 저장소다.
    /// </summary>
    public sealed class PlayerManager : MonoBehaviour
    {
        private static PlayerManager instance;
        public static PlayerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject host = new GameObject(nameof(PlayerManager));
                    instance = host.AddComponent<PlayerManager>();
                }
                return instance;
            }
        }
        private readonly PlayerInfo currentPlayer = new PlayerInfo();
        private readonly Dictionary<string, PlayerInfo> otherPlayers = new Dictionary<string, PlayerInfo>();

        /// <summary>원본의 GetCurrentPlayer와 동일한 현재 플레이어 데이터 접근점이다.</summary>
        public PlayerInfo GetCurrentPlayer => currentPlayer;
        public PlayerInfo CurrentPlayer => currentPlayer;

        /// <summary>게임 씬의 Player 태그 오브젝트. 각 게임 시스템이 이 참조를 사용한다.</summary>
        public GameObject CurrentPlayerGameObject { get; private set; }

        public IReadOnlyDictionary<string, PlayerInfo> OtherPlayers => otherPlayers;
        public event Action<GameObject> CurrentPlayerObjectChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshCurrentPlayerGameObject();
        }

        private void OnDestroy()
        {
            if (instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshCurrentPlayerGameObject();
        }

        private void RefreshCurrentPlayerGameObject()
        {
            CurrentPlayerGameObject = null;
            try
            {
                CurrentPlayerGameObject = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                // 샘플 씬에 Player 태그가 없어도 데이터 전용 사용은 가능하다.
            }
            CurrentPlayerObjectChanged?.Invoke(CurrentPlayerGameObject);
        }

        public void RegisterOtherPlayer(PlayerInfo player)
        {
            if (player != null && !string.IsNullOrEmpty(player.PlayerId))
                otherPlayers[player.PlayerId] = player;
        }

        public bool TryGetOtherPlayer(string playerId, out PlayerInfo player)
        {
            return otherPlayers.TryGetValue(playerId, out player);
        }

        public void RemoveOtherPlayer(string playerId)
        {
            if (!string.IsNullOrEmpty(playerId))
                otherPlayers.Remove(playerId);
        }
    }
}
