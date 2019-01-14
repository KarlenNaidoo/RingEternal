using UnityEngine;
using System.Collections;

namespace RingEternal.MyCharacter
{

    public class HealthController : MonoBehaviour, IHealthController
    {
        [SerializeField] protected float _currentHealth;
        [SerializeField] protected bool _isDead;
        public int maxHealth = 100;
        public float healthRecovery = 0f;
        public float healthRecoveryDelay = 0f;
        [HideInInspector]
        public float currentHealthRecoveryDelay;
        // Declare two delegates: OnReciveDamage and OnDead
        public delegate void OnReceiveDamage();
        public delegate void OnDead();
        public static event OnReceiveDamage onReceiveDamage;
        public static event OnDead onDead;
        bool inHealthRecovery;

        public float currentHealth
        {
            get
            {
                return _currentHealth;
            }

            protected set
            {
                _currentHealth = value;
                if (!_isDead && _currentHealth <= 0)
                {
                    _isDead = true;
                    Debug.Log("Is Dead");
                    //onDead();
                }
            }
        }

        public bool isDead
        {
            get
            {
                if (!_isDead && currentHealth <= 0)
                {
                    _isDead = true;
                    Debug.Log("Is Dead");
                    //onDead();
                }
                return _isDead;
            }

            set
            {
                _isDead = value;
            }
        }

        protected virtual void Start()
        {
            currentHealth = maxHealth;
            currentHealthRecoveryDelay = healthRecoveryDelay;
        }

        protected virtual bool canRecoverHealth
        {
            get
            {
                return !(currentHealth <= 0 || (healthRecovery == 0 && currentHealth < maxHealth));
            }
        }

        protected virtual IEnumerator RecoverHealthOverTime()
        {
            inHealthRecovery = true;
            while (canRecoverHealth)
            {
                HealthRecovery();
                yield return null;
            }

            inHealthRecovery = false;
        }

        protected virtual void HealthRecovery()
        {
            if (!canRecoverHealth) return;
            if (currentHealthRecoveryDelay > 0)
            {
                currentHealthRecoveryDelay -= Time.deltaTime;
            }
            else
            {
                if (currentHealth > maxHealth)
                {
                    currentHealth = maxHealth;
                }
                if (currentHealth < maxHealth)
                {
                    currentHealth += healthRecovery * Time.deltaTime;
                }
            }
        }

        public virtual void ChangeHealth(int value)
        {
            currentHealth += value;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            if (!isDead && currentHealth <= 0)
            {
                isDead = true;
                //onDead();
            }
        }

        public virtual void ChangeMaxHealth(int value)
        {
            maxHealth += value;
            if (maxHealth < 0)
            {
                maxHealth = 0;
            }
        }

        public virtual void ReceiveDamage(Damage damage)
        {
            Debug.Log("Receiving Damage");
            if (damage != null)
            {
                currentHealthRecoveryDelay = currentHealth <= 0 ? 0 : healthRecoveryDelay;
                if (damage.damageValue > 0 && !inHealthRecovery)
                {
                    StartCoroutine(RecoverHealthOverTime());
                }
                if (currentHealth > 0)
                {
                    currentHealth -= damage.damageValue;

                    //Debug.Log("Current Health: " + currentHealth);
                }
            }
            if (damage.hitReaction)
            {
                PlayHurtAnimation(true);
            }
        }

        public virtual void PlayHurtAnimation(bool value)
        {
            Debug.Log("Play hurt animation. Override in child classes");
        }
    }


    [System.Serializable]
    public class Damage
    {
        public int damageValue;
        public bool ignoreDefense;
        public Vector3 hitPosition;
        public bool hitReaction = true;

        public Damage(int value)
        {
            this.damageValue = value;
            this.hitReaction = true;
        }

        public Damage(Damage damage)
        {

            this.damageValue = damage.damageValue;
            this.ignoreDefense = damage.ignoreDefense;
            this.hitPosition = damage.hitPosition;
        }

    }
}