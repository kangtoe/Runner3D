using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public enum MathpidStatus
{
    Undefined = 0,    
    OnStartAnimation,
    OnSolving, // 문제 풀이 중
    OnSolveAnimation, // 문제 풀이 직후 연출 중
    Finished
}

// 1. 시작 시 : 문제 알아오기 요청
// 2. 문제를 알아온 직후 : 문제를 UI에 적용

// 3. 판넬이 활성화 되었을 때 : 시작 애니메이션 재생
// 4. 문제 답 제출 시 : 정답/오답 애니메이션 재생
// 5. 정답/오답 애니메이션 종료 시 : 창 닫기, 다음 문제 알아와 UI 적용

// Play 씬에서, mathpid 발판을 밟았을 때 학습 문제 표시 및 체점
public class WJ_Sample_Play : MonoBehaviour
{
    public static WJ_Sample_Play Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WJ_Sample_Play>();
            }
            return instance;
        }
    }
    private static WJ_Sample_Play instance;

    [SerializeField]
    MathpidStatus status;

    [Header("Panels")]
    [SerializeField] GameObject panel_question;         //문제 패널(진단,학습)

    [SerializeField] Text textDescription;        //문제 설명 텍스트
    [SerializeField] TEXDraw textEquation;           //문제 텍스트(※TextDraw로 변경 필요)
    [SerializeField] Button[] btAnsr = new Button[4]; //정답 버튼들
    TEXDraw[] textAnsr;                  //정답 버튼들 텍스트(※TextDraw로 변경 필요)
   
    [Header("Status")]
    int currentQuestionIndex;
    bool isSolvingQuestion;
    float questionSolveTime;

    [Header("TEXDraw 폰트 설정 문자열")]
    [SerializeField]
    string texDrawfontText = @"\cmb";

    [Header("시작 연출")]
    public UnityEvent onStart;

    [Header("Debug")]
    [SerializeField] 
    bool dataSetting = false; // 데이터 갱신 중?
    [SerializeField]    
    int loop = 0; // 8개 문제 묶음을 받아 온 횟수
    [SerializeField]
    float timeScaleOnActivePanel = 0.1f; // 문제 풀이 중 시간 스케일

    WJ_Connector wj_conn => WJ_Connector.Instance;


    #region 유니티 콜백

    private void Awake()
    {
        // 버튼의 TEXDraw 컴포넌트 알아오기
        textAnsr = new TEXDraw[btAnsr.Length];
        for (int i = 0; i < btAnsr.Length; ++i)
        {
            textAnsr[i] = btAnsr[i].GetComponentInChildren<TEXDraw>();
        }            
        
        ActivePanel(false);
    }

    private void OnEnable()
    {
        
    }

    private void Update()
    {
        if (isSolvingQuestion) questionSolveTime += Time.deltaTime;
    }

    #endregion

    // 문제 풀이 시작
    public void StartQuestion()
    {
        ActivePanel(true);

        // 문제 표기
        if (currentQuestionIndex % 8 == 0) loop++;        

        DoNewQuestions();        
    }
    
    //  n 번째 학습 문제 받아와, 문제 표기    
    private void GetLearning(int _index)
    {
        //Debug.Log("GetLearning");
        //if (_index == 0) currentQuestionIndex = 0;

        dataSetting = false;
        WjChallenge.Learning_Question qst = wj_conn.cLearnSet.data.qsts[_index];
        MakeQuestion(qst.textCn, qst.qstCn, qst.qstCransr, qst.qstWransr);                   
    }
    
    // 받아온 데이터를 가지고 문제를 표시    
    void MakeQuestion(string textCn, string qstCn, string qstCransr, string qstWransr)
    {
        Debug.Log("MakeQuestion");
        StartTimeBar();

        //panel_question.SetActive(true);
        ActivePanel(true);

        string correctAnswer;
        string[] wrongAnswers;

        textDescription.text = textCn;
        textEquation.text = texDrawfontText + qstCn;

        correctAnswer = qstCransr;
        wrongAnswers = qstWransr.Split(',');

        int ansrCount = Mathf.Clamp(wrongAnswers.Length, 0, 3) + 1;

        for (int i = 0; i < btAnsr.Length; i++)
        {
            if (i < ansrCount)
                btAnsr[i].gameObject.SetActive(true);
            else
                btAnsr[i].gameObject.SetActive(false);
        }

        int ansrIndex = Random.Range(0, ansrCount);

        for (int i = 0, q = 0; i < ansrCount; ++i, ++q)
        {
            if (i == ansrIndex)
            {
                textAnsr[i].text = texDrawfontText + correctAnswer;
                --q;
            }
            else
            {
                textAnsr[i].text = texDrawfontText + wrongAnswers[q]; 
            }            
        }
        isSolvingQuestion = true;
    }    
    
    // 답을 고르고 맞았는 지 체크    
    public void SelectAnswer(int _idx = -1)
    {
        if (dataSetting) Debug.Log("데이터 받아오는 중");

        Debug.Log("SelectAnswer idx : " + _idx);
        InitTimeBar();

        bool isCorrect;
        string ansrCwYn;
        string ansr;

        if (_idx == -1) ansr = ""; // 답안 제출하지 못함 (공란?)                
        else ansr = textAnsr[_idx].text;        

        isCorrect = ansr.CompareTo(wj_conn.cLearnSet.data.qsts[currentQuestionIndex].qstCransr) == 0 ? true : false;
        ansrCwYn = isCorrect ? "Y" : "N";
        
        // 커넥터 통해 문제 답안 결과 보내기
        wj_conn.Learning_SelectAnswer(currentQuestionIndex, ansr, ansrCwYn, (int)(questionSolveTime * 1000));

        isSolvingQuestion = false;
        questionSolveTime = 0;
        
        currentQuestionIndex++;
        currentQuestionIndex %= 8;
        
        ActivePanel(false);

        // 디버그
        {
            Debug.Log("loop : " + loop + " || currentQuestionIndex : " + currentQuestionIndex);
            //if (wj_conn == null) Debug.Log("null 6");
            if (wj_conn.cLearnSet == null) Debug.Log("null 5");
            if (wj_conn.cLearnSet.data == null) Debug.Log("null 4");
            //if (wj_conn.cLearnSet.data.qsts == null) Debug.Log("null 3");
            //if (wj_conn.cLearnSet.data.qsts[currentQuestionIndex] == null) Debug.Log("null 2");
            //if (wj_conn.cLearnSet.data.qsts[currentQuestionIndex].qstCransr == null) Debug.Log("null 1");             
        }
    }

    // 새로운 문제들 받아와 표기
    public void DoNewQuestions()
    {
        if (wj_conn == null) Debug.LogError("Cannot find Connector");

        dataSetting = true;
        // 문제 정보를 새롭게 받아올 때, 0번 문제를 UI에 표시하도록 이벤트 등록
        wj_conn.onGetLearning.AddListener(() => GetLearning(0));
        // 문제 정보를 새롭게 받아옴
        wj_conn.Learning_GetQuestion();        
    }

    // 다음 문제를 표기
    public void DoNextQuestions()
    {
        GetLearning(currentQuestionIndex);
    }

    // 문제 풀이 시간 초과
    void OnOverTime()
    {
        SelectAnswer(-1);
    }

    #region 시간 게이지

    [Header("문제당 풀이시간")]
    [SerializeField]
    float timeLimit = 10;

    [SerializeField]
    Image timeGage;

    [SerializeField]
    //public UnityEvent onTimeEnd;

    public void SetTimeLimit(float time)
    {
        timeLimit = time;
    }

    public void StartTimeBar()
    {
        StopAllCoroutines();
        StartCoroutine(TimeBarCr());
    }

    public void InitTimeBar()
    {
        StopAllCoroutines();
        timeGage.fillAmount = 1;
    }

    IEnumerator TimeBarCr()
    {
        float leftTime = timeLimit;
        while (true)
        {
            leftTime -= Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(leftTime / timeLimit);
            timeGage.fillAmount = ratio;
            yield return null;
            if (ratio == 0) break;
        }

        Debug.Log("time end");

        // 문제 풀이 시간 초과
        //onTimeEnd.Invoke();
        OnOverTime();
    }

    #endregion       

    void ActivePanel(bool active)
    {
        if (active == true)
        {
            TimeManager.Instance.SetScale(timeScaleOnActivePanel);
            panel_question.SetActive(true);
        }
        else if (active == false)
        {
            TimeManager.Instance.SetScale(1);
            panel_question.SetActive(false);
        }
    }    
}
