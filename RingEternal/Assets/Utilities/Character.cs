using UnityEngine;


/// <summary>
/// The base abstract class for all character controllers, provides common functionality.
/// </summary>
public abstract class Character : HealthController
{

    [Header("References")]
    [SerializeField] protected Collider _collider;


    protected Rigidbody _rigidbody;                                // access the Rigidbody component
    protected PhysicMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;       // create PhysicMaterial for the Rigidbody
                      // access CapsuleCollider information
    protected float colliderRadius, colliderHeight;        // storage capsule collider extra information        ]
    protected Vector3 colliderCenter;                      // storage the center of the capsule collider info
    protected IBlackboard blackboard;
    



    [Header("Base Parameters")]

    [Tooltip("If specified, will use the direction from the character to this Transform as the gravity vector instead of Physics.gravity. Physics.gravity.magnitude will be used as the magnitude of the gravity vector.")]
    public Transform gravityTarget;

    [Tooltip("Multiplies gravity applied to the character even if 'Individual Gravity' is unchecked.")]
    [SerializeField] protected float gravityMultiplier = 2f; // gravity modifier - often higher than natural gravity feels right for game characters

    [SerializeField] protected float airborneThreshold = 0.6f; // Height from ground after which the character is considered airborne
    [SerializeField] float slopeStartAngle = 50f; // The start angle of velocity dampering on slopes
    [SerializeField] float slopeEndAngle = 85f; // The end angle of velocity dampering on slopes
    [SerializeField] float spherecastRadius = 0.1f; // The radius of sperecasting
    [SerializeField] LayerMask groundLayers; // The walkable layers

    private PhysicMaterial zeroFrictionMaterial;
    private PhysicMaterial highFrictionMaterial;
    protected const float half = 0.5f;
    protected float originalHeight;
    protected Vector3 originalCenter;



    protected virtual void Awake()
    {
        blackboard = GetComponent<IBlackboard>();

    }

    protected override void Start()
    {
        base.Start();
        _rigidbody = blackboard.primaryRigidbody;


        //// Store the collider volume
        //originalHeight = collider.height;
        //originalCenter = collider.center;

        // Physics materials
        zeroFrictionMaterial = new PhysicMaterial();
        zeroFrictionMaterial.dynamicFriction = 0f;
        zeroFrictionMaterial.staticFriction = 0f;
        zeroFrictionMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        zeroFrictionMaterial.bounciness = 0f;
        zeroFrictionMaterial.bounceCombine = PhysicMaterialCombine.Minimum;

        highFrictionMaterial = new PhysicMaterial();

        // Making sure rigidbody rotation is fixed
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    protected Vector3 GetGravity()
    {
        if (gravityTarget != null)
        {
            return (gravityTarget.position - transform.position).normalized * Physics.gravity.magnitude;
        }

        return Physics.gravity;
    }

    // Spherecast from the root to find ground height
    protected virtual RaycastHit GetSpherecastHit()
    {
        Vector3 up = transform.up;
        Ray ray = new Ray(_rigidbody.position + up * airborneThreshold, -up);
        RaycastHit h = new RaycastHit();
        h.point = transform.position - transform.transform.up * airborneThreshold;
        h.normal = transform.up;

        Physics.SphereCast(ray, spherecastRadius, out h, airborneThreshold * 2f, groundLayers);
        
        return h;
    }

    // Gets angle around y axis from a world space direction
    public float GetAngleFromForward(Vector3 worldDirection, bool localtransform = false)
    {
        if (localtransform)
        {
            Vector3 local = transform.InverseTransformDirection(worldDirection);
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }
        return Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
    }

    // Rotate a rigidbody around a point and axis by angle
    protected void RigidbodyRotateAround(Vector3 point, Vector3 axis, float angle)
    {
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);
        Vector3 d = transform.position - point;
        _rigidbody.MovePosition(point + rotation * d);
        _rigidbody.MoveRotation(rotation * transform.rotation);
    }

    //// Scale the capsule collider to 'mlp' of the initial value
    //protected void ScaleCapsule(float mlp)
    //{
    //    foreach(Collider _collider in _colliders)
    //    {
    //        originalHeight = _collider.h;
    //        originalCenter = _collider.center;
    //    }
    //    if (collider.height != originalHeight * mlp)
    //    {
    //        collider.height = Mathf.MoveTowards(collider.height, originalHeight * mlp, Time.deltaTime * 4);
    //        collider.center = Vector3.MoveTowards(collider.center, originalCenter * mlp, Time.deltaTime * 2);
    //    }
    //}

    // Set the collider to high friction material
    protected void HighFriction()
    {
            _collider.material = highFrictionMaterial;

    }

    // Set the collider to zero friction material
    protected void ZeroFriction()
    {
            _collider.material = zeroFrictionMaterial;
        
        
    }

    // Get the damper of velocity on the slopes
    protected float GetSlopeDamper(Vector3 velocity, Vector3 groundNormal)
    {
        float angle = 90f - Vector3.Angle(velocity, groundNormal);
        angle -= slopeStartAngle;
        float range = slopeEndAngle - slopeStartAngle;
        return 1f - Mathf.Clamp(angle / range, 0f, 1f);
    }


}
