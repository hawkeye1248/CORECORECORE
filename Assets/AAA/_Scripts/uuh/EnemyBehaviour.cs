using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    // --- Davranış Türünü Seçmek İçin ---
    public enum AIType { Sentry, Patrol };
    public enum AttackType { Melee, Ranged };
    public AIType aiType;
    public AttackType attackType;

    // --- Genel Değişkenler ---
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    // --- Durum Değişkenleri ---
    public float sightRange, attackRange;
    private bool playerInSightRange, playerInAttackRange;
    
    // --- Saldırı Değişkenleri ---
    public float timeBetweenAttacks;
    private bool alreadyAttacked;

    // --- Devriye (Patrol) Değişkenleri ---
    [Tooltip("Sadece 'Patrol' tipindeki düşmanlar için kullanılır.")]
    public float walkPointRange;
    private Vector3 walkPoint;
 [SerializeField]    private bool walkPointSet;
    
    // --- Gözcü (Sentry) Değişkenleri ---
    private Vector3 startingPosition;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Başlangıç pozisyonunu kaydediyoruz. Sentry tipi için gerekli.
        startingPosition = transform.position;
    }

    private void Start()
    {
        // Oyuncuyu bulma işlemini Singleton pattern ile yapmak en sağlıklısı.
        // Eğer Player script'inizde 'public static Player instance;' varsa bu kod çalışır.
        // Yoksa eski yönteme dönebilirsiniz: player = GameObject.FindGameObjectWithTag("Player").transform;
        if (Player.Instance != null)
        {
            player = Player.Instance.transform;
        }
        else
        {
            Debug.LogError("Oyuncu bulunamadı! Player script'inde Singleton instance olduğundan emin olun.");
        }
    }



    private void Update()
    {
        // Oyuncunun menzilde olup olmadığını kontrol et
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // --- YAPAY ZEKA DURUM MAKİNESİ ---
        // Bu kısım, düşmanın mevcut durumuna göre hangi eylemi yapacağına karar verir.

        // 1. Durum: Oyuncu ne görüş ne de saldırı menzilinde
        if (!playerInSightRange && !playerInAttackRange)
        {
            // Davranış türüne göre hareket et
            switch (aiType)
            {
                case AIType.Sentry:
                    ReturnToStartPosition(); // Başlangıç noktasına geri dön
                    break;
                case AIType.Patrol:
                    Patroling(); // Devriye at
                    break;
            }
        }

        // 2. Durum: Oyuncu görüş menzilinde ama saldırı menzilinde değil
        if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer(); // Oyuncuyu takip et
        }

        // 3. Durum: Oyuncu hem görüş hem de saldırı menzilinde
        if (playerInAttackRange && playerInSightRange)
        {
            AttackPlayer(); // Oyuncuya saldır
        }
    }

    // --- EYLEM FONKSİYONLARI ---

    // TİP 1: SENTRY - Başlangıç noktasına dönme
    private void ReturnToStartPosition()
    {
        // NavMesh Agent'a başlangıç pozisyonunu hedef olarak ver
        agent.SetDestination(startingPosition);
    }

    // TİP 2: PATROL - Devriye atma
   private void Patroling()
{
    // Eğer bir hedefimiz yoksa, yeni bir tane arayalım.
    if (!walkPointSet)
    {
        SearchWalkPoint();
    }

    // SADECE geçerli bir hedefimiz varsa hareket etme ve mesafe kontrolü yapalım.
    if (walkPointSet)
    {
        // Hedefi belirle
        agent.SetDestination(walkPoint);

        // Hedefe olan mesafeyi hesapla
        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        // Hedefe ulaşıldıysa, bir sonraki sefere yeni bir nokta araması için hedefi sıfırla.
        if (distanceToWalkPoint.magnitude < 2f)
            walkPointSet = false;
    }
}

    private void SearchWalkPoint()
{
    for (int i = 0; i < 10; i++) // Geçerli bir nokta bulmak için 30 kez dene
    {
        // 1. Rastgele bir nokta oluştur
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        Vector3 randomPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        NavMeshHit hit;
        // 2. Bu noktanın yakınında geçerli bir NavMesh yüzeyi bul
        if (NavMesh.SamplePosition(randomPoint, out hit, walkPointRange, NavMesh.AllAreas))
        {
            // 3. ULAŞILABİLİRLİK KONTROLÜ
            NavMeshPath path = new NavMeshPath();
            // Ajanın mevcut konumundan bulunan noktaya bir yol hesapla
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                // Eğer yol tam ve eksiksiz ise, bu nokta geçerlidir!
                walkPoint = hit.position;
                walkPointSet = true;
                return; // Döngüden ve fonksiyondan çık
            }
        }
    }
    // 30 denemede de ulaşılabilir bir nokta bulunamadıysa, AI bir sonraki frame'de tekrar dener.
}

    // ORTAK: Oyuncuyu takip etme
    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    // ORTAK: Oyuncuya saldırma
    private void AttackPlayer()
    {
        if (attackType == AttackType.Melee)
        {
            MeleeAttack();
        }
        else if (attackType == AttackType.Ranged)
        {
            RangedAttack();
        }
        // Saldırırken hareket etme
        
    }

    private void RangedAttack()
    {
        agent.SetDestination(transform.position);

        // Yüzünü oyuncuya dön
        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            // --- Saldırı Kodu ---
            //Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            //rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
            //rb.AddForce(transform.up * 8f, ForceMode.Impulse);
            // --- Bitiş ---

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }


    private void MeleeAttack()
    {
        throw new System.NotImplementedException();
    }


    private void ResetAttack()
    {
        alreadyAttacked = false;
    }
}