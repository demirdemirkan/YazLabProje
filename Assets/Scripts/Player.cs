using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player: MonoBehaviour
{
    float speed = 2;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            transform.position = transform.position + Vector3.forward * speed * Time.deltaTime;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            transform.position = transform.position + Vector3.back * speed * Time.deltaTime;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            transform.position = transform.position + Vector3.left * speed * Time.deltaTime;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            transform.position = transform.position + Vector3.right * speed * Time.deltaTime;
        }
    }
}