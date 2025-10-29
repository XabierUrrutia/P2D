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

    [Header("Cooldown de Movimiento")]
    public float clickCooldown = 1.5f;
    private float lastClickTime = 0f;
    private bool canClick = true;

    [Header("Configuración de Puentes")]
    public float distanciaParagemPonte = 0.15f;
    public float tempoMaximoPonte = 5f;
    private float tempoNoPonte = 0f;

    [Header("Configuración de Colisiones")]
    public float raioDeteccao = 0.25f;
    public float margemSeguranca = 0.1f;

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

    // NUEVO: Para controlar el estado del movimiento por puentes
    private bool usandoPuente = false;
    private Vector3 pontoPuenteAtual;

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

        // Actualizar cooldown
        if (!canClick && Time.time - lastClickTime >= clickCooldown)
        {
            canClick = true;
        }

        // Detección de atasco en puente
        if (estaAMover && EstaNaPonte(transform.position))
        {
            tempoNoPonte += Time.deltaTime;
            if (tempoNoPonte > tempoMaximoPonte)
            {
                Debug.LogWarning("Personaje atascado en puente - reiniciando movimiento");
                ReiniciarMovimento();
            }
        }
        else
        {
            tempoNoPonte = 0f;
        }
    }

    void ProcessarInput()
    {
        if (Input.GetMouseButtonDown(1) && canClick)
        {
            // Aplicar cooldown
            canClick = false;
            lastClickTime = Time.time;

            // Si há um alvo ativo, cancelá-lo ao clicar com o botão direito
            if (alvo != null)
            {
                LimparAlvo();
            }

            Vector3 ratoMundo = cam.ScreenToWorldPoint(Input.mousePosition);
            ratoMundo.z = 0;

            if (EstaNoChao(ratoMundo) || EstaNaPonte(ratoMundo))
            {
                bool cruzaAgua = VerificarSeCaminhoCruzaAgua(transform.position, ratoMundo);

                pontosCaminho.Clear();
                usandoPuente = false;

                if (cruzaAgua)
                {
                    Debug.Log("Camino cruza agua, buscando puente...");
                    Vector3? pontoPonte = EncontrarMelhorPonteParaDestino(transform.position, ratoMundo);
                    if (pontoPonte.HasValue)
                    {
                        Debug.Log($"Encontrada ponte en: {pontoPonte.Value}");

                        // NUEVO: Verificar que el camino al puente sea seguro
                        if (!CaminhoTemAgua(transform.position, pontoPonte.Value))
                        {
                            pontosCaminho.Add(pontoPonte.Value);
                            pontosCaminho.Add(ratoMundo);
                            posicaoAlvo = pontosCaminho[0];
                            estaAMover = true;
                            usandoPuente = true;
                            pontoPuenteAtual = pontoPonte.Value;

                            Debug.Log($"Ruta establecida: Personaje -> Puente ({pontoPonte.Value}) -> Destino ({ratoMundo})");
                        }
                        else
                        {
                            Debug.LogWarning("El camino al puente también tiene agua, buscando alternativa...");
                            // Buscar un puente con camino seguro
                            Vector3? ponteAlternativo = EncontrarPonteComCaminhoSeguro(transform.position, ratoMundo);
                            if (ponteAlternativo.HasValue)
                            {
                                pontosCaminho.Add(ponteAlternativo.Value);
                                pontosCaminho.Add(ratoMundo);
                                posicaoAlvo = pontosCaminho[0];
                                estaAMover = true;
                                usandoPuente = true;
                                pontoPuenteAtual = ponteAlternativo.Value;
                            }
                            else
                            {
                                Debug.LogError("No se encontró un puente con camino seguro");
                                estaAMover = false;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("O caminho cruza água e não foi encontrada nenhuma ponte acessível.");
                        estaAMover = false;
                    }
                }
                else
                {
                    pontosCaminho.Add(ratoMundo);
                    posicaoAlvo = pontosCaminho[0];
                    estaAMover = true;
                    usandoPuente = false;
                }

                if (estaAMover && prefabMarcadorClique != null)
                {
                    // Mostrar marcadores para todos los puntos del camino
                    foreach (var ponto in pontosCaminho)
                    {
                        GameObject marcador = Instantiate(prefabMarcadorClique, ponto, Quaternion.identity);
                        Destroy(marcador, 1.0f);
                    }
                }
            }
        }
    }

    void ProcessarMovimento()
    {
        // Si há um alvo ativo, o movimento é processado no Update()
        if (alvo != null) return;

        if (!estaAMover || pontosCaminho.Count == 0) return;

        Vector3 alvoAtual = pontosCaminho[0];
        Vector3 paraAlvo = alvoAtual - transform.position;
        float dist = paraAlvo.magnitude;
        Vector3 dir = paraAlvo.normalized;

        // Ajustar la distancia de parada según el terreno
        float distanciaParagemAjustada = EstaNaPonte(transform.position) || EstaNaPonte(alvoAtual) ?
            distanciaParagemPonte : distanciaParagem;

        // NUEVO: Verificación más estricta de seguridad durante el movimiento
        if (usandoPuente && pontosCaminho.Count > 1 && pontosCaminho[0] == pontoPuenteAtual)
        {
            // Estamos yendo al puente - verificar que no nos salgamos del camino
            Vector3 proximaPos = transform.position + dir * velocidadeMovimento * Time.deltaTime;

            // Si nos estamos desviando hacia el agua, corregir la dirección
            if (EstaNaAgua(proximaPos) && !EstaNaPonte(proximaPos))
            {
                Debug.LogWarning("Desviándose hacia el agua, corrigiendo dirección...");
                // Recalcular dirección directamente al puente
                dir = (pontoPuenteAtual - transform.position).normalized;
                alvoAtual = pontoPuenteAtual;
            }
        }

        // Controlo de velocidad
        float velocidadeDesejada = velocidadeMovimento;
        if (dist < 1f)
        {
            velocidadeDesejada = Mathf.Lerp(0f, velocidadeMovimento, dist);
        }

        float accel = velocidadeDesejada > velocidadeAtual ? aceleracao : desaceleracao;
        velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, accel * Time.deltaTime);

        direcaoMovimento = dir;
        transform.position += (Vector3)(direcaoMovimento * velocidadeAtual * Time.deltaTime);

        // Verificar chegada ao punto
        if (dist < Mathf.Max(distanciaParagemAjustada, limiteAlcancePonto))
        {
            Debug.Log($"Llegado al punto: {alvoAtual}. Puntos restantes: {pontosCaminho.Count - 1}");

            // NUEVO: Si llegamos al puente, actualizar estado
            if (usandoPuente && pontosCaminho[0] == pontoPuenteAtual)
            {
                Debug.Log("¡Llegado al puente! Continuando hacia destino final...");
                usandoPuente = false;
            }

            pontosCaminho.RemoveAt(0);
            if (pontosCaminho.Count > 0)
            {
                posicaoAlvo = pontosCaminho[0];
            }
            else
            {
                estaAMover = false;
                velocidadeAtual = 0f;
                usandoPuente = false;
                Debug.Log("¡Destino alcanzado!");
            }
        }
    }

    // NUEVO: Método para encontrar puentes con camino seguro
    Vector3? EncontrarPonteComCaminhoSeguro(Vector3 inicio, Vector3 destino)
    {
        Collider2D[] todasPontes = Physics2D.OverlapCircleAll(inicio, raioMaximoProcuraPonte, camadaPonte);

        if (todasPontes == null || todasPontes.Length == 0)
            return null;

        // Ordenar por proximidad
        var pontesOrdenadas = todasPontes.OrderBy(p => Vector2.Distance(inicio, p.transform.position));

        foreach (var ponte in pontesOrdenadas)
        {
            Vector3 pontoPonte = ponte.ClosestPoint(inicio);

            // Verificar que el camino al puente sea seguro
            if (!CaminhoTemAgua(inicio, pontoPonte) && EsPontoAcessivel(pontoPonte))
            {
                // Verificar que del puente al destino también sea seguro
                if (!CaminhoTemAgua(pontoPonte, destino))
                {
                    Debug.Log($"Puente seguro encontrado en: {pontoPonte}");
                    return pontoPonte;
                }
            }
        }

        return null;
    }

    void ReiniciarMovimento()
    {
        estaAMover = false;
        velocidadeAtual = 0f;
        pontosCaminho.Clear();
        LimparAlvo();
        tempoNoPonte = 0f;
        usandoPuente = false;
    }

    // MÉTODO PÚBLICO PARA VERIFICAR SI PUEDE MOVERSE
    public bool CanMove()
    {
        return canClick;
    }

    // MÉTODO PÚBLICO PARA OBTENER TIEMPO RESTANTE DEL COOLDOWN
    public float GetRemainingCooldown()
    {
        if (canClick) return 0f;
        return Mathf.Max(0f, clickCooldown - (Time.time - lastClickTime));
    }

    // MÉTODO PÚBLICO PARA FORZAR MOVIMIENTO (ignora cooldown)
    public void ForceMove(Vector3 position)
    {
        DefinirAlvo(position);
    }

    // MÉTODOS PARA ALVO
    public void DefinirAlvo(Transform novoAlvo)
    {
        LimparAlvo();
        alvo = novoAlvo;
        pontosCaminho.Clear();
        estaAMover = true;
        usandoPuente = false;
    }

    public void DefinirAlvo(Vector3 posicaoAlvo)
    {
        LimparAlvo();
        alvoTemporario = new GameObject("AlvoTemporario");
        alvoTemporario.transform.position = posicaoAlvo;
        alvo = alvoTemporario.transform;
        pontosCaminho.Clear();
        estaAMover = true;
        usandoPuente = false;
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

    // MÉTODO MEJORADO - Detección más precisa de cruce con agua
    bool VerificarSeCaminhoCruzaAgua(Vector3 inicio, Vector3 fim)
    {
        float distanciaTotal = Vector2.Distance(inicio, fim);
        if (distanciaTotal < 0.01f) return false;

        int numPontos = Mathf.CeilToInt(distanciaTotal / 0.1f);
        bool encontrouAgua = false;

        for (int i = 0; i <= numPontos; i++)
        {
            float t = (float)i / (float)numPontos;
            Vector3 ponto = Vector3.Lerp(inicio, fim, t);

            // Si encontramos agua y no estamos en un puente
            if (EstaNaAgua(ponto) && !EstaNaPonte(ponto))
            {
                encontrouAgua = true;
            }

            // Si encontramos tierra después de agua, entonces el camino cruza agua
            if (encontrouAgua && (EstaNoChao(ponto) || EstaNaPonte(ponto)))
            {
                return true;
            }
        }

        return false;
    }

    // MÉTODO MEJORADO - Encuentra el mejor puente considerando la dirección del destino
    Vector3? EncontrarMelhorPonteParaDestino(Vector3 inicio, Vector3 destino)
    {
        // Dirección hacia el destino
        Vector3 direcaoDestino = (destino - inicio).normalized;

        // Buscar todos los puentes en el radio máximo
        Collider2D[] todasPontes = Physics2D.OverlapCircleAll(inicio, raioMaximoProcuraPonte, camadaPonte);

        if (todasPontes == null || todasPontes.Length == 0)
            return null;

        // Filtrar puentes accesibles
        List<Collider2D> pontesAcessiveis = new List<Collider2D>();
        foreach (var ponte in todasPontes)
        {
            Vector3 pontoPonte = ponte.ClosestPoint(inicio);
            if (EsPontoAcessivel(pontoPonte) && !CaminhoTemAgua(inicio, pontoPonte))
            {
                pontesAcessiveis.Add(ponte);
            }
        }

        if (pontesAcessiveis.Count == 0)
            return null;

        // Ordenar puentes por:
        // 1. Proximidad al inicio
        // 2. Dirección hacia el destino
        var pontesOrdenadas = pontesAcessiveis.OrderBy(p =>
        {
            Vector3 pontoPonte = p.ClosestPoint(inicio);
            float distancia = Vector3.Distance(inicio, pontoPonte);
            Vector3 direcaoPonte = (pontoPonte - inicio).normalized;
            float similaridadeDirecao = Vector3.Dot(direcaoDestino, direcaoPonte);

            // Combinar distancia y dirección (preferir puentes en la dirección del destino)
            return distancia * (2f - Mathf.Clamp01(similaridadeDirecao));
        });

        // Devolver el punto del primer puente accesible
        foreach (var ponte in pontesOrdenadas)
        {
            Vector3 pontoPonte = ponte.ClosestPoint(inicio);
            if (EsPontoAcessivel(pontoPonte))
            {
                Debug.Log($"Puente seleccionado en: {pontoPonte}, distancia: {Vector3.Distance(inicio, pontoPonte)}");
                return pontoPonte;
            }
        }

        return null;
    }

    // Verifica si hay agua entre dos puntos
    bool CaminhoTemAgua(Vector3 inicio, Vector3 fim)
    {
        float distancia = Vector3.Distance(inicio, fim);
        int numChecks = Mathf.CeilToInt(distancia / 0.1f);

        for (int i = 0; i <= numChecks; i++)
        {
            float t = (float)i / (float)numChecks;
            Vector3 ponto = Vector3.Lerp(inicio, fim, t);

            if (EstaNaAgua(ponto) && !EstaNaPonte(ponto))
                return true;
        }

        return false;
    }

    bool EsPontoAcessivel(Vector3 ponto)
    {
        // Verificar que el punto está en el puente y no en agua
        return EstaNaPonte(ponto) && !EstaNaAgua(ponto);
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

        // Distribuição das direções
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
        Collider2D agua = Physics2D.OverlapCircle(posicao, raioDeteccao, camadaAgua);
        return agua != null;
    }

    bool EstaNoChao(Vector3 posicao)
    {
        Collider2D chao = Physics2D.OverlapCircle(posicao, raioDeteccao, camadaChao);
        return chao != null;
    }

    bool EstaNaPonte(Vector3 posicao)
    {
        Collider2D ponte = Physics2D.OverlapCircle(posicao, raioDeteccao, camadaPonte);
        if (ponte != null)
        {
            // Verificar que estamos realmente sobre el puente, no solo cerca
            Vector3 pontoNoPuente = ponte.ClosestPoint(posicao);
            float distancia = Vector2.Distance(posicao, pontoNoPuente);
            return distancia <= raioDeteccao + margemSeguranca;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        // Punto objetivo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(posicaoAlvo, 0.1f);

        // Puntos del camino
        Gizmos.color = Color.yellow;
        foreach (var ponto in pontosCaminho)
        {
            Gizmos.DrawWireCube(ponto, Vector3.one * 0.15f);
        }

        // Radio de detección
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, raioDeteccao);

        // Próxima posición prevista
        if (estaAMover && pontosCaminho.Count > 0)
        {
            Vector3 dir = (pontosCaminho[0] - transform.position).normalized;
            Vector3 proximaPos = transform.position + dir * velocidadeMovimento * Time.deltaTime;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(proximaPos, 0.1f);
        }

        // NUEVO: Dibujar línea del camino actual
        if (pontosCaminho.Count > 0)
        {
            Gizmos.color = Color.white;
            Vector3 anterior = transform.position;
            foreach (var ponto in pontosCaminho)
            {
                Gizmos.DrawLine(anterior, ponto);
                anterior = ponto;
            }
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