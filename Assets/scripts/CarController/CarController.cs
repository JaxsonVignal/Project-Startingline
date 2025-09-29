using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public enum Axel 
    {
        Front,
        Rear    
    }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    public float maxAcceleration = 30.0f;
    public float brakeAcceleration = 50.0f;

    public float turnSensitivity = 1.0f;
    public float maxSteerAngle = 30.0f;

    public Vector3 _centerOfMass;

    public List<Wheel> wheels;

    float moveInput;
    float steerInput;

    private Rigidbody carRB;


    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
        carRB.centerOfMass = _centerOfMass;
    }

    void Update()
    {
        GetInputs();
        Animationwheels();
    }

    void LateUpdate()
    {
        Move();
        Steer();
        Brake();
    }

    void GetInputs()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }

    
    void Move() 
    {
     foreach(var wheel in wheels)
     {
      wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * Time.deltaTime;
     }    
    }

    void Steer() 
    { 
      foreach(var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
               var _steerAngle = steerInput * turnSensitivity * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.6f);
            }
        }
    }

    void Brake()
    {
     if (Input.GetKey(KeyCode.Space))
     {
       foreach (var wheel in wheels)
       {
         wheel.wheelCollider.brakeTorque = 300 * brakeAcceleration * Time.deltaTime;
       }
     }
      else
      {
       foreach (var wheel in wheels)
       {
                wheel.wheelCollider.brakeTorque = 0;
       }
      }


    }


    void Animationwheels()
    {
        foreach (var wheel in wheels)
        {
            Quaternion rot;
            Vector3 pos;
            wheel.wheelCollider.GetWorldPose(out pos, out rot);
            wheel.wheelModel.transform.position = pos;
            wheel.wheelModel.transform.rotation = rot;
        }
    }

}

