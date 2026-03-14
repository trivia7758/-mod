using System;
using System.Collections;
using UnityEngine;

namespace CaoCao.Battle
{
    /// <summary>
    /// Handles sprite animation for a battle unit.
    /// Directions: Down, Up, Left, Right.
    /// Left-facing sprites can be flipped for Right.
    /// </summary>
    public enum FacingDirection { Down, Up, Left, Right }

    public class BattleUnitAnimator : MonoBehaviour
    {
        SpriteRenderer _sr;
        FacingDirection _facing = FacingDirection.Down;
        bool _isAnimating;
        float _walkTimer;
        int _walkFrame;
        bool _isWalking;

        // Walk animation speed — must be faster than step duration (0.15s per cell)
        const float WalkFrameInterval = 0.1f;

        public FacingDirection Facing => _facing;

        public event Action OnDeathAnimComplete;

        void Awake()
        {
            BattleSpriteSheet.EnsureLoaded();
        }

        /// <summary>
        /// Initialize: find or create sprite renderer, set initial idle sprite.
        /// </summary>
        public void Init(SpriteRenderer sr)
        {
            _sr = sr;
            if (_sr == null)
            {
                _sr = GetComponentInChildren<SpriteRenderer>();
            }
            SetIdle(FacingDirection.Down);
        }

        void Update()
        {
            if (_isWalking)
            {
                _walkTimer += Time.deltaTime;
                if (_walkTimer >= WalkFrameInterval)
                {
                    _walkTimer -= WalkFrameInterval;
                    _walkFrame = 1 - _walkFrame; // toggle 0/1
                    ApplyWalkFrame();
                }
            }
        }

        // === Public API ===

        /// <summary>
        /// Set idle pose facing a direction.
        /// </summary>
        public void SetIdle(FacingDirection dir)
        {
            _facing = dir;
            _isWalking = false;
            _isAnimating = false;

            if (_sr == null) return;

            switch (dir)
            {
                case FacingDirection.Down:
                    SetSprite(BattleSpriteSheet.IdleDown, false);
                    break;
                case FacingDirection.Up:
                    SetSprite(BattleSpriteSheet.IdleUp, false);
                    break;
                case FacingDirection.Left:
                    // Frame 8 is native left-facing idle
                    SetSprite(BattleSpriteSheet.IdleLeft, false);
                    break;
                case FacingDirection.Right:
                    // Idle right = flip left-facing idle
                    SetSprite(BattleSpriteSheet.IdleLeft, true);
                    break;
            }
        }

        /// <summary>
        /// Start walking animation in the given direction.
        /// Call StopWalking() when movement ends.
        /// </summary>
        public void StartWalking(FacingDirection dir)
        {
            _facing = dir;
            _isWalking = true;
            _walkTimer = 0;
            _walkFrame = 0;
            ApplyWalkFrame();
        }

        /// <summary>
        /// Determine facing direction from movement delta and start/continue walking.
        /// If already walking in the same direction, keeps frame cycling going (no reset).
        /// </summary>
        public void StartWalkingTowards(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            FacingDirection dir;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                dir = delta.x >= 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                dir = delta.y >= 0 ? FacingDirection.Up : FacingDirection.Down;

            // If already walking in this direction, just keep going — don't reset frame
            if (_isWalking && _facing == dir)
                return;

            StartWalking(dir);
        }

        /// <summary>
        /// Stop walking and return to idle in current facing direction.
        /// </summary>
        public void StopWalking()
        {
            _isWalking = false;
            SetIdle(_facing);
        }

        /// <summary>
        /// Face towards a target cell from current cell.
        /// </summary>
        public void FaceTowards(Vector2Int from, Vector2Int target)
        {
            var delta = target - from;
            FacingDirection dir;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                dir = delta.x >= 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                dir = delta.y >= 0 ? FacingDirection.Up : FacingDirection.Down;

            SetIdle(dir);
        }

        /// <summary>
        /// Play attack animation (briefly show block/thrust frame then return to idle).
        /// </summary>
        public void PlayAttack(FacingDirection dir, Action onComplete = null)
        {
            _facing = dir;
            _isWalking = false;
            StartCoroutine(AttackAnim(dir, onComplete));
        }

        /// <summary>
        /// Play hit/damaged animation.
        /// </summary>
        public void PlayHit(Action onComplete = null)
        {
            _isWalking = false;
            StartCoroutine(HitAnim(onComplete));
        }

        /// <summary>
        /// Play block/defend animation.
        /// </summary>
        public void PlayBlock(FacingDirection dir, Action onComplete = null)
        {
            _isWalking = false;
            StartCoroutine(BlockAnim(dir, onComplete));
        }

        /// <summary>
        /// Play death animation (gasp frames then fade out).
        /// </summary>
        public void PlayDeath(Action onComplete = null)
        {
            _isWalking = false;
            StartCoroutine(DeathAnim(onComplete));
        }

        /// <summary>
        /// Play victory/levelup animation.
        /// </summary>
        public void PlayVictory(Action onComplete = null)
        {
            _isWalking = false;
            StartCoroutine(VictoryAnim(onComplete));
        }

        // === Animation Coroutines ===

        IEnumerator AttackAnim(FacingDirection dir, Action onComplete)
        {
            _isAnimating = true;

            // Show block/thrust frame for the direction (reuse block sprites as attack pose)
            switch (dir)
            {
                case FacingDirection.Down:
                    SetSprite(BattleSpriteSheet.BlockDown, false);
                    break;
                case FacingDirection.Up:
                    SetSprite(BattleSpriteSheet.BlockUp, false);
                    break;
                case FacingDirection.Left:
                    // Block left is native (frame 2)
                    SetSprite(BattleSpriteSheet.BlockLeft, false);
                    break;
                case FacingDirection.Right:
                    // Block right = flip block left
                    SetSprite(BattleSpriteSheet.BlockLeft, true);
                    break;
            }

            // Brief lunge forward
            Vector3 originalPos = transform.position;
            Vector3 lungeDir = dir switch
            {
                FacingDirection.Down => Vector3.down,
                FacingDirection.Up => Vector3.up,
                FacingDirection.Left => Vector3.left,
                FacingDirection.Right => Vector3.right,
                _ => Vector3.zero
            };

            float lungeDistance = 8f;
            transform.position = originalPos + lungeDir * lungeDistance;
            yield return new WaitForSeconds(0.15f);
            transform.position = originalPos;
            yield return new WaitForSeconds(0.1f);

            _isAnimating = false;
            SetIdle(dir);
            onComplete?.Invoke();
        }

        IEnumerator HitAnim(Action onComplete)
        {
            _isAnimating = true;

            SetSprite(BattleSpriteSheet.Hit, false);

            // Flash effect
            if (_sr != null)
            {
                Color original = _sr.color;
                _sr.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                _sr.color = original;
                yield return new WaitForSeconds(0.15f);
                _sr.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                _sr.color = original;
            }

            yield return new WaitForSeconds(0.1f);

            _isAnimating = false;
            SetIdle(_facing);
            onComplete?.Invoke();
        }

        IEnumerator BlockAnim(FacingDirection dir, Action onComplete)
        {
            _isAnimating = true;

            switch (dir)
            {
                case FacingDirection.Down:
                    SetSprite(BattleSpriteSheet.BlockDown, false);
                    break;
                case FacingDirection.Up:
                    SetSprite(BattleSpriteSheet.BlockUp, false);
                    break;
                case FacingDirection.Left:
                    // Block left is native
                    SetSprite(BattleSpriteSheet.BlockLeft, false);
                    break;
                case FacingDirection.Right:
                    // Block right = flip block left
                    SetSprite(BattleSpriteSheet.BlockLeft, true);
                    break;
            }

            yield return new WaitForSeconds(0.3f);

            _isAnimating = false;
            SetIdle(dir);
            onComplete?.Invoke();
        }

        IEnumerator DeathAnim(Action onComplete)
        {
            _isAnimating = true;

            // Gasp animation: alternate between two frames
            for (int i = 0; i < 3; i++)
            {
                SetSprite(BattleSpriteSheet.DeathGasp0, false);
                yield return new WaitForSeconds(0.2f);
                SetSprite(BattleSpriteSheet.DeathGasp1, false);
                yield return new WaitForSeconds(0.2f);
            }

            // Fade out
            if (_sr != null)
            {
                float elapsed = 0;
                float duration = 0.5f;
                Color c = _sr.color;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    c.a = 1f - (elapsed / duration);
                    _sr.color = c;
                    yield return null;
                }
                c.a = 0;
                _sr.color = c;
            }

            _isAnimating = false;
            onComplete?.Invoke();
            OnDeathAnimComplete?.Invoke();
        }

        IEnumerator VictoryAnim(Action onComplete)
        {
            _isAnimating = true;
            SetSprite(BattleSpriteSheet.Victory, false);

            // Bounce up and down
            Vector3 originalPos = transform.position;
            for (int i = 0; i < 3; i++)
            {
                float elapsed = 0;
                float duration = 0.15f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float yOffset = Mathf.Sin(t * Mathf.PI) * 6f;
                    transform.position = originalPos + Vector3.up * yOffset;
                    yield return null;
                }
                transform.position = originalPos;
            }

            yield return new WaitForSeconds(0.3f);

            _isAnimating = false;
            SetIdle(FacingDirection.Down);
            onComplete?.Invoke();
        }

        // === Internal ===

        void ApplyWalkFrame()
        {
            if (_sr == null) return;

            switch (_facing)
            {
                case FacingDirection.Down:
                    SetSprite(_walkFrame == 0 ? BattleSpriteSheet.WalkDown0 : BattleSpriteSheet.WalkDown1, false);
                    break;
                case FacingDirection.Up:
                    SetSprite(_walkFrame == 0 ? BattleSpriteSheet.WalkUp0 : BattleSpriteSheet.WalkUp1, false);
                    break;
                case FacingDirection.Left:
                    // Frames 4,5 are native left-facing walk
                    SetSprite(_walkFrame == 0 ? BattleSpriteSheet.WalkRight0 : BattleSpriteSheet.WalkRight1, false);
                    break;
                case FacingDirection.Right:
                    // Walk right = flip left-facing frames
                    SetSprite(_walkFrame == 0 ? BattleSpriteSheet.WalkRight0 : BattleSpriteSheet.WalkRight1, true);
                    break;
            }
        }

        void SetSprite(Sprite sprite, bool flipX)
        {
            if (_sr == null || sprite == null) return;
            _sr.sprite = sprite;
            _sr.flipX = flipX;
        }
    }
}
