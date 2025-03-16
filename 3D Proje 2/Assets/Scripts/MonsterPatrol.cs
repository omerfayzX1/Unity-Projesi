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
    //G�ncel Devriye Noktas�
    private int currentPatrolIndex;
      
    private NavMeshAgent agent;

    public float waitTimeAtLastSeen;
    //Son g�rd��� konujm
    private Vector3 lastSeenPosition;
    private bool waitingAtLastSeen;

    private void Start()
    {
        //Script'in at�l� olduo�u objeden NavMeshAgent adl� bile�eni al�r ve atamas�n� sa�lar.
        agent = GetComponent<NavMeshAgent>();
        //Tag'i Player olan objeyi bulur ve atamas�n� yapar
        player  = GameObject.FindGameObjectWithTag("Player").transform;
        //Canavar�n ba�lang�� noktas�n� , ilk devriye noktas� olarak atar
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius,LayerMask);
        //E�er bir oyuncu bulursa bu sat�r� �al��t�r�r.
        foreach (var hit in hits)
        {
            //Oyuncu ve canavar�n konumuna g�re canavar oyuncuya do�ru hareket eder.
            Vector3 directionPlayer = (hit.transform.position - transform.position).normalized;
            //Canavar�n oyuncuya olan a��s�n� hesaplar
            float anglePlayer = Vector3.Angle(transform.forward,directionPlayer);

            //E�er oyuncu g�r�� a��s�ndaysa buray� �al��t�r�r.
            if (anglePlayer<viewAngle /2)
            {
                //Canavardan oyuncuya do�ru bir ���n at�l�r ve oyuncuya �arp�p �arpmad���n� hesaplar
                if (!Physics.Linecast(transform.position,hit.transform.position,obstacleMask))
                {
                    //I��n�n �arpt��� son yere do�ru gider.
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

    //S�radaki devriye noktas�na etmeyi sa�layan methoddur.
    private void GoToNextPatrolPoint()
    {
        //Devriye noktalar� uzunlu�u 0 ise metdou kapat�r
        if (patrolPoints.Length == 0) 
        {
            return;
        }
        //Cnaavar�n konumunu hesaplar
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
        //Kovalanma veya yol bekleme veya kalan mesafe 0.5f'ten k���k ise
        if (!isChasing && !agent.pathPending && agent.remainingDistance <0.5f)
        {

            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }   


    private IEnumerator GoToLastSeenPosition()
    {
        //E�er son g�r�len noktada bekliyorsa metodu �al��t�rma
        if (waitingAtLastSeen) yield break;
        {
            agent.SetDestination(lastSeenPosition);
            waitingAtLastSeen = true;
        }
        //hedefe ula�mas�n� bekles
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
        //Belirli bir zaman bekledikten sonra en yak�n devriye noktas�na geri d�n
        waitingAtLastSeen = false;
        GoToNearestPatrolPoint();
    }
}
