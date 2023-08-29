using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using WjChallenge;

//public enum CurrentStatus 
//{ 
//    WAITING, 
//    DIAGNOSIS, 
//    LEARNING 
//}

public enum DiagonosticStatus
{
    Undefined = 0,
    ChoosingDiff,
    OnChoosingDiffAnimation,
    OnSolving, // 문제 풀이 중
    OnSolveAnimation, // 연출 중 : 다음 문제 대기
    DiagonosticFinished
}

public class WJ_Sample_Diagonostic : MonoBehaviour
{
    #region 싱글톤
    public static WJ_Sample_Diagonostic Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WJ_Sample_Diagonostic>();
            }
            return instance;
        }
    }
    private static WJ_Sample_Diagonostic instance;
    #endregion

    [SerializeField]
    DiagonosticStatus status;
    
    [Header("Panels")]
    [SerializeField] GameObject panel_diag_chooseDiff;  //난이도 선택 패널
    [SerializeField] GameObject panel_question;         //문제 패널(진단,학습)
    [SerializeField] GameObject panel_finish;         //문제 패널(진단,학습)

    [SerializeField] Text       textDescription;        //문제 설명 텍스트
    [SerializeField] TEXDraw    textEquation;           //문제 텍스트(※TextDraw로 변경 필요)
    [SerializeField] Button[]   btAnsr = new Button[4]; //정답 버튼들
    TEXDraw[]                   textAnsr;                  //정답 버튼들 텍스트(※TextDraw로 변경 필요)

    [Header("Status")]
    //int     currentQuestionIndex;
    int     QuestionCount = 8;    
    bool    isSolvingQuestion;
    float   questionSolveTime;    

    [Header("TEXDraw 폰트 설정 문자열")]
    [SerializeField]
    string texDrawfontText = @"\galmuri";

    [Header("진단 평가 점수")]
    [SerializeField]
    int score;
    [SerializeField]
    int scorePreCurrect = 5; // 문제 정답 시 점수

    [Header("난이도 선택 후 연출 : 처음으로 문제 정보를 알아왔을 때 한번만 실행")]
    public UnityEvent onGetQuestionFirst;
    bool eventInvoked = false;

    [Header("진단 평가 점수")]
    [SerializeField]
    Animator anim;

    WJ_Connector wj_conn => WJ_Connector.Instance;
    Diagnotics_Data data;

    //[Header("For Debug")]
    //[SerializeField] WJ_DisplayText wj_displayText;         //텍스트 표시용(필수X)
    //[SerializeField] Button getLearningButton;      //문제 받아오기 버튼

    //[SerializeField] WJ_Connector       wj_conn;
    //CurrentStatus               currentStatus;
    //public CurrentStatus      CurrentStatus => currentStatus;

    private void Awake()
    {
        textAnsr = new TEXDraw[btAnsr.Length];
        for (int i = 0; i < btAnsr.Length; ++i)
        {
            textAnsr[i] = btAnsr[i].GetComponentInChildren<TEXDraw>();
        }        
    }

    private void OnEnable()
    {
        OnStartDiagonostic();
    }

    private void Update()
    {
        if (isSolvingQuestion) questionSolveTime += Time.deltaTime;
    }

    void OnGetQuestionFirst()
    {
        if (eventInvoked) return;
        eventInvoked = true;

        //Debug.Log("InvokeOnGetQuestion");
        onGetQuestionFirst.Invoke();
        UpdateQuestionUI(data.textCn, data.qstCn, data.qstCransr, data.qstWransr);
    }

    // 진단 평가 시작 시
    void OnStartDiagonostic()
    {        
        if (wj_conn is null) Debug.LogError("Cannot find Connector");
        wj_conn.onGetDiagnosis.AddListener(delegate {
            GetDiagnosis();
            OnGetQuestionFirst();              
        });
        //wj_conn.onGetDiagnosis.RemoveListener()

        status = DiagonosticStatus.ChoosingDiff;
        panel_diag_chooseDiff.SetActive(true);        
    }

    // 진단 평가 완료 시
    void OnEndDiagonostic()
    {
        status = DiagonosticStatus.DiagonosticFinished;

        panel_question.SetActive(false);
        panel_finish.SetActive(true);

        UIManager_Diagonostic.Instance.SetDiagonosisScore(score);

        SaveManager.DiagonosticCompleted = true;
        SaveManager.DiagonosticScore = score;        
    }

    // 난이도 버튼 선택 시 : 버튼 이벤트로 호출
    public void OnChooseDifficulty(int a)
    {        
        wj_conn.FirstRun_Diagnosis(a);        
        //status = DiagonosticStatus.OnChoosingDiffAnimation;
    }    

    // 진단평가 문제 받아오기 : 커넥터 이벤트에서 호출 (진단평가 문제를 받아온 직후)
    void GetDiagnosis()
    {
        //Debug.Log("GetDiagnosis");

        data = wj_conn.cDiagnotics.data;
        switch (data.prgsCd)
        {
            case "W":                                
                Debug.Log("진단평가 데이터 받아옴");                
                break;
            case "E":
                Debug.Log("진단평가 완료");                                
                break;
        }
    }

    // 다음 문제 불러오기 : 애니메이션 이벤트 호출 (BulletAnimController의 NextProblem 메소드)
    public void ToNextQuestion()
    {
        if (status != DiagonosticStatus.OnSolveAnimation) return;

        if (QuestionCount > 0)
        {
            UpdateQuestionUI(data.textCn, data.qstCn, data.qstCransr, data.qstWransr);
        }
        else
        {
            OnEndDiagonostic();
        }        
    }

    // 받아온 데이터를 가지고 문제를 표시
    public void UpdateQuestionUI(string textCn, string qstCn, string qstCransr, string qstWransr)
    {
        Debug.Log("UpdateQuestionUI");

        status = DiagonosticStatus.OnSolving;
                
        panel_diag_chooseDiff.SetActive(false);
        panel_question.SetActive(true);

        string      correctAnswer;
        string[]    wrongAnswers;

        textDescription.text = textCn;
        textEquation.text = texDrawfontText + qstCn;

        correctAnswer = qstCransr;
        wrongAnswers = qstWransr.Split(',');

        int ansrCount = Mathf.Clamp(wrongAnswers.Length, 0, 3) + 1;

        for(int i=0; i<btAnsr.Length; i++)
        {
            if (i < ansrCount)
                btAnsr[i].gameObject.SetActive(true);
            else
                btAnsr[i].gameObject.SetActive(false);
        }

        int ansrIndex = Random.Range(0, ansrCount);

        for(int i = 0, q = 0; i < ansrCount; ++i, ++q)
        {
            if (i == ansrIndex)
            {
                textAnsr[i].text = texDrawfontText + correctAnswer;
                --q;
            }
            else
                textAnsr[i].text = texDrawfontText + wrongAnswers[q];
        }

        isSolvingQuestion = true;
        questionSolveTime = 0;        
        DiagonosticManager.Instance.StartTimeBar();
    }

    // 답을 고르고 맞았는 지 체크
    public void SelectAnswer(int _idx = -1)
    {
        isSolvingQuestion = false;
        QuestionCount--;

        UIManager_Diagonostic.Instance.DoBulletUsingEffect();
        UIManager_Diagonostic.Instance.SetLeftQuestionCount(QuestionCount);
        DiagonosticManager.Instance.StopTimeBar();

        bool isCorrect;
        string ansrCwYn;
        string myAnsr;
        string currectAnsr = wj_conn.cDiagnotics.data.qstCransr;

        if (_idx == -1) myAnsr = ""; // 답안 제출하지 못함 (공란?)                        
        else
        {
            myAnsr = textAnsr[_idx].text;
            myAnsr = myAnsr.Replace(texDrawfontText, ""); // 폰트 문자열 제거
        }

        // 답안 평가
        isCorrect = myAnsr.CompareTo(currectAnsr) == 0 ? true : false;
        ansrCwYn = isCorrect ? "Y" : "N";  
        
        // 커넥터 통해 문제 답안 결과 보내기
        wj_conn.Diagnosis_SelectAnswer(myAnsr, ansrCwYn, (int)(questionSolveTime * 1000));

        // 현재 상태 : 애니메이션 연출 상태
        status = DiagonosticStatus.OnSolveAnimation;

        // 정답/오답 시 처리        
        if (isCorrect)
        {
            // 정답 처리
            UIManager_Diagonostic.Instance.CreateCurrectUI();
            score += scorePreCurrect;

            // 연출
            switch (_idx)
            {
                case 0:
                    anim.SetTrigger("click0");
                    break;
                case 1:
                    anim.SetTrigger("click1");
                    break;
                case 2:
                    anim.SetTrigger("click2");
                    break;
                case 3:
                    anim.SetTrigger("click3");
                    break;
                default:
                    Debug.Log("_idx error");
                    break;
            }            
        }
        else
        {
            // 오답 처리
            UIManager_Diagonostic.Instance.CreateIncurrectUI();

            anim.SetTrigger("disappear");
        }        

        // 디버깅
        {
            //Debug.Log("isCorrect : " + isCorrect);
            Debug.Log("SelectAnswer idx : " + _idx);
            //Debug.Log("myAnsr : " + myAnsr);
            //Debug.Log("currectAnsr : " + currectAnsr);
            //wj_displayText.SetState("진단평가 중", myAnsr, ansrCwYn, questionSolveTime + " 초");
        }
    }

    #region 만료됨

    /// <summary>
    ///  n 번째 학습 문제 받아오기
    /// </summary>
    private void GetLearning(int _index)
    {
        //if (_index == 0) currentQuestionIndex = 0;

        //WjChallenge.Learning_Question qst = wj_conn.cLearnSet.data.qsts[_index];
        //MakeQuestion(qst.textCn, qst.qstCn, qst.qstCransr, qst.qstWransr);
    }

    public void DisplayCurrentState(string state, string myAnswer, string isCorrect, string svTime)
    {
        //if (wj_displayText == null) return;

        //wj_displayText.SetState(state, myAnswer, isCorrect, svTime);
    }

    public void EvaluateDiagonostic()
    {
        // 진단 평과 결과 종합하여 합산 (풀이 시간 등)
        //Debug.Log("EvaluateDiagonostic");
    }

    #endregion
}
