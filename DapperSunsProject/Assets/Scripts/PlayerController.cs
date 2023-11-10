using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour, IDamage
{
    [Header("----- Components -----")]
    [SerializeField] CharacterController controller;
    [SerializeField] Transform shootPos;
    [SerializeField] Transform headPos;

    [Header("----- Player Stats -----")]
    [Range(1, 20)][SerializeField] float playerSpeed;
    [Range(1, 50)][SerializeField] float jumpHeight;
    [Range(-10, 50)][SerializeField] float gravityValue;
    [Range(0, 1)][SerializeField] float friction;

    [Header("----- Dash Stats -----")]
    [SerializeField] float dashSpeed;
    [SerializeField] float dashDuration;

    [Header("----- Ground Pound Stats -----")]
    [SerializeField] float slamSpeed;

    [Header("----- Gun Stats -----")]
    [SerializeField] int boopDist;
    [SerializeField] int boopForce;
    [SerializeField] GameObject boopCard;

    [Header("----- Audio -----")]
    [SerializeField] AudioClip blastSFX;
    [SerializeField] AudioClip blastPenaltySFX;
    [SerializeField] AudioClip jumpSFX;
    [SerializeField] AudioClip dashSFX;
    [SerializeField] AudioClip slamSound;

    Transform startPos;
    int startHP;

    Vector3 move;
    Vector3 playerVelocity;
    bool groundedPlayer;
    bool canJump;
    bool canBeat = false;
    bool hitBeat = false;
    bool hitPenalty = false;
    bool slamming = false;
    int HP = 1;

    void Start()
    {
        GameManager.instance.OnRestartEvent += Restart;

        startPos = GameManager.instance.GetPlayerSpawn().transform;
        startHP = HP;

        GameManager.instance.playerDead = false;
        SpawnPlayer();
    }

    void Update()
    {
        if (!GameManager.instance.isPaused)
        {
            if (GameManager.instance.beatWindow)
            {
                canBeat = true;
            }
            else
            {
                if (canBeat)
                {
                    hitPenalty = false;
                }
                canBeat = false;
                hitBeat = false;
            }

            ShootInput();
            MovePlayer();
            DashInput();
            SlamInput(); 
        }
    }

    void Restart()
    {
        SpawnPlayer();
    }

    void MovePlayer()
    {
        float frictionForce;

        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
            canJump = true;
            frictionForce = friction / 2;
        }
        else
        {
            frictionForce = friction;
        }

        move = Input.GetAxis("Horizontal") * transform.right +
               Input.GetAxis("Vertical") * transform.forward;

        if (Input.GetButtonDown("Jump") && canJump)
        {
            if (!hitPenalty)
            {
                if (canBeat && !hitBeat)
                {
                    hitBeat = true;
                    AudioManager.instance.playOnce(jumpSFX);
                    playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                    canJump = false;
                }
                else
                {
                    hitPenalty = true;
                    BoopPenalty();
                }
            }
        }

        if (slamming)
        {
            // If we're ground pounding, we only want to apply the downward velocity
            playerVelocity.x = 0;
            playerVelocity.z = 0;
            move = Vector3.zero;
        }

        CheckHeadHit();

        playerVelocity.y += gravityValue * Time.deltaTime;

        playerVelocity.x *= frictionForce;
        playerVelocity.z *= frictionForce;

        controller.Move((move * playerSpeed + playerVelocity) * Time.deltaTime);

        if (!groundedPlayer)
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
        }

        controller.Move(playerVelocity * Time.deltaTime);
    }

    void CheckHeadHit()
    {
        RaycastHit hit;
        if (Physics.Raycast(headPos.transform.position, Vector3.up, out hit, 0.2f))
        {
            playerVelocity.y = -playerVelocity.y;
        }
    }

    void ShootInput()
    {
        if (Input.GetButtonDown("Shoot"))
        {
            if (!hitPenalty)
            {
                if (canBeat && !hitBeat)
                {
                    hitBeat = true;
                    Boop();
                }
                else
                {
                    hitPenalty = true;
                    BoopPenalty();
                }
            }
        }
    }

    void Boop()
    {
        AudioManager.instance.playOnce(blastSFX);
        canJump = false;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f)), out hit, boopDist))
        {
            Vector3 launchDirection = hit.normal;
            playerVelocity.x += launchDirection.x * boopForce * 2;
            playerVelocity.y += launchDirection.y * boopForce;
            playerVelocity.z += launchDirection.z * boopForce * 2;

            if (!hit.collider.isTrigger)
            {
                IBoop boopable = hit.collider.GetComponent<IBoop>();

                if (hit.transform != transform && boopable != null)
                {
                    boopable.DoBoop(boopForce);
                }
            }
        }

        Instantiate(boopCard, shootPos.position, shootPos.rotation);
    }

    void BoopPenalty()
    {
        AudioManager.instance.playOnce(blastPenaltySFX);
    }

    public void takeDamage(int amount)
    {
        HP -= amount;

        if (HP <= 0)
        {
            GameManager.instance.playerDead = true;
            GameManager.instance.PopupLose();
        }
    }

    public void SpawnPlayer()
    {
        controller.enabled = false;

        transform.position = startPos.position;
        transform.rotation = startPos.rotation;

        controller.enabled = true;

        HP = 1;
    }

    IEnumerator DoDash()
    {

       float startTime = Time.time;
       Vector3 dashDirection = move; // Assuming move is the direction you want to dash in.

       // Disable gravity by storing the current playerVelocity.y
       float originalYVelocity = playerVelocity.y;
       playerVelocity.y = 0;

       while (Time.time < startTime + dashDuration)
       {
           controller.Move(dashDirection.normalized * dashSpeed * Time.deltaTime);
           yield return null;
       }

       // Re-enable gravity
       playerVelocity.y = originalYVelocity;
    }

    void DashInput()
    {
        if (Input.GetButtonDown("Dash") && Input.GetAxis("Vertical") > 0.1f)
        {
            if (!hitPenalty)
            {
                if (canBeat && !hitBeat)
                {
                    hitBeat = true;
                    AudioManager.instance.playOnce(dashSFX);
                    StartCoroutine(GameManager.instance.FlashLines(dashDuration));
                    StartCoroutine(DoDash());
                }
                else
                {
                    hitPenalty = true;
                    BoopPenalty();
                }
            }
        }
    }

    void SlamInput()
    {
        if (Input.GetButtonDown("Slam") && !groundedPlayer && !slamming)
        {
            if (!hitPenalty)
            {
                if (canBeat && !hitBeat)
                {
                    hitBeat = true;
                    AudioManager.instance.playOnce(slamSound);
                    StartCoroutine(DoSlam());
                }
                else
                {
                    hitPenalty = true;
                    BoopPenalty();
                }
            }
        }
    }

    IEnumerator DoSlam()
    {
        slamming = true;

        // Disable player horizontal control and gravity influence here if desired
        float originalGravity = gravityValue;
        gravityValue = 0;

        // Apply the ground pound speed directly downwards
        playerVelocity.y = -slamSpeed;

        // Wait until the player hits the ground
        while (!groundedPlayer)
        {
            yield return null;
        }

        // Apply the slam impact logic (e.g., damage enemies, create an impact effect, etc.)
        SlamImpact();

        // Reset gravity influence
        gravityValue = originalGravity;

        slamming = false;
    }

    void SlamImpact()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, boopDist))
        {
            if (!hit.collider.isTrigger)
            {
                IBoop boopable = hit.collider.GetComponent<IBoop>();

                if (hit.transform != transform && boopable != null)
                {
                    boopable.DoBoop(boopForce);
                }
            }
        }
    }
}