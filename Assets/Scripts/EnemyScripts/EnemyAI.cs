using UnityEngine;

[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyAI : MonoBehaviour
{
    public float alcanceDeteccao = 10f;
    public float distanciaParagemAtaque = 4f;
    public float intervaloMovimento = 3f;
    public float raioMovimento = 5f;
    public LayerMask camadaChao;
    public LayerMask camadaAgua;

    private EnemyController movimento; // Alterado para EnemyController
    private EnemyShooting atirador;
    private Transform jogador;
    private Vector3 alvoAleatorio;
    private float temporizador;
    private bool aPerseguir = false;

    void Start()
    {
        // Agora usamos EnemyController em vez de CharacterMovement2D
        movimento = GetComponent<EnemyController>();
        if (movimento == null)
        {
            Debug.LogError("EnemyAI: Componente EnemyController não encontrado no GameObject!");
            enabled = false;
            return;
        }

        atirador = GetComponent<EnemyShooting>();
        
        // Encontrar o jogador de forma segura
        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null)
        {
            jogador = jogadorObj.transform;
        }
        else
        {
            Debug.LogError("EnemyAI: Não foi encontrado nenhum objeto com a tag 'Player'");
        }

        alvoAleatorio = transform.position;
        temporizador = intervaloMovimento;
    }

    void Update()
    {
        if (movimento == null) return;

        if (jogador == null) 
        {
            // Tentar encontrar o jogador novamente
            GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jogadorObj != null)
            {
                jogador = jogadorObj.transform;
            }
            else
            {
                return;
            }
        }

        float distanciaAoJogador = Vector2.Distance(transform.position, jogador.position);

        // Detetar jogador
        if (distanciaAoJogador <= alcanceDeteccao)
        {
            aPerseguir = true;
        }
        else if (distanciaAoJogador > alcanceDeteccao * 1.2f)
        {
            aPerseguir = false;
        }

        if (aPerseguir)
        {
            if (distanciaAoJogador > distanciaParagemAtaque)
            {
                // Usar o método SetTarget do EnemyController
                movimento.SetTarget(jogador.position);
            }
            else
            {
                // Parar para disparar - usar a posição atual
                movimento.SetTarget(transform.position);
                
                if (atirador != null)
                {
                    // Se o teu EnemyShooting tiver um método para disparar, podes chamá-lo aqui
                    // atirador.Disparar();
                }
            }
        }
        else
        {
            Patrulhar();
        }
    }

    void Patrulhar()
    {
        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            alvoAleatorio = ObterPosicaoChaoAleatoria();
            movimento.SetTarget(alvoAleatorio);
            temporizador = intervaloMovimento;
        }
    }

    Vector3 ObterPosicaoChaoAleatoria()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 posicaoAleatoria = transform.position + (Vector3)(Random.insideUnitCircle * raioMovimento);
            if (EstaNoChao(posicaoAleatoria) && !EstaNaAgua(posicaoAleatoria))
                return posicaoAleatoria;
        }
        return transform.position;
    }

    bool EstaNoChao(Vector3 posicao) 
    {
        Collider2D colisao = Physics2D.OverlapCircle(posicao, 0.2f, camadaChao);
        return colisao != null;
    }

    bool EstaNaAgua(Vector3 posicao) 
    {
        Collider2D colisao = Physics2D.OverlapCircle(posicao, 0.2f, camadaAgua);
        return colisao != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceDeteccao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaParagemAtaque);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(alvoAleatorio, Vector3.one * 0.3f);

        if (Application.isPlaying)
        {
            Gizmos.color = aPerseguir ? Color.green : Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}