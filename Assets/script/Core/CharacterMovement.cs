using UnityEngine;
using System.Collections;

namespace Elementor.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CharacterView))]
    public class CharacterMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float minIdleTime = 2f;
        [SerializeField] private float maxIdleTime = 5f;
        [SerializeField] private float patrolRadius = 3f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 1f;

        private Rigidbody rb;
        private CharacterView characterView;
        private Vector3 startPosition;
        private Coroutine movementCoroutine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            characterView = GetComponent<CharacterView>();
            startPosition = transform.position;
        }

        private void OnEnable()
        {
            // characterView.OnAnimationStateChanged += HandleAnimationStateChanged;
        }

        private void OnDisable()
        {
            // characterView.OnAnimationStateChanged -= HandleAnimationStateChanged;
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
            }
        }

        private void HandleAnimationStateChanged(CharacterView view, CharacterAnimationState previous, CharacterAnimationState current)
        {
            if (current == CharacterAnimationState.Idle)
            {
                rb.isKinematic = false;
                StartRandomMovement();
            }
            else
            {
                rb.isKinematic = true;
                StopRandomMovement();
            }
        }

        public void StartRandomMovement()
        {
            if (movementCoroutine == null)
            {
                movementCoroutine = StartCoroutine(RandomMovementRoutine());
            }
        }

        public void StopRandomMovement()
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
                rb.velocity = Vector3.zero;
            }
        }

        private IEnumerator RandomMovementRoutine()
        {
            while (true)
            {
                float idleTime = Random.Range(minIdleTime, maxIdleTime);
                yield return new WaitForSeconds(idleTime);

                Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                randomDirection.y = 0;
                Vector3 targetPosition = startPosition + randomDirection;

                if (IsSafeDestination(targetPosition))
                {
                    yield return MoveTo(targetPosition);
                }
            }
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            transform.LookAt(destination);
            
            while (Vector3.Distance(transform.position, destination) > 0.1f)
            {
                if (!IsSafePath())
                {
                    rb.velocity = Vector3.zero;
                    yield break;
                }
                Vector3 moveDirection = (destination - transform.position).normalized;
                rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
                yield return null;
            }
            rb.velocity = Vector3.zero;
        }

        private bool IsSafeDestination(Vector3 destination)
        {
            return Physics.Raycast(destination + Vector3.up * 0.5f, Vector3.down, groundCheckDistance, groundLayer);
        }

        private bool IsSafePath()
        {
            Vector3 front = transform.position + transform.forward * 0.5f;
            return Physics.Raycast(front + Vector3.up * 0.5f, Vector3.down, groundCheckDistance, groundLayer);
        }
    }
}
