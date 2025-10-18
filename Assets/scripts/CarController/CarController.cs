using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public enum ControlMode { Keyboard, Buttons }
    public enum Axel { Front, Rear }
    public enum CarState { Forward, Reverse, Neutral }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public GameObject wheelEffectObj;
        public ParticleSystem smokeParticle;
        public Axel axel;
    }

    [Header("Controls")]
    public ControlMode control = ControlMode.Keyboard;

    [Header("Car Settings")]
    public float maxSpeed = 70f;
    public float acceleration = 3500f;
    public float brakePower = 4000f;
    public float maxSteerAngle = 35f;
    public float reverseSteerMultiplier = 1.5f;
    public float steeringSpeed = 6f;
    public float deceleration = 0.98f;
    public Vector3 _centerOfMass;

    public List<Wheel> wheels;

    [Header("Handling & Drift")]
    [Range(0f, 1f)] public float driftFactor = 0.90f;
    [Range(0f, 1f)] public float angularDragFactor = 0.25f;
    public float speedSteerReduction = 0.025f;
    public float driftTriggerTime = 0.5f;
    public float counterSteerFactor = 0.6f;   // Strength of countersteering
    public float slipAngleThreshold = 10f;    // Slip angle in degrees before countersteering starts

    private float moveInput;
    private float steerInput;
    private Rigidbody carRb;
    private CarLights carLights;

    private float turnTimer = 0f;
    private bool isDrifting = false;
    private CarState currentState = CarState.Neutral;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;
        carRb.mass = 1200f;
        carRb.drag = 0.03f;
        carRb.angularDrag = 0.5f;

        carLights = GetComponent<CarLights>();
    }

    void Update()
    {
        GetInputs();
        AnimateWheels();
        WheelEffects();

        if (Input.GetKeyDown(KeyCode.L) && carLights != null)
        {
            carLights.isFrontLightOn = !carLights.isFrontLightOn;
            carLights.OperateFrontLights();

            carLights.isBackLightOn = carLights.isFrontLightOn;
            carLights.OperateBackLights();
        }
    }

    void FixedUpdate()
    {
        ApplyDrive();
        ApplySteering();
        ApplyBrakes();
        ApplyDeceleration();
        SimulateInertia();
    }

    void GetInputs()
    {
        if (control == ControlMode.Keyboard)
        {
            moveInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
        }
    }

    void ApplyDrive()
    {
        float forwardSpeed = Vector3.Dot(carRb.velocity, transform.forward);

        // Determine car state
        if (moveInput > 0)
            currentState = CarState.Forward;
        else if (moveInput < 0)
            currentState = CarState.Reverse;
        else if (Mathf.Abs(forwardSpeed) < 0.1f)
            currentState = CarState.Neutral;

        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Rear)
            {
                wheel.wheelCollider.brakeTorque = 0f;

                switch (currentState)
                {
                    case CarState.Forward:
                    case CarState.Reverse:
                        wheel.wheelCollider.motorTorque = moveInput * acceleration;
                        break;
                    case CarState.Neutral:
                        wheel.wheelCollider.motorTorque = 0f;
                        break;
                }

                // Braking when pressing opposite direction
                if ((forwardSpeed > 0.5f && moveInput < 0) ||
                    (forwardSpeed < -0.5f && moveInput > 0))
                {
                    wheel.wheelCollider.motorTorque = 0f;
                    wheel.wheelCollider.brakeTorque = brakePower;
                }
            }
        }
    }

    void ApplySteering()
    {
        float speedFactor = Mathf.Clamp01(carRb.velocity.magnitude * speedSteerReduction);
        float steerAngle = steerInput * maxSteerAngle * (1 - speedFactor);

        // Invert steering in reverse
        if (currentState == CarState.Reverse)
            steerAngle *= -reverseSteerMultiplier;

        // Calculate slip angle for countersteering
        Vector3 localVelocity = transform.InverseTransformDirection(carRb.velocity);
        float slipAngle = Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.z)) * Mathf.Rad2Deg;

        // Detect drift based on slip
        if (Mathf.Abs(steerInput) > 0.1f && carRb.velocity.magnitude > 5f)
        {
            turnTimer += Time.fixedDeltaTime;
            if (turnTimer > driftTriggerTime)
                isDrifting = Mathf.Abs(slipAngle) > slipAngleThreshold;
        }
        else
        {
            turnTimer = 0f;
            isDrifting = false;
        }

        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                float finalSteer = Mathf.Lerp(
                    wheel.wheelCollider.steerAngle,
                    steerAngle,
                    Time.fixedDeltaTime * steeringSpeed
                );

                // Apply countersteering only when drifting
                if (isDrifting)
                {
                    float counterSteer = -Mathf.Sign(slipAngle) * Mathf.Min(Mathf.Abs(slipAngle) / 30f, 1f) * counterSteerFactor * maxSteerAngle;
                    finalSteer += counterSteer;
                }

                wheel.wheelCollider.steerAngle = finalSteer;
            }
        }
    }

    void ApplyBrakes()
    {
        bool isHandBrake = Input.GetKey(KeyCode.Space);

        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Rear)
                wheel.wheelCollider.brakeTorque = isHandBrake ? brakePower : 0f;
        }

        if (carLights != null)
        {
            carLights.isBackLightOn = isHandBrake || carLights.isFrontLightOn;
            carLights.OperateBackLights();
        }
    }

    void ApplyDeceleration()
    {
        float forwardSpeed = Vector3.Dot(carRb.velocity, transform.forward);

        if (Mathf.Abs(moveInput) < 0.1f ||
            (forwardSpeed > 0.1f && moveInput < 0) ||
            (forwardSpeed < -0.1f && moveInput > 0))
        {
            Vector3 localVel = transform.InverseTransformDirection(carRb.velocity);
            localVel.z *= deceleration;
            carRb.velocity = transform.TransformDirection(localVel);
        }
    }

    void AnimateWheels()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.wheelModel.transform.SetPositionAndRotation(pos, rot);
        }
    }

    void WheelEffects()
    {
        foreach (var wheel in wheels)
        {
            if (Input.GetKey(KeyCode.Space) &&
                wheel.axel == Axel.Rear &&
                wheel.wheelCollider.isGrounded &&
                carRb.velocity.magnitude >= 10f)
            {
                wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = true;
                wheel.smokeParticle.Emit(1);
            }
            else
            {
                wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = false;
            }
        }
    }

    void SimulateInertia()
    {
        Vector3 localVel = transform.InverseTransformDirection(carRb.velocity);

        if (!isDrifting)
            localVel.x *= driftFactor;

        carRb.velocity = transform.TransformDirection(localVel);
        carRb.angularVelocity *= (1 - Time.fixedDeltaTime * angularDragFactor);
    }
}
