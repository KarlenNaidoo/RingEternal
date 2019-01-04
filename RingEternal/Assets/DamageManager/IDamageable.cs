using UnityEngine;

public interface IDamageReceiver
{
    Transform transform { get; }
    GameObject gameObject { get; }
    void ReceiveDamage(Damage damage);
}

public interface IHealthController: IDamageReceiver
{
    float currentHealth { get; }
    bool isDead { get; set; }
    void ChangeHealth(int value);
    void ChangeMaxHealth(int value);
    void PlayHurtAnimation(bool value);
}

public interface IHitboxResponder
{

    void CollidedWith(Collider collider);

}