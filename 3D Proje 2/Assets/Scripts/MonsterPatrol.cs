using UnityEngine.AI;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.PlayerLoop;
using System.Collections;

public class MonsterPatrol : MonoBehaviour
{
    public float viewRadius = 10f;
    public float viewAngle = 90f;
    public LayerMask LayerMask;
    public LayerMask obstacleMask;

    private Transform player;
    private bool isChasing;

    //Devriye ile ilgili 
    public Transform[] patrolPoints;
    //Güncel Devriye Noktasý
    private int currentPatrolIndex;
      
    private NavMeshAgent agent;

    public float waitTimeAtLastSeen;
    //Son gördüðü konujm
    private Vector3 lastSeenPosition;
    private bool waitingAtLastSeen;

    private void Start()
    {
        //Script'in atýlý olduoðu objeden NavMeshAgent adlý bileþeni alýr ve atamasýný saðlar.
        agent = GetComponent<NavMeshAgent>();
        //Tag'i Player olan objeyi bulur ve atamasýný yapar
        player  = GameObject.FindGameObjectWithTag("Player").transform;
        //Canavarýn baþlangýç noktasýný , ilk devriye noktasý olarak atar
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius,LayerMask);
        //Eðer bir oyuncu bulursa bu satýrý çalýþtýrýr.
        foreach (var hit in hits)
        {
            //Oyuncu ve canavarýn konumuna göre canavar oyuncuya doðru hareket eder.
            Vector3 directionPlayer = (hit.transform.position - transform.position).normalized;
            //Canavarýn oyuncuya olan açýsýný hesaplar
            float anglePlayer = Vector3.Angle(transform.forward,directionPlayer);

            //Eðer oyuncu görüþ açýsýndaysa burayý çalýþtýrýr.
            if (anglePlayer<viewAngle /2)
            {
                //Canavardan oyuncuya doðru bir ýþýn atýlýr ve oyuncuya çarpýp çarpmadýðýný hesaplar
                if (!Physics.Linecast(transform.position,hit.transform.position,obstacleMask))
                {
                    //Iþýnýn çarptýðý son yere doðru gider.
                    lastSeenPosition = hit.transform.position;
                    //Oyuncuyu kovalar
                    ChasePlayer(hit.transform);
                    return;
                }
            }
        }

        if (isChasing)
        {
            isChasing = false;
            StartCoroutine(GoToLastSeenPosition());
        }

        
    }
    
    private Transform FindClosestPatrolPoint()
    {

        Transform closestPatrolPoint = null;

        float minDistance = Mathf.Infinity;

        foreach (Transform point in patrolPoints)
        {

            float distance = Vector3.Distance(transform.position,point.position);

            if (distance<minDistance)
            {
                minDistance = distance;

                closestPatrolPoint = point;         
            }
        }
        return closestPatrolPoint;
    }

    private void GoToNearestPatrolPoint()
    {
        Transform closestPoint = FindClosestPatrolPoint();
        if (closestPoint != null)
        {
            agent.SetDestination(closestPoint.position);
        }
    }

    //Sýradaki devriye noktasýna etmeyi saðlayan methoddur.
    private void GoToNextPatrolPoint()
    {
        //Devriye noktalarý uzunluðu 0 ise metdou kapatýr
        if (patrolPoints.Length == 0) 
        {
            return;
        }
        //Cnaavarýn konumunu hesaplar
        agent.SetDestination(patrolPoints[currentPatrolIndex].position); 
        currentPatrolIndex = (currentPatrolIndex+1)% patrolPoints.Length;
    }

    private void ChasePlayer(Transform playerTransform)
    {
        //Oyuncunun konumuna git
        agent.SetDestination(playerTransform.position);
        isChasing = true;
    }

    private void Update()
    {
        CheckForPlayer();
        //Kovalanma veya yol bekleme veya kalan mesafe 0.5f'ten küçük ise
        if (!isChasing && !agent.pathPending && agent.remainingDistance <0.5f)
        {

            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }   


    private IEnumerator GoToLastSeenPosition()
    {
        //Eðer son görülen noktada bekliyorsa metodu çalýþtýrma
        if (waitingAtLastSeen) yield break;
        {
            agent.SetDestination(lastSeenPosition);
            waitingAtLastSeen = true;
        }
        //hedefe ulaþmasýný bekles
        while (!agent.pathPending & agent.remainingDistance > 0.1f)
        {
            yield return null;
        }
        float elapsedTime = 0f;
        agent.isStopped = true;
        //Belirli bir saniye bekle
        while (elapsedTime < waitTimeAtLastSeen)
        {
            CheckForPlayer();
            if (isChasing)
            {
                agent.isStopped = false;
                waitingAtLastSeen = false;
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        //Belirli bir zaman bekledikten sonra en yakýn devriye noktasýna geri dön
        waitingAtLastSeen = false;
        GoToNearestPatrolPoint();
    }
}
