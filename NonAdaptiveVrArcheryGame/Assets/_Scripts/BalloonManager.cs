using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    public GameObject balloon;
    public static BalloonManager Instance { get; private set; }
    private int currentBalloonCount = 0;
    public int CurrentBalloonCount => currentBalloonCount; 
    public int maxMissles;

    [Header("Target")]
    [SerializeField] private Transform target;
    public Transform Target => target;

    // Update is called once per frame
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    void Update()
    {   
        if (currentBalloonCount < maxMissles)
        {
            //TODO: this random spawn position range needs to be adjusted, its too close to the camera
            Vector3 randomSpawnPosition = new Vector3(
                Random.Range(-10, 11),
                5,
                Random.Range(-10, 11)
            );
            GameObject newBalloon = Instantiate(balloon, randomSpawnPosition, Quaternion.identity);
            newBalloon.GetComponent<Balloon>().SetTarget(target); // <-- Set target here
            RegisterBalloon();
        }
    }

    public void RegisterBalloon()
    {
        currentBalloonCount++;
    }

    public void ReportBalloonDown()
    {
        currentBalloonCount--;
        if(currentBalloonCount < 0)
        {
            currentBalloonCount = 0;
        } 
    }
}
