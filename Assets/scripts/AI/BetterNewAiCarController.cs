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
    [Tooltip("The parent object containing all path waypoints as children.")]
    public Transform path;
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

    // --- References ---
    [Header("References")]
    [Tooltip("List of wheels used by the car.")]
    [SerializeField] private List<CarController.Wheel> wheels;
    [Tooltip("Rigidbody component of the car.")]
    [SerializeField] private Rigidbody carRb;
    [Tooltip("Reference to the player car.")]
    [SerializeField] private CarController playerCar;

    private Collider carCollider;
    private float carWidth;
    private float carLength;
    private float playerCarWidth;
    private float playerCarLength;
    private Transform[] waypoints;
    public static HashSet<BetterNewAiCarController> allAiCars;

    /*private void OnEnable() => allAiCars.Add(this);
    private void OnDisable() => allAiCars.Remove(this);*/

    private void Start()
    {
        int startTime = DateTime.Now.Millisecond;

        for (int i = 0; i < 1000000; i++)
        {
            BezierMath.CalculateBezierPoint(new Vector2[] {
                new Vector2(UnityEngine.Random.Range(-100f, 100f),UnityEngine.Random.Range(-100f, 100f)),
                new Vector2(UnityEngine.Random.Range(-100f, 100f),UnityEngine.Random.Range(-100f, 100f)),
                new Vector2(UnityEngine.Random.Range(-100f, 100f),UnityEngine.Random.Range(-100f, 100f))
                }, 
                UnityEngine.Random.Range(0, 1f)
            );
        }


        Debug.Log("Bezier Calculation Time: " + (DateTime.Now.Millisecond - startTime) + " ms");

        /*if (path == null)
        {
            Debug.LogError("Path transform is not assigned.");
            enabled = false;
            return;
        }

        waypoints = path.GetComponentsInChildren<Transform>();
        if (waypoints == null)
        {
            Debug.Log("Waypoints is empty!");
            enabled = false;
        }

        if (waypoints.Contains(path)) Debug.Log("waypoints contains path");

        if (carRb == null) carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = DEFAULT_CENTER_OF_MASS;

        carCollider = GetComponent<Collider>();
        if (carCollider != null)
        {
            carWidth = carCollider.bounds.size.x;
            carLength = carCollider.bounds.size.z;
        }
        
        GameManager gm = GameManager.instance;
        if (
            playerCar == null 
            && gm != null 
            && gm.currentCar != null
            )
        {
            playerCar = gm.currentCar.GetComponent<CarController>();
            Collider playerCollider = gm.currentCar.GetComponent<Collider>();
            
            playerCarWidth = playerCollider.bounds.size.x;
            playerCarLength = playerCollider.bounds.size.z;
            
        }*/
    }

    /*private void FixedUpdate()
    {
        // Airborne?
        if (Physics.Raycast(transform.position, Vector3.down, GROUND_RAY_LENGTH))
        {
            // Apply gravity
            carRb.AddForce(gravityMultiplier * Physics.gravity.magnitude * Vector3.down, ForceMode.Acceleration);
        }
    }*/
}