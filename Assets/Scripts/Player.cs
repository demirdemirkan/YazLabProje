using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;

    Rigidbody rb;
    private bool isCharacterWalking;
    private bool isCharacterRunning;
    public Animator animator;
    Vector3 inputDir;
    float currentSpeed;

    // ADD
    [Header("Visual")]
    public Transform model;          // cowboy child'ını buraya sürükle
    public float modelRotateLerp = 12f; // dönüş yumuşatma hızı


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
        {
            inputDir += transform.right;
            // TriggerWalkAnimation();
        }
        // if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A)&&!Input.GetKey(KeyCode.D)) 
        //{
        //  TriggerIdleAnimation();
        //}
        bool moving = inputDir.magnitude >= 0.1f;
        bool sprint = moving && Input.GetKey(KeyCode.LeftShift);

        if (!moving)
        {
            TriggerIdleAnimation();
            isCharacterWalking = false;
            isCharacterRunning = false;
        }
        else if (sprint)
        {
            TriggerRunAnimation();        // YENİ
        }
        else
        {
            isCharacterRunning = false;   // yürüyüşe geçince koşu flag’i kapansın
            TriggerWalkAnimation();
        }

        inputDir = inputDir.normalized;
    }

    void FixedUpdate()
    {
        // Y bileşenini koru (yerçekimi için)
        Vector3 v = rb.velocity;

        // Hedef yatay hız
        Vector3 targetXZ = inputDir * currentSpeed;

        // Anlık hızda XZ’yi hedefe ayarla, Y’yi dokunma
        v.x = targetXZ.x;
        v.z = targetXZ.z;
        rb.velocity = v;

        // ADD: Görsel modeli hareket yönüne döndür (parent dönmez)
        if (model != null && inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            model.rotation = Quaternion.Slerp(model.rotation, targetRot, modelRotateLerp * Time.fixedDeltaTime);
        }
        // input yoksa mevcut yönünü korur (idle'da sabit)



    }
    void TriggerWalkAnimation()
    {
        if (!isCharacterWalking)
        {
            animator.SetTrigger("Walk");
            isCharacterWalking = true;
        }
    }
    void TriggerIdleAnimation()
    {
        if (isCharacterWalking || isCharacterRunning)
        {
            animator.ResetTrigger("Walk"); // öneri (çakışmayı engeller)
            animator.ResetTrigger("Run");  // öneri (koşu tetikliyse iptal et)
            animator.SetTrigger("Idle");
            isCharacterWalking = false;
            isCharacterRunning = false;
        }
    }
    void TriggerRunAnimation()
    {
        if (!isCharacterRunning)
        {
            animator.SetTrigger("Run");
            isCharacterRunning = true;
            isCharacterWalking = false; // koşuya geçince yürüyüş flag’i kapansın
        }
    }
}

