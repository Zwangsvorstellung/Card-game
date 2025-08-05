using UnityEngine;

public class CamController : MonoBehaviour
{
    private static CamController instance = null;
    public static CamController Instance => instance;

    [SerializeField] Transform headView;
    [SerializeField] Transform boardView;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
            instance = this;
            DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if(Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            GoToHeadView();
        }
        if(Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            GoToBoardView();
        }
    }

    private bool estEnVueBoard = false;
    
    public void GoToHeadView()
    {
        iTween.MoveTo(gameObject, headView.position, .5f);
        iTween.RotateTo(gameObject, headView.rotation.eulerAngles, .5f);
        estEnVueBoard = false;
    }
    
    public void GoToBoardView()
    {
        iTween.MoveTo(gameObject, boardView.position, .5f);
        iTween.RotateTo(gameObject, boardView.rotation.eulerAngles, .5f);
        estEnVueBoard = true;
    }
    
    public bool EstEnVueBoard()
    {
        return estEnVueBoard;
    }
}
