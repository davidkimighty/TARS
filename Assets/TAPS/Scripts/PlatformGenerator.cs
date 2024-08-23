using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace TAPS
{
    [Serializable]
    public class Platform
    {
        public GameObject Object;
        public Platform Right;
        public Platform Left;

        public void Reset()
        {
            Right = null;
            Left = null;
        }
    }
    public class PlatformGenerator : MonoBehaviour
    {
        [SerializeField] private InputActionProperty _moveAction;
        [SerializeField] private GameObject _placeHolder;
        [SerializeField] private GameObject _platformPrefab;
        [SerializeField] private Transform _movingObject;
        [SerializeField] private float _threshold = 1.1f;
        [SerializeField] private float _spawnDistance = 20f;
        
        private IObjectPool<Platform> _pool;
        private Platform _currentPlatform;
        private Vector3 _moveDir;
        
        private void OnEnable()
        {
            _moveAction.action.performed += ReadMove;
        }

        private void OnDisable()
        {
            _moveAction.action.performed -= ReadMove;
        }
        
        private void Start()
        {
            _pool = new ObjectPool<Platform>(CreatePlatform, OnTakeFromPool,
                OnReturnedToPool, OnDestroyPoolObject, true, 5, 10);
            
            Destroy(_placeHolder);
            _currentPlatform = _pool.Get();
            _currentPlatform.Object.transform.position = Vector3.zero;
        }
        
        private void Update()
        {
            if (_moveDir.magnitude <= 0) return;
            
            Vector3 toObject = _movingObject.position - _currentPlatform.Object.transform.position;
            float dot = Vector3.Dot(_moveDir, toObject.normalized);
            if (dot <= 0 || toObject.magnitude < _threshold) return;
            
            bool isRight = _moveDir.x > 0;
            bool nextEmpty = (isRight && _currentPlatform.Right == null) || (!isRight && _currentPlatform.Left == null);
            if (!nextEmpty)
            {
                _currentPlatform = isRight ? _currentPlatform.Right : _currentPlatform.Left;
                return;
            }
            
            Platform newPlatform = _pool.Get();
            Vector3 spawnPoint = _currentPlatform.Object.transform.position + (isRight ? Vector3.right : Vector3.left) * _spawnDistance;
            newPlatform.Object.transform.position = spawnPoint;

            if (isRight)
            {
                _currentPlatform.Right = newPlatform;
                newPlatform.Left = _currentPlatform;
                if (_currentPlatform.Left != null && _currentPlatform.Left.Object.activeSelf)
                {
                    _pool.Release(_currentPlatform.Left);
                    _currentPlatform.Left = null;
                }
            }
            else
            {
                _currentPlatform.Left = newPlatform;
                newPlatform.Right = _currentPlatform;
                if (_currentPlatform.Right != null && _currentPlatform.Right.Object.activeSelf)
                {
                    _pool.Release(_currentPlatform.Right);
                    _currentPlatform.Right = null;
                }
            }
            _currentPlatform = newPlatform;
        }
        
        private void ReadMove(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            input.y = 0f;
            _moveDir = input;
        }
        
        private Platform CreatePlatform()
        {
            GameObject platformObj = Instantiate(_platformPrefab);
            platformObj.gameObject.SetActive(false);
            
            return new Platform { Object = platformObj };
        }

        private void OnReturnedToPool(Platform platform)
        {
            platform.Reset();
            platform.Object.SetActive(false);
        }

        private void OnTakeFromPool(Platform platform) => platform.Object.SetActive(true);

        private void OnDestroyPoolObject(Platform platform) => Destroy(platform.Object);
    }
}
