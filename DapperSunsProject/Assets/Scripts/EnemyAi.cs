using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAi : Beat, IDamage
{
    [Header("---Components---")]
    [SerializeField] Renderer model;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Color flash;
    [SerializeField] Material returnColor;
    [SerializeField] Transform shootPos;

    [Header("---Stats---")]
    [SerializeField] int HP;
    [SerializeField] int PlayerFaceSpeed;

    [Header("---Gun stats---")]
    [SerializeField] GameObject bullet;
    [SerializeField] float shootRate;

    Vector3 playerDirection;
    bool isShooting;
    bool playerInRange;

    // Start is called before the first frame update

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if(playerInRange)
        {
            playerDirection = GameManager.instance.player.transform.position - transform.position;

            //if (!isShooting)
            //{
            //    StartCoroutine(Shoot());
            //}

            if (agent.remainingDistance < agent.stoppingDistance)
            {
                FaceTarget();
            }
            agent.SetDestination(GameManager.instance.player.transform.position);
        }

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public void takeDamage(int Amount)
    {
        HP -= Amount;

        StartCoroutine(FlashColor());

        if(HP <= 0)
        {
            Destroy(gameObject);
        }
    }

    protected override void DoBeat()
    {
        Instantiate(bullet, shootPos.position, transform.rotation);
    }

    //IEnumerator Shoot()
    //{
    //    isShooting = true;

    //    Instantiate(bullet, shootPos.position, transform.rotation);

    //    yield return new WaitForSeconds(shootRate);

    //    isShooting = false;
    //}
    IEnumerator FlashColor()
    {
        model.material.color = flash;

        yield return new WaitForSeconds(0.1f);

        model.material = returnColor;
    }
    void FaceTarget()
    {
        Quaternion Rotation = Quaternion.LookRotation(playerDirection);

        transform.rotation = Quaternion.Lerp(transform.rotation, Rotation, Time.deltaTime * PlayerFaceSpeed);
    }
}
