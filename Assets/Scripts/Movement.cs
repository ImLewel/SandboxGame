using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
public class Movement : MonoBehaviour {
  private Vector3 PlayerMovementInput;
  private Vector2 PlayerMouseInput;
  private Vector3 MoveVector;

  private Camera main;
  private GameObject player;
  private CapsuleCollider playerCollider;
  private Rigidbody rb;
  private Transform body;
  [SerializeField] private Transform rArm;
  private Slider stamina;

  private float origPlayerHeight;
  [SerializeField] private float speed = 5f;
  [SerializeField] private float sprintSpeed = 7f;
  [SerializeField] private float currentSpeed = 5f;
  [SerializeField] private float jumpForce = 10f;
  [SerializeField] private float sensitivity = 1f;
  [SerializeField] private float crouchOffset = 0.5f;

  private bool canJump;
  private bool crouching;
  private bool grounded;

  private Coroutine cor;

  private float rotX;
  private float rotY;

  Dictionary<Transform, Vector3> originalChildPositions = new();
  void Start() {
    Cursor.lockState = CursorLockMode.Locked;
    player = transform.gameObject;
    rb = player.GetComponent<Rigidbody>();
    playerCollider = player.GetComponent<CapsuleCollider>();
    origPlayerHeight = playerCollider.height;
    main = Camera.main;
    body = transform.Find("Body");
    stamina = GameObject.Find("HUD").GetComponent<UImanager>().progressBar;
  }

  void Update() {
    PlayerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
    PlayerMouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    grounded = isGrounded(player, playerCollider);
    Move();
  }

  void MoveCamera() {
    rotX -= PlayerMouseInput.y * sensitivity * Time.deltaTime;
    rotX = Mathf.Clamp(rotX, -90f, 90f);

    rotY = PlayerMouseInput.x * sensitivity * Time.deltaTime;

    player.transform.Rotate(0f, rotY, 0f);
    main.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
    rArm.transform.localRotation = main.transform.localRotation;
  }

  private void Move() {
    MoveVector = transform.TransformDirection(PlayerMovementInput) * currentSpeed;
    rb.velocity = new Vector3(MoveVector.x, rb.velocity.y, MoveVector.z);

    if (Input.GetKey(KeyCode.LeftShift) && grounded && !crouching && stamina.value > stamina.minValue && PlayerMovementInput != Vector3.zero) {
      currentSpeed = sprintSpeed;
      if (cor != null)
        StopCoroutine(cor);
      cor = StartCoroutine(StaminaDelay(0.1f, 0f));
    }
    else {
      currentSpeed = speed;
      if (cor != null)
        StopCoroutine(cor);
      cor = StartCoroutine(StaminaDelay(0.15f, 1f));
    }

    if (Input.GetKeyDown(KeyCode.Space) && grounded && stamina.value >= 0.25f) 
      canJump = true;

    if (Input.GetKey(KeyCode.LeftControl)) crouching = true;

    if (crouching) CrouchState(true, crouchOffset);

    if (!Input.GetKey(KeyCode.LeftControl) && crouching)
      if (!CapsuleChecker()) crouching = false;

    if (!crouching) CrouchState(false, crouchOffset);
  }

  IEnumerator StaminaDelay(float rate, float barrier) {
    while (true) {
      if (grounded) {
        if (stamina.value > barrier) {
          stamina.value -= rate * Time.deltaTime;
        }
        else if (stamina.value < barrier) {
          stamina.value += rate * Time.deltaTime;
        }
      }
      yield return new WaitForSeconds(0.1f);
    }
  }

  bool CapsuleChecker() {
    float difference = origPlayerHeight - playerCollider.height;
    float radiusOfOne = difference / 4f; // capsule => 2 sphere => 2 diameters => 4 radius from previous

    Vector3 centerOne = transform.position + Vector3.up * 2f * radiusOfOne;
    //0.1f reduces height a little because player can stand at 2f but CheckCapsule can collide ceil at 2f too
    Vector3 centerTwo = transform.position + (Vector3.up * (radiusOfOne - 0.1f)) + (Vector3.up * difference);

    return Physics.CheckCapsule(centerOne, centerTwo, radiusOfOne, ~LayerMask.GetMask("PlayerLayer"));
  }

  bool isGrounded(GameObject obj, Collider collider) =>
    Physics.Raycast(obj.transform.position, -obj.transform.up, collider.bounds.extents.y + 0.1f);
  //0.1f extends raycast a little further to check outer surface

  void Jump() {
    if (cor != null) StopCoroutine(cor);
    rb.AddForce(0, jumpForce, 0, ForceMode.Impulse);
    stamina.value -= 0.25f;
  }

  void CrouchState(bool state, float _offset) {
    Vector3 newPosition;

    foreach (Transform child in transform) {
      if (!originalChildPositions.ContainsKey(child)) 
        originalChildPositions.Add(child, child.localPosition);

      if (state) {
        if (child == body && child.localScale.y != _offset) {
          child.localScale = new Vector3(child.localScale.x, child.localScale.y * _offset, child.localScale.z);
          playerCollider.height *= _offset;
        }
        newPosition = new Vector3(
            originalChildPositions[child].x,
            originalChildPositions[child].y * _offset,
            originalChildPositions[child].z
          );
      }
      else {
        if (child == body && playerCollider.height != origPlayerHeight) {
          child.localScale = new Vector3(child.localScale.x, child.localScale.y * (1f / _offset), child.localScale.z);
          playerCollider.height *= (1f / _offset);
        }
        newPosition = originalChildPositions[child];
      }
        
      child.localPosition = newPosition;
    }
  }

  private void FixedUpdate() {
    if (canJump) {
      Jump();
      canJump = false;
    }

  }

  private void LateUpdate() {
    MoveCamera();
  }
}
