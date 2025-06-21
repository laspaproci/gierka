using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerOnlineController : NetworkBehaviour
{
    [Header("Ruch i skok")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Atak")]
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private LayerMask targetLayer;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [Tooltip("Lista punktów startowych, przypisz w inspektorze")]
    [SerializeField] private Transform[] spawnPoints;

    // HP synchro
    private NetworkVariable<int> hp = new NetworkVariable<int>(100);

    // Komponenty
    private Rigidbody2D rb;
    private Animator animator;
    private Vector3 baseScale;
    private bool isGrounded = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        UIManager.Instance.RegisterPlayer(OwnerClientId);
        hp.OnValueChanged += OnHpChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
            UIManager.Instance.UnregisterPlayer(OwnerClientId);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleJump();
        HandleFall();
        HandleAttack();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
        animator.SetFloat("Speed", Mathf.Abs(h));

        if (h > 0f)
            transform.localScale = baseScale;
        else if (h < 0f)
            transform.localScale = new Vector3(-baseScale.x, baseScale.y, baseScale.z);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
            animator.SetBool("IsJumping", true);
        }
    }

    private void HandleFall()
    {
        if (Input.GetButton("Fall") && !isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpForce);
    }

    private void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1"))
            SubmitAttackServerRpc();
    }

    [ServerRpc]
    private void SubmitAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.right * attackRange * Mathf.Sign(transform.localScale.x);
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius, targetLayer);
        foreach (var hit in hits)
        {
            var pc = hit.GetComponent<PlayerOnlineController>();
            if (pc != null)
                pc.hp.Value = Mathf.Max(0, pc.hp.Value - attackDamage);
        }

        PlayAttackClientRpc();
    }

    [ClientRpc]
    private void PlayAttackClientRpc(ClientRpcParams rpcParams = default)
    {
        animator.SetTrigger("Attack");
    }

    private void OnHpChanged(int oldHp, int newHp)
    {
        if (IsOwner)
            UIManager.Instance.UpdateHpDisplay(OwnerClientId, newHp);

        if (newHp <= 0)
            StartCoroutine(HandleDeathAndRespawn());
    }

    private IEnumerator HandleDeathAndRespawn()
    {
        animator.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        if (IsServer)
            hp.Value = 100;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            transform.position = spawn.position;
        }

        GetComponent<Collider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        animator.ResetTrigger("Die");
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
        Vector2 origin = (Vector2)transform.position + Vector2.right * attackRange * Mathf.Sign(transform.localScale.x);
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
