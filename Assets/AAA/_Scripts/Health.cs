using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth = 100;

    [Header("Health Drain")]
    
    [SerializeField] private bool doesDrainHealth = false;
    private float healthTimer;
    [SerializeField] private float healthDrainTickTime = 0.25f;
    [SerializeField] private float healthDrainPerTick;
    
    private bool isInvulnearable = false;
    private bool isDead = false;

    public event EventHandler OnPlayerHealthUpdated;
    public event EventHandler OnPlayerDeath;

    public UnityEvent damaged;
    public UnityEvent heal;
    public UnityEvent death;
    
    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (doesDrainHealth)
        {
            DrainHealth();   
        }
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public void DamageHealth(float damage)
    {
        if (!isInvulnearable)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            damaged?.Invoke();
            OnPlayerHealthUpdated?.Invoke(this, EventArgs.Empty);
            if (currentHealth <= 0)
            {
                isDead = true;
                OnPlayerDeath?.Invoke(this, EventArgs.Empty);
                death?.Invoke();
            }
        }
    }

    public void HealHealth(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        heal?.Invoke();
        OnPlayerHealthUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void KillCharacter()
    {
        damaged?.Invoke();
        OnPlayerHealthUpdated?.Invoke(this, EventArgs.Empty);
        currentHealth = 0;
        isDead = true;
        OnPlayerDeath?.Invoke(this, EventArgs.Empty);
        death?.Invoke();
    }

    public void ResetCharacter()
    {
        isDead = false;
        currentHealth = maxHealth;
    }

    private void DrainHealth()
    {
        healthTimer += Time.deltaTime;
        if (healthTimer >= healthDrainTickTime)
        {
            DamageHealth(healthDrainPerTick);
            healthTimer = 0;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }
}
