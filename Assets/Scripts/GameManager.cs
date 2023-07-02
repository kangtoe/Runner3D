using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region 싱글톤
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }
    private static GameManager instance;
    #endregion

    public Transform ClickPointObject => clickPointObject;
    [SerializeField]
    Transform clickPointObject;
    
    [SerializeField]
    LayerMask groundLayerMask;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ClickCheck();
    }

    // 클릭 지점 지면에 클릭 포인터 표시
    void ClickCheck()
    {
        // 마우스 오른쪽 입력 처리
        if (Input.GetMouseButtonDown(1))
        {
            Vector3? point = GetClickPoint();
            if (point == null)
            {
                Debug.Log("클릭 지점 Ground 물체 없음");
                return;
            }

            ClickPointer.Instance.SetPointer(point.Value);
            clickPointObject.position = point.Value;
            Player.Instance.MovePosX_Smooth(point.Value.x);
        }
    }

    // 마우스 클릭 지점 알아내기
    Vector3? GetClickPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, groundLayerMask))
        {
            return hit.point;
        }
        return null;
    }
}
