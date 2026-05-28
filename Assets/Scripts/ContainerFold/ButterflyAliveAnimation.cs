using System.Collections;
using UnityEngine;

/// <summary>
/// Lerps wing transforms between two authored reference poses in a ping-pong loop.
/// Call <see cref="Play"/> to start and <see cref="Stop"/> to halt at the current position.
/// </summary>
public class ButterflyAliveAnimation : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Tooltip("Seconds for one full flap cycle (Pose A → Pose B → Pose A).")]
    public float cycleDuration = 1.5f;

    [Tooltip("Easing applied to the normalised ping-pong t value. Defaults to ease-in-out.")]
    public AnimationCurve flapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Reference pose A — right wing local rotation source.")]
    [SerializeField] private Transform poseARight;

    [Tooltip("Reference pose A — left wing local rotation source.")]
    [SerializeField] private Transform poseALeft;

    [Tooltip("Reference pose B — right wing local rotation source.")]
    [SerializeField] private Transform poseBRight;

    [Tooltip("Reference pose B — left wing local rotation source.")]
    [SerializeField] private Transform poseBLeft;

    [Tooltip("The right wing transform to drive.")]
    [SerializeField] private Transform targetRight;

    [Tooltip("The left wing transform to drive.")]
    [SerializeField] private Transform targetLeft;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (poseARight  == null) Debug.LogWarning("[ButterflyAliveAnimation] poseARight is not assigned.");
        if (poseALeft   == null) Debug.LogWarning("[ButterflyAliveAnimation] poseALeft is not assigned.");
        if (poseBRight  == null) Debug.LogWarning("[ButterflyAliveAnimation] poseBRight is not assigned.");
        if (poseBLeft   == null) Debug.LogWarning("[ButterflyAliveAnimation] poseBLeft is not assigned.");
        if (targetRight == null) Debug.LogWarning("[ButterflyAliveAnimation] targetRight is not assigned.");
        if (targetLeft  == null) Debug.LogWarning("[ButterflyAliveAnimation] targetLeft is not assigned.");
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Starts the ping-pong flap loop. Safe to call multiple times; stops any existing coroutine first.</summary>
    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(WingFlapLoop());
    }

    /// <summary>Stops the flap loop, leaving the wings at their current rotation.</summary>
    public void Stop()
    {
        StopAllCoroutines();
    }

    // -------------------------------------------------------------------------
    // Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator WingFlapLoop()
    {
        float halfCycle = Mathf.Max(cycleDuration * 0.5f, 0.01f);

        while (true)
        {
            float rawT    = Mathf.PingPong(Time.time / halfCycle, 1f);
            float curvedT = flapCurve.Evaluate(rawT);

            if (targetRight != null && poseARight != null && poseBRight != null)
                targetRight.localRotation = Quaternion.Lerp(
                    poseARight.localRotation, poseBRight.localRotation, curvedT);

            if (targetLeft != null && poseALeft != null && poseBLeft != null)
                targetLeft.localRotation = Quaternion.Lerp(
                    poseALeft.localRotation, poseBLeft.localRotation, curvedT);

            yield return null;
        }
    }
}
