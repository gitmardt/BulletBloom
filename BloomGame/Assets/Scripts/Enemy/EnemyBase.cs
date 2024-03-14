using SmoothShakePro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : MonoBehaviour
{
    //References
    protected Transform player;
    public PlayableDirector attackTimeline;
    public float attackDistance = 5;
    public float damageRange = 4;
    public float attackDamage = 1;
    public float attackAngleThreshold = 45f;
    public bool destroyOnAttack = true;

    public int layerIndex = 0;

    public AudioSource[] hit;

    public bool randomLayer = true;
    public GameObject[] objectsToRandomlyLayer;
    public GameObject[] objectsToRandomlyLayerWithChildren;

    public SmoothShakeStarter enemyIsHitShake;

    //Variables
    public float health = 5;

    private Coroutine activeAttack;

    //Private variables
    private NavMeshAgent agent;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    int[] GetLayerNumbers(LayerMask layerMask)
    {
        List<int> layers = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            if (layerMask == (layerMask | (1 << i)))
            {
                layers.Add(i);
            }
        }

        return layers.ToArray();
    }

    private void Start()
    {
        player = Player.instance.gameObject.transform;

        if (randomLayer)
        {
            LayerMask mask = WaveManager.instance.waves[WaveManager.instance.currentWaveIndex].allowedLayers;
            int[] layers = GetLayerNumbers(mask);

            if (layers.Length > 0)
            {
                int randomIndex = Random.Range(0, layers.Length);
                layerIndex = layers[randomIndex];
            }
            else
            {
                Debug.LogWarning("LayerMask is empty.");
                return;
            }

            gameObject.layer = layerIndex;

            if (objectsToRandomlyLayer.Length > 0)
            {
                foreach(GameObject obj in objectsToRandomlyLayer)
                {
                    obj.layer = layerIndex;
                }

                if(objectsToRandomlyLayerWithChildren.Length > 0)
                {
                    foreach (GameObject obj in objectsToRandomlyLayerWithChildren)
                    {
                        foreach (Transform child in obj.transform)
                        {
                            child.gameObject.layer = layerIndex;
                            SetLayerRecursively(child.gameObject, layerIndex);
                        }
                    }
                }
            }
            else
            {
                foreach (Transform child in gameObject.transform)
                {
                    child.gameObject.layer = layerIndex;
                    SetLayerRecursively(child.gameObject, layerIndex);
                }
            }
        }
        else
        {
            layerIndex = gameObject.layer;
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        foreach (Transform child in obj.transform)
        {
            child.gameObject.layer = newLayer;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public virtual float AttackDuration() => 5;

    // Update is called once per frame
    void Update()
    {
        if (CombatManager.instance.navMeshIsGenerated)
        {
            if (activeAttack == null)
            {
                agent.isStopped = false;
                agent.destination = player.position;
            }
            else
            {
                agent.isStopped = true;
            }
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer <= attackAngleThreshold && Vector3.Distance(player.position, transform.position) <= attackDistance)
        {
            activeAttack ??= StartCoroutine(Attack());
        }
    }

    public virtual IEnumerator Attack()
    {
        attackTimeline.Play();
        yield return new WaitForSeconds(AttackDuration());
        activeAttack = null;
        if (destroyOnAttack) Destroy(gameObject);
    }

    public virtual void DealDamage() 
    {
        if (Vector3.Distance(player.position, transform.position) < attackDistance)
        {
            FeedbackManager.instance.hitShake.StartShake(FeedbackManager.instance.hitShakePlayer);
            FeedbackManager.instance.playerhit.Play();
            FeedbackManager.instance.gameover.Play();
            Player.instance.health--;
        }
    }

    public void Damage()
    {
        if (hit.Length > 0)
        {
            int randomIndex = Random.Range(0, hit.Length);
            Utility.RandomizePitch(hit, randomIndex, 0.8f, 1.2f);
            hit[randomIndex].Play();
        }
        if (enemyIsHitShake != null) enemyIsHitShake.StartShake();
        health--;
        if(health == 0) Die();
        FeedbackManager.instance.hitShake.StartShake(FeedbackManager.instance.hitShakeEnemy);
    }

    public void Die()
    {
        if (Random.value > 0.5f)
        {
            Utility.RandomizePitch(FeedbackManager.instance.enemyDeath1, 0.8f, 1.2f);
            FeedbackManager.instance.enemyDeath1.Play();
        }
        else
        {
            Utility.RandomizePitch(FeedbackManager.instance.enemyDeath2, 0.8f, 1.2f);
            FeedbackManager.instance.enemyDeath2.Play();
        }

        GameObject enemyDieEffect = Instantiate(FeedbackManager.instance.enemyDieEffectPrefab, transform.position, Quaternion.identity);
        FeedbackManager.instance.StartCoroutine(FeedbackManager.instance.InstantiateAndKill(enemyDieEffect,2f));

        FeedbackManager.instance.hitShake.StartShake(FeedbackManager.instance.EnemyDieShake);
        WaveManager.instance.enemies.Remove(gameObject);
        Destroy(gameObject);
    }
}
