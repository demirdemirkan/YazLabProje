using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;

    Rigidbody rb;
    private bool isCharacterWalking;
    public Animator animator;
    Vector3 inputDir;
    float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        inputDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            inputDir += transform.forward;
            //TriggerWalkAnimation();
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputDir -= transform.forward;
            //TriggerWalkAnimation();
        }
        if (Input.GetKey(KeyCode.A))
        {
            inputDir -= transform.right;
            //TriggerWalkAnimation();
        }
        if (Input.GetKey(KeyCode.D)) 
        { inputDir += transform.right;
           // TriggerWalkAnimation();
        }
        // if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A)&&!Input.GetKey(KeyCode.D)) 
        //{
        //  TriggerIdleAnimation();
        //}
        if (inputDir.magnitude < .1f) {TriggerIdleAnimation();}
        else {TriggerWalkAnimation();}
        inputDir = inputDir.normalized;
    }

    void FixedUpdate()
    {
        // Y bileşenini koru (yerçekimi için)
        Vector3 v = rb.linearVelocity;

        // Hedef yatay hız
        Vector3 targetXZ = inputDir * currentSpeed;

        // Anlık hızda XZ’yi hedefe ayarla, Y’yi dokunma
        v.x = targetXZ.x;
        v.z = targetXZ.z;
        rb.linearVelocity = v;

        
    }
     void TriggerWalkAnimation() 
    {
        if(!isCharacterWalking)
        {
            animator.SetTrigger("Walk");
            isCharacterWalking = true;
        }
    }
    void TriggerIdleAnimation() 
    {
        if (isCharacterWalking) 
        {
            animator.SetTrigger("Idle");
            isCharacterWalking = false;
        }
    }
}
