using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterMovement2D : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidadeMovimento = 4f;
    private Transform alvo;
    private GameObject alvoTemporario;
    public float aceleracao = 8f;
    public float desaceleracao = 10f;
    public float distanciaParagem = 0.05f;

    [Header("Camadas de terreno")]
    public LayerMask camadaChao;
    public LayerMask camadaAgua;
    public LayerMask camadaPonte;

    [Header("Sprites - 8 direções (2 frames + idle)")]
    public Sprite frenteDireita_L;
    public Sprite frenteDireita_R;
    public Sprite frenteDireita_Idle;

    public Sprite frenteEsquerda_L;
    public Sprite frenteEsquerda_R;
    public Sprite frenteEsquerda_Idle;

    public Sprite trasDireita_L;
    public Sprite trasDireita_R;
    public Sprite trasDireita_Idle;

    public Sprite trasEsquerda_L;
    public Sprite trasEsquerda_R;
    public Sprite trasEsquerda_Idle;

    [Header("Marcador de Clique")]
    public GameObject prefabMarcadorClique;

    private SpriteRenderer sr;
    private Camera cam;
    private Vector3 posicaoAlvo;
    private bool estaAMover = false;
    private float velocidadeAtual = 0f;
    private float temporizadorAnim = 0f;
    private bool alternarAnim = false;
    private Vector2 direcaoMovimento = Vector2.zero;
    private Vector2 ultimaDirecao = new Vector2(1, -1);
    private List<Vector3> pontosCaminho = new List<Vector3>();
    private float limiteAlcancePonto = 0.12f;
    public float raioMaximoProcuraPonte = 20f;
    public float passoProcuraPonte = 1f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;

        if (PlayerPositionManager.HasSavedPosition)
        {
            transform.position = PlayerPositionManager.GetPosition();
            PlayerPositionManager.HasSavedPosition = false;
            Debug.Log("Posição restaurada: " + transform.position);
        }

        posicaoAlvo = transform.position;
        AtualizarSprite(ultimaDirecao, false, true);
    }

    void Update()
    {
        ProcessarInput();
        ProcessarMovimento();
        AtualizarAnimacao();
        
        // MOVIMENTO COM ALVO - CORRIGIDO
        if (alvo != null)
        {
            // Calcular distância ao alvo
            float distanciaAoAlvo = Vector2.Distance(transform.position, alvo.position);
            
            // Se estamos suficientemente perto, parar
            if (distanciaAoAlvo <= distanciaParagem)
            {
                estaAMover = false;
                velocidadeAtual = 0f;
                LimparAlvo();
                return;
            }
            
            // Calcular direção e mover
            Vector2 direcao = (alvo.position - transform.position).normalized;
            direcaoMovimento = direcao;
            
            // Aplicar aceleração/desaceleração
            float velocidadeDesejada = velocidadeMovimento;
            if (distanciaAoAlvo < 1f)
            {
                velocidadeDesejada = Mathf.Lerp(0f, velocidadeMovimento, distanciaAoAlvo);
            }
            
            float accel = velocidadeDesejada > velocidadeAtual ? aceleracao : desaceleracao;
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, accel * Time.deltaTime);
            
            transform.position += (Vector3)(direcaoMovimento * velocidadeAtual * Time.deltaTime);
        }
    }

    void ProcessarInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // Se há um alvo ativo, cancelá-lo ao clicar com o botão direito
            if (alvo != null)
            {
                LimparAlvo();
            }
            
            Vector3 ratoMundo = cam.ScreenToWorldPoint(Input.mousePosition);
            ratoMundo.z = 0;

            if (EstaNoChao(ratoMundo) || EstaNaPonte(ratoMundo))
            {
                bool cruzaAgua = CaminhoCruzaAgua(transform.position, ratoMundo);

                pontosCaminho.Clear();

                if (cruzaAgua)
                {
                    Vector3? pontoPonte = EncontrarPonteMaisProxima(transform.position);
                    if (pontoPonte.HasValue)
                    {
                        pontosCaminho.Add(pontoPonte.Value);
                        pontosCaminho.Add(ratoMundo);
                        posicaoAlvo = pontosCaminho[0];
                        estaAMover = true;
                    }
                    else
                    {
                        Debug.Log("O caminho cruza água e não foi encontrada nenhuma ponte próxima.");
                        estaAMover = false;
                    }
                }
                else
                {
                    pontosCaminho.Add(ratoMundo);
                    posicaoAlvo = pontosCaminho[0];
                    estaAMover = true;
                }

                if (estaAMover && prefabMarcadorClique != null)
                {
                    GameObject marcador = Instantiate(prefabMarcadorClique, pontosCaminho.Last(), Quaternion.identity);
                    Destroy(marcador, 0.6f);
                }
            }
        }
    }

    // MÉTODOS PARA ALVO - CORRIGIDOS
    public void DefinirAlvo(Transform novoAlvo)
    {
        LimparAlvo();
        alvo = novoAlvo;
        pontosCaminho.Clear();
        estaAMover = true;
    }

    public void DefinirAlvo(Vector3 posicaoAlvo)
    {
        LimparAlvo();
        alvoTemporario = new GameObject("AlvoTemporario");
        alvoTemporario.transform.position = posicaoAlvo;
        alvo = alvoTemporario.transform;
        pontosCaminho.Clear();
        estaAMover = true;
    }

    public void LimparAlvo()
    {
        if (alvoTemporario != null)
        {
            Destroy(alvoTemporario);
            alvoTemporario = null;
        }
        alvo = null;
    }

    void ProcessarMovimento()
    {
        // Se há um alvo ativo, o movimento é processado no Update()
        if (alvo != null) return;
        
        if (!estaAMover || pontosCaminho.Count == 0) return;

        Vector3 alvoAtual = pontosCaminho[0];
        Vector3 paraAlvo = alvoAtual - transform.position;
        float dist = paraAlvo.magnitude;
        Vector3 dir = paraAlvo.normalized;

        // Prever próximo passo e evitar água
        Vector3 proximaPos = transform.position + dir * velocidadeMovimento * Time.deltaTime;
        if (EstaNaAgua(proximaPos) && !EstaNaPonte(proximaPos))
        {
            Vector3? pontoPonte = EncontrarPonteMaisProxima(transform.position);
            if (pontoPonte.HasValue)
            {
                pontosCaminho.Insert(0, pontoPonte.Value);
                alvoAtual = pontosCaminho[0];
                dir = (alvoAtual - transform.position).normalized;
            }
            else
            {
                estaAMover = false;
                velocidadeAtual = 0f;
                return;
            }
        }

        // Controlo de velocidade
        float velocidadeDesejada = velocidadeMovimento;
        if (dist < 0.5f)
        {
            velocidadeDesejada = Mathf.Lerp(0f, velocidadeMovimento, dist / 0.5f);
        }

        float accel = velocidadeDesejada > velocidadeAtual ? aceleracao : desaceleracao;
        velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, accel * Time.deltaTime);

        direcaoMovimento = dir;
        transform.position += (Vector3)(direcaoMovimento * velocidadeAtual * Time.deltaTime);

        // Verificar chegada ao ponto
        if (dist < Mathf.Max(distanciaParagem, limiteAlcancePonto))
        {
            pontosCaminho.RemoveAt(0);
            if (pontosCaminho.Count > 0)
            {
                posicaoAlvo = pontosCaminho[0];
            }
            else
            {
                estaAMover = false;
                velocidadeAtual = 0f;
            }
        }
    }

    bool CaminhoCruzaAgua(Vector3 inicio, Vector3 fim)
    {
        float distanciaTotal = Vector2.Distance(inicio, fim);
        if (distanciaTotal < 0.01f) return false;

        float passo = 0.12f;
        bool viuChao = false;
        bool viuAguaAposChao = false;

        int passos = Mathf.CeilToInt(distanciaTotal / passo);
        for (int i = 0; i <= passos; i++)
        {
            float t = (float)i / (float)passos;
            Vector3 p = Vector3.Lerp(inicio, fim, t);

            bool c = EstaNoChao(p) || EstaNaPonte(p);
            bool a = EstaNaAgua(p) && !EstaNaPonte(p);

            if (c && !viuChao)
                viuChao = true;

            if (viuChao && a)
                viuAguaAposChao = true;

            if (viuAguaAposChao && c)
            {
                return true;
            }
        }

        return false;
    }

    Vector3? EncontrarPonteMaisProxima(Vector3 de)
    {
        float raio = passoProcuraPonte;
        Collider2D[] colisoes = null;
        
        while (raio <= raioMaximoProcuraPonte)
        {
            colisoes = Physics2D.OverlapCircleAll(de, raio, camadaPonte);
            if (colisoes != null && colisoes.Length > 0)
                break;

            raio += passoProcuraPonte;
        }

        if (colisoes == null || colisoes.Length == 0)
            return null;

        var ordenado = colisoes.OrderBy(h => Vector2.Distance(de, h.bounds.center));
        foreach (var h in ordenado)
        {
            Vector3 candidato = h.bounds.center;
            Vector3 pontoMaisProximo = h.ClosestPoint(de);
            if (pontoMaisProximo != Vector3.zero)
                candidato = pontoMaisProximo;

            if (!CaminhoCruzaAgua(de, candidato))
            {
                return candidato;
            }
        }

        return null;
    }

    void AtualizarAnimacao()
    {
        if ((direcaoMovimento.magnitude > 0.1f && estaAMover) || alvo != null)
        {
            temporizadorAnim += Time.deltaTime;
            if (temporizadorAnim >= 0.2f)
            {
                temporizadorAnim = 0f;
                alternarAnim = !alternarAnim;
            }

            AtualizarSprite(direcaoMovimento, alternarAnim);
            ultimaDirecao = direcaoMovimento;
        }
        else
        {
            AtualizarSprite(ultimaDirecao, false, true);
        }
    }

    void AtualizarSprite(Vector2 direcao, bool alternar, bool idle = false)
    {
        if (sr == null) return;

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        if (angulo < 0) angulo += 360;

        Sprite escolhido = frenteDireita_Idle;

        // CORRIGIDO: Melhor distribuição das direções
        if (angulo >= 337.5f || angulo < 22.5f)        // Direita
            escolhido = idle ? frenteDireita_Idle : (alternar ? frenteDireita_L : frenteDireita_R);
        else if (angulo >= 22.5f && angulo < 67.5f)    // Cima-Direita
            escolhido = idle ? trasDireita_Idle : (alternar ? trasDireita_L : trasDireita_R);
        else if (angulo >= 67.5f && angulo < 112.5f)   // Cima
            escolhido = idle ? trasDireita_Idle : (alternar ? trasDireita_L : trasDireita_R);
        else if (angulo >= 112.5f && angulo < 157.5f)  // Cima-Esquerda
            escolhido = idle ? trasEsquerda_Idle : (alternar ? trasEsquerda_L : trasEsquerda_R);
        else if (angulo >= 157.5f && angulo < 202.5f)  // Esquerda
            escolhido = idle ? frenteEsquerda_Idle : (alternar ? frenteEsquerda_L : frenteEsquerda_R);
        else if (angulo >= 202.5f && angulo < 247.5f)  // Baixo-Esquerda
            escolhido = idle ? frenteEsquerda_Idle : (alternar ? frenteEsquerda_L : frenteEsquerda_R);
        else if (angulo >= 247.5f && angulo < 292.5f)  // Baixo
            escolhido = idle ? frenteDireita_Idle : (alternar ? frenteDireita_L : frenteDireita_R);
        else if (angulo >= 292.5f && angulo < 337.5f)  // Baixo-Direita
            escolhido = idle ? frenteDireita_Idle : (alternar ? frenteDireita_L : frenteDireita_R);

        sr.sprite = escolhido;
    }

    bool EstaNaAgua(Vector3 posicao)
    {
        return Physics2D.OverlapCircle(posicao, 0.2f, camadaAgua) != null;
    }

    bool EstaNoChao(Vector3 posicao)
    {
        return Physics2D.OverlapCircle(posicao, 0.2f, camadaChao) != null;
    }

    bool EstaNaPonte(Vector3 posicao)
    {
        return Physics2D.OverlapCircle(posicao, 0.2f, camadaPonte) != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(posicaoAlvo, 0.1f);
        
        // Desenhar pontos do caminho
        Gizmos.color = Color.yellow;
        foreach (var ponto in pontosCaminho)
        {
            Gizmos.DrawWireCube(ponto, Vector3.one * 0.15f);
        }
    }

    void OnDestroy()
    {
        // Limpar alvos temporários ao destruir o objeto
        if (alvoTemporario != null)
        {
            Destroy(alvoTemporario);
        }
    }
}