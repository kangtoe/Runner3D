using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region 싱글톤
    public static Player Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Player>();
            }
            return instance;
        }
    }
    private static Player instance;
    #endregion

    [Header("이동")]    
    [SerializeField]
    float forwardSpeed = 1f;
    float ForwardSpeed
    {
        get
        {
            float mult = 1;
            if (skill.IsSkillActive) mult = 1.5f;

            return forwardSpeed * mult;
        }
    }

    [SerializeField]
    float sideSpeed = 1f;
    float SideSpeed
    {
        get {
            float mult = 1;
            if (skill.IsSkillActive) mult = 1.5f;

            return sideSpeed * mult;
        }        
    } 

    [Header("공격")]    
    //[SerializeField]
    float attackInterval = 1; // upgade 레벨에 따라 제어됨
    //float lastAttackTime = 0;
    bool canAttack = true;
    [SerializeField]
    float attackRange = 5f;

    [Header("스킬 시전 중 보스에 대한 피해 증폭")]
    [SerializeField]
    float skillDamageToBossMult = 3f;

    // 일반 피해값
    public int damageOnNomal => ScoreManager.Instance.GetCurrentScore();
    // 스킬 발동 중, 일반 적에 대한 피해
    public int damageOnSkillToEnemy => int.MaxValue;
    // 스킬 발동 중, 적 보스에 대한 피해
    public int damageOnSkillToBoss => (int)(damageOnNomal * skillDamageToBossMult);

    [Header("리볼버")]
    [SerializeField]
    GameObject revolver;

    [Header("공격 이펙트")]
    [SerializeField]
    ParticleSystem nozzle;
    [SerializeField]
    BulletParticle nomalParticle;
    [SerializeField]
    BulletParticle skillParticle;

    [Header("스킬")]
    [SerializeField]
    SkillManager skill;

    [Header("텍스트 출력")]
    [SerializeField]
    Transform textPoint;

    [Header("애니메이터")]
    [SerializeField]
    Animator anim;

    // 사망
    bool isDead = false;

    // 코루틴
    Coroutine moveCr;
    Coroutine attackCr;


    // Start is called before the first frame update
    void Start()
    {
        attackCr = StartCoroutine(AttackCheckCr(attackInterval));   
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;

        // 전방으로 이동 (z축 이동)
        transform.Translate(ForwardSpeed * transform.forward * Time.deltaTime);        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    #region X축 이동 제어

    public void MovePosX_Smooth(float targetX)
    {
        if (isDead) return;
        //Debug.Log("MovePosY_Smooth || targetY :" + targetX);
        
        if (moveCr != null) StopCoroutine(moveCr);        
        moveCr = StartCoroutine(MovePosX_SmoothCr(targetX));
    }

    IEnumerator MovePosX_SmoothCr(float targetX)
    {
        while (true)
        {
            float currentX = transform.position.x;

            // 현재 X 위치를 목표 위치 방향으로 조금씩 이동
            if (targetX > currentX) currentX += Time.fixedDeltaTime * SideSpeed;
            else if (targetX < currentX) currentX -= Time.fixedDeltaTime * SideSpeed;

            bool closeEnough = Mathf.Abs(targetX - currentX) <= Time.fixedDeltaTime * SideSpeed;
            if (closeEnough) currentX = targetX;

            transform.position = new Vector3(currentX, transform.position.y, transform.position.z);
            if (currentX == targetX) yield break;

            yield return new WaitForFixedUpdate();
        }
    }

    #endregion

    #region 공격

    public void SetAttackInterval(float time)
    {
        attackInterval = time;
    }

    // 공격 검사
    IEnumerator AttackCheckCr(float interval)
    {
        while (true)
        {
            if (canAttack)
            {
                TryAtack();
            }

            yield return new WaitForSeconds(interval);
        }
    }

    void TryAtack()
    {
        //Enemy target = EnemySpwaner.Instance.GetClosestEnemy_Z();
        Enemy target = EnemySpwaner.Instance.GetClosestEnemy_Transform(transform, attackRange);

        //bug.Log("TryAtack");

        // 가장 가까운 적 공격
        if (target is null)
        {
            //Debug.Log("target is null");
            return;
        }
        //else Debug.Log("target :" + target.name);

        //lastAttackTime = Time.time;
        Attack(target);
    }

    void Attack(Enemy target)
    {
        nozzle.Play();

        Vector3 point = target.GetComponent<Collider>().ClosestPoint(nomalParticle.transform.position);

        // 사격 파티클 생성
        if (skill.IsSkillActive)
        {
            skillParticle.Shoot(point);
        }
        else
        {
            nomalParticle.Shoot(point);
        }
    }

    #endregion

    // 사망
    public void OnDead()
    {
        anim.SetTrigger("dead");
        revolver.SetActive(false);
        isDead = true;
        StopAllCoroutines();        
    }

    public void MakeText(string str)
    {
        Text3dMaker.Instance.MakeText(str, Vector3.zero ,textPoint);
    }
}
