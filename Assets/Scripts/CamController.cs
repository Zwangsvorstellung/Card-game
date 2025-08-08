using UnityEngine;

public class CamController : MonoBehaviour
{
    private static CamController instance = null;
    public static CamController Instance => instance;
    private bool viewBoardOn = false;

    [SerializeField] Transform headView;
    [SerializeField] Transform boardView;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
       // HandleMouseScroll();
    }

    private void HandleMouseScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0)
            GoToHeadView();
        else if (scroll > 0)
            GoToBoardView();
    }

    public void GoToHeadView()
    {
        iTween.MoveTo(gameObject, headView.position, .5f);
        iTween.RotateTo(gameObject, headView.rotation.eulerAngles, .5f);
        viewBoardOn = false;
    }
    
    public void GoToBoardView()
    {
        iTween.MoveTo(gameObject, boardView.position, .5f);
        iTween.RotateTo(gameObject, boardView.rotation.eulerAngles, .5f);
        viewBoardOn = true;
    }
    
    public bool ViewBoardOn => viewBoardOn;
}
