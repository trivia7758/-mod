using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CaoCao.Story
{
    public class StoryUnit : MonoBehaviour
    {
        [Header("Grid (set by controller from StoryMapData)")]
        public Vector2Int tile;
        public bool isIsometric = true;

        [Header("Tile dimensions (world units, matches editor grid overlay)")]
        [Tooltip("Diamond half-width in world units")]
        public float tileHalfWidth = 0.24f;
        [Tooltip("Diamond half-height in world units")]
        public float tileHalfHeight = 0.24f;
        [Tooltip("Grid origin offset in world space")]
        public Vector2 gridOffset = new(0.24f, -0.24f);

        [Header("Animation")]
        [SerializeField] SpriteRenderer spriteRenderer;
        public Sprite[] frontFrames; // idle_down, walk_down frames
        public Sprite[] backFrames;  // idle_up, walk_up frames
        public int frameWidth = 48;
        public int frameHeight = 64;

        [Header("Movement")]
        [SerializeField] int stepFrames = 10;
        [SerializeField] int animTick = 2;

        public event Action OnReachedTile;

        Vector2Int _dir = Vector2Int.down;
        bool _moving;
        List<Vector2Int> _path = new();
        int _pathIndex;
        Vector3 _startWorld;
        Vector3 _targetWorld;
        int _progressFrame;
        float _accumulator;
        int _animCounter;
        const float LogicStep = 1f / 60f;

        // Animation state
        string _currentAnim = "";
        int _currentFrame;

        void Start()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            PlayIdle(_dir);
        }

        void Update()
        {
            if (!_moving) return;
            _accumulator += Time.deltaTime;
            while (_accumulator >= LogicStep)
            {
                _accumulator -= LogicStep;
                StepLogicFrame();
            }
        }

        /// <summary>
        /// Convert tile coordinate to Unity world position.
        /// Uses the same formula as StoryMapEditor.TileCenter():
        ///   x = (tile.x - tile.y) * tileHalfWidth  + gridOffset.x
        ///   y = (tile.x + tile.y) * (-tileHalfHeight) + gridOffset.y
        /// </summary>
        public Vector2 TileToWorld(Vector2Int t)
        {
            float x = (t.x - t.y) * tileHalfWidth + gridOffset.x;
            float y = (t.x + t.y) * (-tileHalfHeight) + gridOffset.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Convert Unity world position back to tile coordinate.
        /// Inverse of TileToWorld.
        /// </summary>
        public Vector2Int WorldToTile(Vector2 pos)
        {
            float dx = pos.x - gridOffset.x;
            float dy = pos.y - gridOffset.y;
            float sum = dx / tileHalfWidth;       // tx - ty
            float dif = dy / (-tileHalfHeight);   // tx + ty
            float fx = (sum + dif) * 0.5f;
            float fy = (dif - sum) * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(fx), Mathf.RoundToInt(fy));
        }

        public void FaceDir(Vector2Int dir)
        {
            // Don't use ClampDir here — FaceDir receives already-correct
            // Unity directions from ParseDir.
            _dir = dir;
            PlayIdle(_dir);
        }

        public void MoveAlong(List<Vector2Int> path)
        {
            if (_moving || path.Count == 0)
            {
                OnReachedTile?.Invoke();
                return;
            }
            _path = new List<Vector2Int>(path);
            _pathIndex = 0;
            BeginStep();
            _moving = true;
        }

        void BeginStep()
        {
            if (_pathIndex >= _path.Count)
            {
                FinishMove();
                return;
            }
            var nextTile = _path[_pathIndex];
            var dir = nextTile - tile;
            _dir = ClampDir(dir);
            _startWorld = transform.position;
            _targetWorld = TileToWorld(nextTile);
            _progressFrame = 0;
            _animCounter = 0;
            PlayWalk(_dir);
        }

        void StepLogicFrame()
        {
            if (!_moving) return;
            _progressFrame++;
            float t = (float)_progressFrame / stepFrames;
            transform.position = Vector3.Lerp(_startWorld, _targetWorld, t);
            TickWalkAnim();
            if (_progressFrame >= stepFrames)
            {
                transform.position = _targetWorld;
                tile = _path[_pathIndex];
                _pathIndex++;
                BeginStep();
            }
        }

        void FinishMove()
        {
            _moving = false;
            PlayIdle(_dir);
            OnReachedTile?.Invoke();
        }

        Vector2Int ClampDir(Vector2Int dir)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                return dir.x > 0 ? Vector2Int.right : Vector2Int.left;
            return dir.y > 0 ? Vector2Int.down : Vector2Int.up;
        }

        // Animation
        void PlayWalk(Vector2Int dir)
        {
            bool isBack = (isIsometric)
                ? (dir == Vector2Int.left || dir == Vector2Int.up)
                : (dir == Vector2Int.up);
            _currentAnim = "walk";
            _currentFrame = 0;
            UpdateSpriteFlip(dir);
            SetSpriteFrame(isBack ? backFrames : frontFrames, 0);
        }

        void PlayIdle(Vector2Int dir)
        {
            bool isBack = (isIsometric)
                ? (dir == Vector2Int.left || dir == Vector2Int.up)
                : (dir == Vector2Int.up);
            _currentAnim = "idle";
            _currentFrame = 0;
            UpdateSpriteFlip(dir);
            SetSpriteFrame(isBack ? backFrames : frontFrames, 0);
        }

        void TickWalkAnim()
        {
            if (_currentAnim != "walk") return;
            _animCounter++;
            if (_animCounter % Mathf.Max(1, animTick) != 0) return;

            bool isBack = (isIsometric)
                ? (_dir == Vector2Int.left || _dir == Vector2Int.up)
                : (_dir == Vector2Int.up);
            var frames = isBack ? backFrames : frontFrames;
            if (frames == null || frames.Length <= 1) return;
            _currentFrame = (_currentFrame + 1) % frames.Length;
            SetSpriteFrame(frames, _currentFrame);
        }

        void UpdateSpriteFlip(Vector2Int dir)
        {
            if (spriteRenderer == null) return;
            if (isIsometric)
                spriteRenderer.flipX = (dir == Vector2Int.right || dir == Vector2Int.left);
            else
                spriteRenderer.flipX = (dir == Vector2Int.right);
        }

        void SetSpriteFrame(Sprite[] frames, int index)
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0) return;
            index = Mathf.Clamp(index, 0, frames.Length - 1);
            spriteRenderer.sprite = frames[index];
        }

        /// <summary>
        /// Move along a list of waypoints at the given speed.
        /// Each waypoint has a position and an optional face direction.
        /// FaceDirection.Auto = determine from movement delta.
        /// </summary>
        public IEnumerator MoveAlongWorld(List<Waypoint> waypoints, float speed)
        {
            if (waypoints == null || waypoints.Count == 0) yield break;

            // World-space movement uses non-iso direction logic
            bool wasIso = isIsometric;
            isIsometric = false;

            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp = waypoints[i];
                Vector3 start = transform.position;
                Vector3 end = new Vector3(wp.position.x, wp.position.y, start.z);

                // Determine direction: explicit face or auto from delta
                Vector2 delta = wp.position - (Vector2)start;
                if (wp.face != FaceDirection.Auto)
                {
                    _dir = FaceDirToVector(wp.face);
                }
                else if (delta.sqrMagnitude > 0.001f)
                {
                    _dir = DeltaToDir(delta);
                }

                // Reset animation state for new segment
                _animCounter = 0;
                _currentFrame = 0;
                _worldAnimTimer = 0f;
                PlayWalk(_dir);

                float dist = Vector3.Distance(start, end);
                if (dist < 0.001f) continue;

                float duration = dist / Mathf.Max(speed, 0.01f);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.position = Vector3.Lerp(start, end, t);
                    // Time-based animation tick for smooth walk cycle
                    _worldAnimTimer += Time.deltaTime;
                    if (_worldAnimTimer >= WorldAnimInterval)
                    {
                        _worldAnimTimer -= WorldAnimInterval;
                        TickWalkAnim();
                    }
                    yield return null;
                }
                transform.position = end;
            }

            isIsometric = wasIso;
            PlayIdle(_dir);
        }

        // Time-based animation for MoveAlongWorld (instead of frame-counter)
        float _worldAnimTimer;
        const float WorldAnimInterval = 0.15f; // seconds between walk frames

        /// <summary>
        /// Convert a movement delta to the nearest cardinal direction.
        /// </summary>
        Vector2Int DeltaToDir(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        static Vector2Int FaceDirToVector(FaceDirection dir)
        {
            return dir switch
            {
                FaceDirection.Up => Vector2Int.up,
                FaceDirection.Down => Vector2Int.down,
                FaceDirection.Left => Vector2Int.left,
                FaceDirection.Right => Vector2Int.right,
                _ => Vector2Int.down
            };
        }

        public void PlayTalk()
        {
            var talk = transform.Find("TalkBubble");
            if (talk != null) talk.gameObject.SetActive(true);
        }

        public void StopTalk()
        {
            var talk = transform.Find("TalkBubble");
            if (talk != null) talk.gameObject.SetActive(false);
        }
    }
}
