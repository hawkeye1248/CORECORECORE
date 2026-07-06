using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Minimal life/death state for the parkour game. There is no HP to whittle down anymore: the
/// character is simply alive or dead. Hazards (see the Hazards folder) call
/// <see cref="KillCharacter"/> to kill it outright, and respawn calls <see cref="ResetCharacter"/>
/// to bring it back.
///
/// Trimmed down from the old combat <c>Health</c> component, keeping only the parts the hazards
/// and respawn actually use (death event, kill, reset, IsDead). Drop it on the same GameObject the
/// old Health lived on.
/// </summary>
public class SimpleHealth : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool isDead = false;

    /// <summary>Raised once when the character dies. Player respawn and the death screen listen to this.</summary>
    public event EventHandler OnDeath;

    [Tooltip("Fired on death for inspector-wired feedback (death SFX, VFX, ...). Optional.")]
    public UnityEvent death;

    /// <summary>
    /// Kills the character. Idempotent: calls while already dead do nothing, so hazards that fire
    /// every frame (LethalTrigger's OnTriggerStay) are safe to call this repeatedly.
    /// </summary>
    public void KillCharacter()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke(this, EventArgs.Empty);
        death?.Invoke();
    }

    /// <summary>Brings the character back to life. Called by respawn.</summary>
    public void ResetCharacter()
    {
        isDead = false;
    }

    public bool IsDead()
    {
        return isDead;
    }
}
