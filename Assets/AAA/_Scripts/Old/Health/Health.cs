using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private HealthData healthData;

    [Header("State")]
    [SerializeField] private float currentHealth;

    public void SetHealthData(HealthData data)
    {
        healthData = data;
    }

    public HealthData GetHealthData()
    {
        return healthData;
    }

    private float healthTimer;
    private bool isInvulnearable = false;
    private bool isDead = false;

    public event EventHandler OnHealthUpdated;
    public event EventHandler OnDeath;

    public UnityEvent damaged;
    public UnityEvent heal;
    public UnityEvent death;

    void Start()
    {
        currentHealth = healthData.maxHealth;
    }

    void Update()
    {
        if (healthData is PlayerHealthData pd && pd.doesDrainHealth)
        {
            DrainHealth(pd);
        }
    }

    public float GetHealthPercentage()
    {
        return currentHealth / healthData.maxHealth;
    }

    public void DamageHealth(float damage)
    {
        if (!isInvulnearable)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, healthData.maxHealth);
            damaged?.Invoke();
            OnHealthUpdated?.Invoke(this, EventArgs.Empty);
            if (currentHealth <= 0 && !isDead)
            {
                isDead = true;
                OnDeath?.Invoke(this, EventArgs.Empty);
                death?.Invoke();
                if (TryGetComponent<CharacterController>(out var cc))
                {
                    cc.enabled = false;
                }
            }
        }
    }

    public void HealHealth(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, healthData.maxHealth);
        heal?.Invoke();
        OnHealthUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void KillCharacter()
    {
        damaged?.Invoke();
        OnHealthUpdated?.Invoke(this, EventArgs.Empty);
        currentHealth = 0;
        isDead = true;
        OnDeath?.Invoke(this, EventArgs.Empty);
        death?.Invoke();
    }

    public void ResetCharacter()
    {
        isDead = false;
        currentHealth = healthData.maxHealth;
    }

    private void DrainHealth(PlayerHealthData pd)
    {
        healthTimer += Time.deltaTime;
        if (healthTimer >= pd.healthDrainTickTime)
        {
            DamageHealth(pd.healthDrainPerTick);
            healthTimer = 0;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }
}
