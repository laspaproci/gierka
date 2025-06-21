using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Respawner : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Transform spawnPoint;
    public float     respawnDelay  = 2f;
    public float     deathAnimTime = 1.5f;

    private Health          health;
    private Animator        animator;
    private Rigidbody2D     rb;
    private SpriteRenderer  sr;
    private bool            isDead = false;

    void Awake()
    {
        health   = GetComponent<Health>();
        animator = GetComponent<Animator>();
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();

        health.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        // 1) zatrzymaj symulację fizyki i ukryj sprite
        rb.simulated        = false;
        if (sr != null) sr.enabled = false;

        // 2) zacznij coroutine respawnu
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        // czekaj animację śmierci + dodatkowe opóźnienie
        yield return new WaitForSeconds(deathAnimTime + respawnDelay);

        // teleport
        if (spawnPoint != null)
            transform.position = spawnPoint.position;
        else
            Debug.LogWarning($"[{name}] Brak przypisanego SpawnPoint!");

        // reset fizyki: zerujemy linear i angular Velocity
        rb.simulated         = true;
        rb.linearVelocity    = Vector2.zero;
        rb.angularVelocity   = 0f;

        // reset zdrowia i animatora
        health.ResetHealth();
        animator.Rebind();
        animator.Update(0f);

        // przywróć widoczność sprite
        if (sr != null) sr.enabled = true;

        isDead = false;
    }
}
