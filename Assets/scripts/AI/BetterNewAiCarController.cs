using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class BetterNewAiCarController : MonoBehaviour
{
    
    // --- Constants ---
    private const float GROUND_RAY_LENGTH = 0.5f;
    private const float STEERING_DEAD_ZONE = 0.05f;
    private const float STEERING_LERP = 0.6f;
    private const float NODE_GIZMO_RADIUS = 0.5f;
    private static readonly Vector3 DEFAULT_CENTER_OF_MASS = new(0, -0.0f, 0);


        // --- Path Following ---
    [Header("Path Following Settings")]
    [Tooltip("Distance threshold for reaching a waypoint.")]
    [SerializeField] private float waypointThreshold = 10.0f;
    [Tooltip("Number of points to calculate for Bezier curves (higher = smoother).")]
    [SerializeField] private float bezierCurveResolution = 10f;
    [Tooltip("Angle threshold for switching between straight lines and curves.")]
    [SerializeField] private float angleThreshold = 35.0f;

    // --- Car Movement ---
    [Header("Car Movement Settings")]
    [Tooltip("Maximum acceleration applied to the car.")]
    [SerializeField] private float maxAcceleration = 300.0f;
    [Tooltip("Braking acceleration.")]
    [SerializeField] private float maxSpeed = 100.0f;

    // --- Steering ---
    [Header("Steering Settings")]
    [Tooltip("Left turn radius (how far the front left wheel can rotate).")]
    [SerializeField] private float leftTurnRadius = 10.0f;
    [Tooltip("Right turn radius (how far the front right wheel can rotate).")]
    [SerializeField] private float rightTurnRadius = 30.0f;
    [Tooltip("Current turn sensitivity.")]
    [SerializeField] private float turnSensitivity = 30.0f;
    [SerializeField] private int lookAheadIndex = 5;

    // --- Physics ---
    [Header("Physics Settings")]
    [Tooltip("Multiplier for gravity force.")]
    [SerializeField] private float gravityMultiplier = 1.5f;
    [Tooltip("Speed multiplier when on grass.")]
    [SerializeField] private float grassSpeedMultiplier = 0.5f;

    // --- Corner Slowdown ---
    [Header("AI Turn Slowdown Settings")]
    [Tooltip("Degrees: Only slow down for turns sharper than this.")]
    [SerializeField] private float slowdownThreshold = 30f;
    [Tooltip("Degrees: Max slowdown at this angle or above.")]
    [SerializeField] private float maxSlowdownAngle = 90f;
    [Tooltip("Minimum speed factor at max angle (e.g. 0.35 = 35% of maxSpeed).")]
    [SerializeField] private float minSlowdown = 0.35f;

    // --- Turn Detection ---
    [Header("Turn Detection Settings")]
    [Tooltip("Radius of the detection sphere for upcoming turns.")]
    [SerializeField] private float detectionRadius = 7.0f;
    [Tooltip("Tolerance for deviation from the Bezier curve.")]
    [SerializeField] private float curveTolerance = 2.0f;

    // --- Avoidance ---
    [Header("Avoidance Settings")]
    [Tooltip("Extra buffer distance added to the safe radius for avoidance checks.")]
    [SerializeField] private float avoidanceBuffer = 2.0f;
    [Tooltip("How far to offset laterally when dodging another car.")]
    [SerializeField] private float avoidanceLateralOffset = 2.0f;

    // --- Boost ---
    [Header("Boost Settings")]
    [Tooltip("Multiplier applied to speed and acceleration when boosting.")]
    [SerializeField] private float boostMultiplier = 1.25f;
    private bool isBoosting = false;


    // --- References ---
    [Header("References")]
    [Tooltip("List of wheels used by the car.")]
    [SerializeField] private List<CarController.Wheel> wheels;
    [Tooltip("Rigidbody component of the car.")]
    [SerializeField] private Rigidbody carRb;
    [Tooltip("Reference to the player car.")]
    [SerializeField] private CarController playerCar;
    public AiCarManager aiCarManager;
    private Collider carCollider;
    public float CarWidth { get; private set; }
    public float CarLength { get; private set; }

    
    private float playerCarWidth;
    private float playerCarLength;
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private CarController.Wheel[] frontWheels = Array.Empty<CarController.Wheel>();
    private float targetTorque;
    private float moveInput = 0f;
    private LayerMask grassLayerMask;
    private float steerInput;
    public void Initialize(AiCarManager aiCarManager, Collider playerCollider)
    {
        this.aiCarManager = aiCarManager;
        playerCar = playerCollider.GetComponent<CarController>();
        Collider pc = playerCollider.GetComponent<Collider>();
        playerCarWidth = pc.bounds.size.x;
        playerCarLength = pc.bounds.size.z;
    }
    private void Awake()
    {
        grassLayerMask = LayerMask.NameToLayer("Grass");

        if (carRb == null) carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = DEFAULT_CENTER_OF_MASS;

        carCollider = GetComponent<Collider>();
        if (carCollider != null)
        {
            CarWidth = carCollider.bounds.size.x;
            CarLength = carCollider.bounds.size.z;
        }

        frontWheels = wheels.Where(w => w.axel == CarController.Axel.Front).ToArray();
    }

    private void FixedUpdate()
    {
        // Airborne?
        if (Physics.Raycast(transform.position, Vector3.down, GROUND_RAY_LENGTH))
        {
            // Apply gravity
            carRb.AddForce(gravityMultiplier * Physics.gravity.magnitude * Vector3.down, ForceMode.Acceleration);
        }

        // Set new waypoint if close enough to current
        if (Vector3.Distance(transform.position, aiCarManager.BezierPoints[currentWaypointIndex]) < waypointThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % aiCarManager.BezierPoints.Count;
        }

        float steerAngle = Vector3.Angle(
                    transform.forward, 
                    aiCarManager.BezierPoints[currentWaypointIndex] - transform.position
        );
        
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(aiCarManager.BezierPoints[currentWaypointIndex] - transform.position),
            STEERING_LERP * Time.fixedDeltaTime
        );

        foreach (CarController.Wheel wheel in frontWheels)
        {
            wheel.wheelCollider.steerAngle = Mathf.Lerp(
                wheel.wheelCollider.steerAngle, 
                steerAngle * Mathf.Sign(Vector3.Cross(transform.forward, aiCarManager.BezierPoints[currentWaypointIndex] - transform.position).y),
                STEERING_LERP * Time.fixedDeltaTime
            );
        }

        ApplyDriveInputs();
    }

    private void ApplyDriveInputs()
    {
        moveInput = 1.0f;
        targetTorque = moveInput * maxAcceleration;

        if (Mathf.Abs(steerInput) > 0.5f)
        {
            targetTorque *= 0.5f;
        }

        if (IsOnGrass())
        {
            targetTorque *= grassSpeedMultiplier;
        }

        // Apply boost if active
        float speedLimit = maxSpeed;
        if (isBoosting)
        {
            speedLimit = (maxSpeed * boostMultiplier) + 20f; // Add flat +20
            targetTorque *= boostMultiplier;
        }

        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = targetTorque;
            wheel.wheelCollider.brakeTorque = 0f;
        }

        ApplySpeedLimit(speedLimit);
    }

    private void ApplySpeedLimit(float targetSpeed)
    {
        float currentSpeed = carRb.linearVelocity.magnitude * 3.6f; // Convert to km/h
        if (currentSpeed > targetSpeed)
        {
            carRb.linearVelocity = carRb.linearVelocity.normalized * (targetSpeed / 3.6f);
        }
    }

    private bool IsOnGrass()
    {
        if (wheels == null) return false;
        foreach (var wheel in wheels)
        {
            if (Physics.Raycast(wheel.wheelCollider.transform.position, -wheel.wheelCollider.transform.up, out RaycastHit hit, wheel.wheelCollider.radius + wheel.wheelCollider.suspensionDistance))
            {
                if (hit.collider.gameObject.layer == grassLayerMask)
                    return true;
            }
        }
        return false;
    }
}