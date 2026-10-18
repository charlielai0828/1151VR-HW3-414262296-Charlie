using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to the wheel GameObject. Add an AudioSource to the same object.
/// Call Spin() from a UI Button's OnClick event.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WheelSpinner : MonoBehaviour
{
    [Header("Spin Motion")]
    [Min(0f)] public float accelerationDuration = 1.2f;
    [Min(0f)] public float cruiseDuration = 1.0f;
    [Min(0.1f)] public float decelerationDuration = 4.0f;
    [Min(0f)] public float maxSpeed = 900f; // degrees per second
    [Min(0)] public int extraTurns = 4;
    public bool clockwise = true;

    [Header("Audio")]
    public AudioClip spinLoopClip;
    public AudioClip stopClip;
    [Range(0f, 1f)] public float spinVolume = 0.7f;
    [Range(0f, 1f)] public float stopVolume = 1f;

    public bool IsSpinning { get; private set; }

    private AudioSource audioSource;
    private Coroutine spinRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void Spin()
    {
        if (IsSpinning) return;

        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(SpinRoutine());
    }

    public void StopImmediately()
    {
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = null;
        IsSpinning = false;
        audioSource.Stop();
    }

    private IEnumerator SpinRoutine()
    {
        IsSpinning = true;
        float direction = clockwise ? -1f : 1f;
        float startZ = transform.eulerAngles.z;

        if (spinLoopClip != null)
        {
            audioSource.clip = spinLoopClip;
            audioSource.loop = true;
            audioSource.volume = spinVolume;
            audioSource.Play();
        }

        // Acceleration: ease from 0 to maxSpeed.
        float elapsed = 0f;
        while (elapsed < accelerationDuration)
        {
            elapsed += Time.deltaTime;
            float t = accelerationDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / accelerationDuration);
            float eased = t * t; // ease-in
            float speed = Mathf.Lerp(0f, maxSpeed, eased);
            transform.Rotate(0f, 0f, direction * speed * Time.deltaTime);
            yield return null;
        }

        // Cruise at maximum speed.
        elapsed = 0f;
        while (elapsed < cruiseDuration)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(0f, 0f, direction * maxSpeed * Time.deltaTime);
            yield return null;
        }

        // Decelerate smoothly. Choose a target angle with several extra turns.
        float currentZ = transform.eulerAngles.z;
        float remainingAngle = 360f * Mathf.Max(1, extraTurns);
        float targetZ = currentZ + direction * remainingAngle;
        float duration = Mathf.Max(0.1f, decelerationDuration);
        elapsed = 0f;
        float previousEase = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-out cubic: fast at first, gradually slows to zero.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float delta = eased - previousEase;
            float totalAngle = direction * remainingAngle;
            transform.Rotate(0f, 0f, totalAngle * delta);
            previousEase = eased;
            yield return null;
        }

        // Normalize angle to keep the transform tidy.
        transform.rotation = Quaternion.Euler(0f, 0f, targetZ % 360f);

        audioSource.Stop();
        audioSource.loop = false;
        if (stopClip != null)
            audioSource.PlayOneShot(stopClip, stopVolume);

        IsSpinning = false;
        spinRoutine = null;
    }
}
