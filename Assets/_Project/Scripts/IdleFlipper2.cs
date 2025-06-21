using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IdleFlipper2 : MonoBehaviour
{
    [Tooltip("Parametr prędkości w Animatorze (domyślnie 'Speed')")]
    public string speedParam = "Speed";

    [Tooltip("Parametr kierunku w Animatorze (domyślnie 'FacingRight')")]
    public string facingParam = "FacingRight";

    [Tooltip("Próg deadzone dla Speed — poniżej tej wartości uznajemy za idle")]
    [Range(0f, 0.5f)]
    public float zeroSpeedThreshold = 0.01f;

    private Animator animator;
    private Vector3  baseScale;

    void Awake()
    {
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;
        baseScale.x = Mathf.Abs(baseScale.x);
    }

    void LateUpdate()
    {
        float currentSpeed = animator.GetFloat(speedParam);

        if (Mathf.Abs(currentSpeed) <= zeroSpeedThreshold)
        {
            // tu odwracamy: jeśli FacingRight==true → dir = -1, a false→+1
            bool facingRight = animator.GetBool(facingParam);
            float dir = facingRight ? -1f : 1f;

            transform.localScale = new Vector3(
                baseScale.x * dir,
                baseScale.y,
                baseScale.z
            );
        }
    }
}