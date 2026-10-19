using UnityEngine;

namespace EyesOff.Player
{
    /// <summary>
    /// Feeds the locomotion blend tree. Runs on every client and derives the speed from how the
    /// transform actually moved, so remote players animate from the position the NetworkTransform
    /// already replicates. No extra network data needed.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");

        [SerializeField] private Animator animator;
        [Tooltip("Speed (m/s) that maps to a blend value of 1. Match PlayerController's move speed.")]
        [SerializeField] private float fullBlendSpeed = 3.5f;
        [SerializeField] private float dampTime = 0.1f;

        private Vector3 _lastPosition;

        private void OnEnable() => _lastPosition = transform.position;

        private void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 worldVelocity = (transform.position - _lastPosition) / dt;
            _lastPosition = transform.position;

            // Local space: x = strafe, z = forward/backward.
            Vector3 local = transform.InverseTransformDirection(worldVelocity) / fullBlendSpeed;

            animator.SetFloat(MoveX, Mathf.Clamp(local.x, -1f, 1f), dampTime, Time.deltaTime);
            animator.SetFloat(MoveY, Mathf.Clamp(local.z, -1f, 1f), dampTime, Time.deltaTime);
        }
    }
}
