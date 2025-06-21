using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Health))]
public class PlayerInputSystem : MonoBehaviour
{
    [Header("Ruch i skok")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Atak")]
    public float attackRange   = 0.02f;
    public float attackRadius  = 0.2f;
    public int   attackDamage  = 20;
    public LayerMask targetLayer;

    [Header("Fall Death")]
    public float deathY      = -5f;
    public int   fallDamage  = 9999;

    [Header("Respawn")]
    public Transform spawnPoint;
    public float     respawnDelay = 2f;

    // stany
    private Vector2     moveInput;
    private bool        isGrounded    = true;
    private int         lastDirection = 1;
    private bool        isDead        = false;

    // referencje
    private Vector3        baseScale;
    private Rigidbody2D    rb;
    private Animator       animator;
    private Health         health;
    private PlayerInputActions inputActions;
    private bool           eventsBound   = false;

    private void Awake()
    {
        rb           = GetComponent<Rigidbody2D>();
        animator     = GetComponent<Animator>();
        health       = GetComponent<Health>();
        baseScale    = transform.localScale;
        inputActions = new PlayerInputActions();

        if (health != null)
            health.OnDeath += OnPlayerDied;
    }

    private void OnEnable()
    {
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        var gm = inputActions.Gameplay;
        gm.Enable();

        if (!eventsBound)
        {
            gm.Move.performed   += OnMove;
            gm.Move.canceled    += OnMoveCanceled;
            gm.Jump.performed   += OnJump;
            gm.Fall.performed   += OnFall;
            gm.Attack.performed += OnAttack;
            eventsBound = true;
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            var gm = inputActions.Gameplay;
            if (eventsBound)
            {
                gm.Move.performed   -= OnMove;
                gm.Move.canceled    -= OnMoveCanceled;
                gm.Jump.performed   -= OnJump;
                gm.Fall.performed   -= OnFall;
                gm.Attack.performed -= OnAttack;
                eventsBound = false;
            }
            gm.Disable();
        }

        if (health != null)
            health.OnDeath -= OnPlayerDied;
    }

    private void Update()
    {
        if (isDead) return;

        Move();

        // Fall death
        if (transform.position.y < deathY && health != null)
            health.TakeDamage(fallDamage);
    }

    private void OnMove(InputAction.CallbackContext ctx)       => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void Move()
    {
        float m = moveInput.x;
        rb.linearVelocity = new Vector2(m * moveSpeed, rb.linearVelocity.y);
        animator.SetFloat("Speed", Mathf.Abs(m));

        if (m > 0f)
        {
            lastDirection = 1;
            animator.SetBool("FacingRight", true);
            transform.localScale = baseScale;
        }
        else if (m < 0f)
        {
            lastDirection = -1;
            animator.SetBool("FacingRight", false);
            transform.localScale = baseScale;
        }
        else
        {
            transform.localScale = new Vector3(
                baseScale.x * lastDirection,
                baseScale.y,
                baseScale.z
            );
        }
    }

    private void OnJump(InputAction.CallbackContext ctx) => Jump();
    private void Jump()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
        animator.SetBool("IsJumping", true);
    }

    private void OnFall(InputAction.CallbackContext ctx) => Fall();
    private void Fall()
    {
        if (!isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpForce);
    }

    private void OnAttack(InputAction.CallbackContext ctx) => Attack();
    private void Attack()
    {
        transform.localScale = new Vector3(baseScale.x * lastDirection, baseScale.y, baseScale.z);
        animator.SetTrigger("Attack");

        Vector2 origin = (Vector2)transform.position + Vector2.right * attackRange * lastDirection;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius, targetLayer);
        foreach (var h in hits)
            h.GetComponent<Health>()?.TakeDamage(attackDamage);
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.contacts.Length > 0 && c.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            animator.SetBool("IsJumping", false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Vector2 origin = (Vector2)transform.position + Vector2.right * attackRange * lastDirection;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }

    private void OnPlayerDied()
    {
        if (isDead) return;
        isDead = true;

        // zatrzymaj fizykę i sterowanie
        rb.simulated = false;
        enabled      = false;

        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        // czekaj na animację + delay
        yield return new WaitForSeconds(respawnDelay + 1.5f);

        // teleport
        if (spawnPoint != null)
            transform.position = spawnPoint.position;
        else
            Debug.LogWarning("[PlayerInputSystem] SpawnPoint nie został przypisany!");

        // reset fizyki
        rb.simulated       = true;
        rb.linearVelocity  = Vector2.zero;
        isGrounded         = true;

        // reset zdrowia i animatora
        health?.ResetHealth();
        animator.Rebind();
        animator.Update(0f);

        isDead  = false;
        enabled = true;
    }
}
