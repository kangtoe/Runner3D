using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Skill : MonoBehaviour
{
    [SerializeField]
    Image fill;

    [SerializeField]
    float coolTime = 10;

    [SerializeField]
    float coolTimeLeft;

    [SerializeField]
    float skillActiveTime = 3f;

    public bool IsSkillActive => isSkillActive;
    [SerializeField] // 디버그용
    bool isSkillActive;

    // Update is called once per frame
    void Update()
    {
        if (coolTimeLeft > 0) coolTimeLeft -= Time.deltaTime;        
        else if (coolTimeLeft < 0) coolTimeLeft = 0;

        // 이미지 fillAmount => 남은 쿨타임 / 전체 쿨타임
        float ratio = 1 - coolTimeLeft / coolTime;
        fill.fillAmount = ratio;
    }

    public void UseSkill()
    {
        if (coolTimeLeft > 0)
        {
            return;
        }

        Debug.Log("스킬 사용");
        StartCoroutine(SkillCr());
        coolTimeLeft = coolTime;
    }


    IEnumerator SkillCr()
    {
        // 스킬효과 활성화
        isSkillActive = true;

        yield return new WaitForSeconds(skillActiveTime);

        // 스킬효과 비활성화
        isSkillActive = false;
    }
}
