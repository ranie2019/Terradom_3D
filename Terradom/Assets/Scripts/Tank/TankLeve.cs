using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TankLeve : MonoBehaviour
{
    private enum EstadoTank
    {
        Patrulhando,
        DesviandoObstaculo,
        EmCombate,
        Re,
        Morto
    }

    [Header("Movimento fisico do veiculo")]
    [SerializeField] private float velocidadeFrente = 4f;
    [SerializeField] private float aceleracao = 6f;
    [SerializeField] private float distanciaEntreEixos = 3f;
    [SerializeField] private float anguloMaximoDirecao = 32f;
    [SerializeField] private float suavidadeDirecao = 3.5f;

    [Header("Patrulha livre")]
    [SerializeField] private float tempoContagem = 5f;        // contador regressivo para trocar direção
    [SerializeField] private float anguloMinimoCurva = 30f;   // ângulo mínimo ao trocar direção
    [SerializeField] private bool alternarLadoDaCurva = true; // alterna esquerda/direita a cada troca

    [Header("IA / Modulos")]
    [SerializeField] private bool buscarModulosAutomaticamente = true;
    [SerializeField] private TankVisao tankVisao;
    [SerializeField] private TankAtaque tankAtaque;
    [SerializeField] private Roda controleRodas;
    [SerializeField] private Vida vidaTank;

    [Header("IA / Visao / Ataque")]
    [SerializeField] private bool pararQuandoTemAlvoNaVisao = true;
    [SerializeField] private bool pararParaAlvoTerrestre = true;
    [SerializeField] private bool pararParaAlvoAereo = true;
    [SerializeField] private float velocidadeDuranteCombate = 0f;
    [SerializeField] private float velocidadeMinimaDuranteCombate = 3f;
    [SerializeField] private float distanciaPreferidaCombate = 28f;
    [SerializeField] private float freioAoEncontrarInimigo = 18f;
    [SerializeField] private float tempoContinuarParadoAposPerderAlvo = 0.15f;
    [SerializeField] private bool tankLeveGerenciaAtaque = true;
    [SerializeField] private bool ativarTankAtaqueSomenteComAlvo = false;

    [Header("Vida / gerenciamento pelo TankLeve")]
    [SerializeField] private bool tankLeveGerenciaVida = true;
    [SerializeField] private bool pararTankQuandoMorrer = true;
    [SerializeField] private bool desativarVisaoQuandoMorrer = true;
    [SerializeField] private bool desativarAtaqueQuandoMorrer = true;
    [SerializeField] private bool tornarRigidbodyKinematicAoMorrer = false;
    [SerializeField] private bool destruirTankAoMorrer = false;
    [SerializeField] private float tempoParaDestruirAposMorrer = 1.5f;

    [Header("Sensor de desvio")]
    [SerializeField] private bool usarSensorDesvio = true;
    [SerializeField] private LayerMask camadasDetectaveis = ~0;
    [SerializeField] private bool detectarTriggers = false;
    [SerializeField] private float distanciaSensorFrontal = 8f;
    [SerializeField] private float raioSensorFrontal = 0.55f;
    [SerializeField] private float alturaSensor = 0.8f;
    [SerializeField] private float offsetFrenteSensor = 1.2f;
    [SerializeField] private float anguloSensoresLaterais = 35f;
    [SerializeField] private float tempoManterDesvio = 1.3f;
    [SerializeField] private float tempoRetoAposDesvio = 0.9f;
    [SerializeField] private bool reduzirVelocidadeAoDesviar = true;
    [SerializeField] private float velocidadeDuranteDesvio = 2.8f;
    [SerializeField] private float velocidadeMinimaDesvio = 1.2f;
    [SerializeField] private float distanciaComecarReduzir = 4f;
    [SerializeField] private float margemEscolhaLado = 0.35f;
    [SerializeField] private bool ignorarChaoNoSensor = true;
    [SerializeField] private float normalMinimaParaChao = 0.55f;
    [SerializeField] private string[] tagsIgnoradasNoSensor = { "Terrain" }; // objetos com essas tags nunca bloqueiam o sensor

    [Header("Anti-stuck (re automatica)")]
    [SerializeField] private bool usarAntiStuck = true;
    [SerializeField] private float tempoParaDetectarStuck = 0.8f;   // segundos parado para acionar a ré
    [SerializeField] private float velocidadeRe = 2.5f;              // velocidade da ré
    [SerializeField] private float duracaoRe = 0.7f;                 // quanto tempo fica dando ré
    [SerializeField] private float anguloViradaAposRe = 45f;         // ângulo de curva forçado após a ré
    [SerializeField] private float limiarMovimentoStuck = 0.15f;     // abaixo disso considera "parado"

    [Header("Colisao fisica")]
    [SerializeField] private bool configurarRigidbodyAutomaticamente = true;
    [SerializeField] private bool congelarTombamento = true;
    [SerializeField] private bool trocarCurvaAoBater = true;
    [SerializeField] private bool ignorarChaoNaColisao = true;
    [SerializeField] private float tempoManterDesvioAposColisao = 1.2f;

    [Header("Rodas - giro Z e direcao Y")]
    [SerializeField] private float rotacaoRodasPorUnidadeVelocidade = 160f;
    [SerializeField] private bool inverterDirecaoVisualRodasDianteiras = false;
    [SerializeField] private float multiplicadorVisualDirecaoRodas = 1f;
    [SerializeField] private float suavidadeVisualDirecaoRodas = 12f;
    [SerializeField] private float limiteCentralizarVisualRodas = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool desenharFrenteNoEditor = true;
    [SerializeField] private bool desenharSensorNoEditor = true;
    [SerializeField] private bool desenharAlvoNoEditor = true;

    private Rigidbody rb;
    private EstadoTank estadoAtual = EstadoTank.Patrulhando;
    private float velocidadeAtual;
    private float anguloDirecaoAtual;
    private float anguloDirecaoAlvo;
    private float anguloVisualDirecaoRodas;
    private float contagemPatrulha;   // contador regressivo visível no Inspector via OnGUI
    private float manterDesvioAte;
    private float manterCombateAte;
    private float distanciaObstaculoAtual;
    private int ultimoLadoCurva = 1;
    private int ladoDesvioAtual = 1;
    private bool sensorDetectandoObstaculo;
    private bool desvioAtivo;
    private bool emCombate;
    private bool temAlvoNaVisao;
    private bool tankMorto;
    private Vector3 pontoSensorDetectado;
    private Vector3 normalSensorDetectado;
    private bool ladoDesvioTravado = false;  // impede troca de lado enquanto desviando
    private readonly Collider[] bufferSobreposicaoSensor = new Collider[32];

    // Anti-stuck
    private float tempoSemMover = 0f;
    private bool emRe = false;
    private float reTerminaAte = 0f;
    private int ladoCurvaAposRe = 1;

    public bool EmCombate => emCombate;
    public bool TemAlvoNaVisao => temAlvoNaVisao;
    public bool EstaMorto => tankMorto;
    public Transform AlvoAtual => tankVisao != null ? tankVisao.AlvoAtual : null;
    public Vida VidaDoTank => vidaTank;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        BuscarModulosDoTank();
        AplicarConfiguracaoRigidbody();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        BuscarModulosDoTank();
        AplicarConfiguracaoRigidbody();
        contagemPatrulha = tempoContagem;
        anguloDirecaoAlvo = 0f;
    }

    private void FixedUpdate()
    {
        BuscarModulosDoTank();
        AtualizarEstadoVida();

        if (tankMorto)
        {
            ManterTankParadoQuandoMorto();
            return;
        }

        AtualizarIACombate();

        AtualizarAntiStuck();

        if (emRe)
        {
            estadoAtual = EstadoTank.Re;
        }
        else if (emCombate)
        {
            PrepararTankParaCombate();
        }
        else
        {
            AtualizarSensorDesvio();
            if (!desvioAtivo) AtualizarPatrulhaLivre();
        }

        AtualizarMovimentoFisico(Time.fixedDeltaTime);
        AtualizarRodasSincronizadasComVeiculo(Time.fixedDeltaTime);
        TankLearningDatabase.SalvarSeNecessario();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TratarColisaoFisica(collision);
    }

    private void OnDisable()
    {
        PararECentralizarRodas();
        if (rb != null) DefinirVelocidadeRigidbody(Vector3.zero);
        anguloVisualDirecaoRodas = 0f;
        TankLearningDatabase.Salvar();
    }

    private void BuscarModulosDoTank()
    {
        if (!buscarModulosAutomaticamente) return;
        if (controleRodas == null) controleRodas = GetComponentInChildren<Roda>();
        if (tankVisao == null) tankVisao = GetComponent<TankVisao>();
        if (tankAtaque == null) tankAtaque = GetComponent<TankAtaque>();
        if (vidaTank == null) vidaTank = GetComponent<Vida>();
    }

    private void AplicarConfiguracaoRigidbody()
    {
        if (!configurarRigidbodyAutomaticamente || rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (congelarTombamento)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void AtualizarEstadoVida()
    {
        if (!tankLeveGerenciaVida || tankMorto || vidaTank == null) return;
        if (VidaEstaZeradaOuMorta()) MorrerTank();
    }

    private bool VidaEstaZeradaOuMorta()
    {
        object valorMorto;
        if (TentarLerMembroVida("morto", out valorMorto) ||
            TentarLerMembroVida("estaMorto", out valorMorto) ||
            TentarLerMembroVida("EstaMorto", out valorMorto) ||
            TentarLerMembroVida("Morto", out valorMorto))
        {
            if (valorMorto is bool mortoBool) return mortoBool;
        }

        object valorVida;
        if (TentarLerMembroVida("vidaAtual", out valorVida) ||
            TentarLerMembroVida("VidaAtual", out valorVida) ||
            TentarLerMembroVida("vida", out valorVida) ||
            TentarLerMembroVida("Vida", out valorVida) ||
            TentarLerMembroVida("hp", out valorVida) ||
            TentarLerMembroVida("HP", out valorVida) ||
            TentarLerMembroVida("GetVidaAtual", out valorVida) ||
            TentarLerMembroVida("ObterVidaAtual", out valorVida))
        {
            return ValorNumericoEhZeroOuMenor(valorVida);
        }

        return false;
    }

    private bool TentarLerMembroVida(string nomeMembro, out object valor)
    {
        valor = null;
        if (vidaTank == null || string.IsNullOrWhiteSpace(nomeMembro)) return false;

        System.Type tipo = vidaTank.GetType();
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        System.Reflection.FieldInfo campo = tipo.GetField(nomeMembro, flags);
        if (campo != null)
        {
            valor = campo.GetValue(vidaTank);
            return true;
        }

        System.Reflection.PropertyInfo propriedade = tipo.GetProperty(nomeMembro, flags);
        if (propriedade != null && propriedade.CanRead)
        {
            valor = propriedade.GetValue(vidaTank, null);
            return true;
        }

        System.Reflection.MethodInfo metodo = tipo.GetMethod(nomeMembro, flags, null, System.Type.EmptyTypes, null);
        if (metodo != null && metodo.ReturnType != typeof(void))
        {
            valor = metodo.Invoke(vidaTank, null);
            return true;
        }

        return false;
    }

    private bool ValorNumericoEhZeroOuMenor(object valor)
    {
        if (valor == null) return false;
        try
        {
            float numero = System.Convert.ToSingle(valor);
            return numero <= 0f;
        }
        catch
        {
            return false;
        }
    }

    public void ReceberDano(int dano)
    {
        EncaminharDanoParaVida(dano, null);
    }

    // Vida envia TakeDamage apos aplicar dano. Alem da vida, isso alimenta
    // o ajuste persistente de evasao do perfil desta equipe.
    public void TakeDamage(int dano)
    {
        if (dano > 0)
            TankLearningDatabase.RegistrarDanoRecebido(ObterEquipe(), distanciaPreferidaCombate);
    }

    public void RegistrarResultadoDisparo(bool acertou, float distancia)
    {
        TankLearningDatabase.RegistrarDisparo(ObterEquipe(), acertou, distancia, distanciaPreferidaCombate);
    }

    public void ReceberDano(int dano, GameObject atacante)
    {
        EncaminharDanoParaVida(dano, atacante);
    }

    public void ReceberDano(int dano, Component atacante)
    {
        EncaminharDanoParaVida(dano, atacante != null ? atacante.gameObject : null);
    }

    private void EncaminharDanoParaVida(int dano, GameObject atacante)
    {
        if (tankMorto || dano <= 0) return;
        BuscarModulosDoTank();
        if (vidaTank == null) return;

        bool chamouMetodo =
            InvocarMetodoVida("ReceberDano", dano, atacante) ||
            InvocarMetodoVida("ReceberDano", dano) ||
            InvocarMetodoVida("TomarDano", dano, atacante) ||
            InvocarMetodoVida("TomarDano", dano) ||
            InvocarMetodoVida("AplicarDano", dano, atacante) ||
            InvocarMetodoVida("AplicarDano", dano);

        if (!chamouMetodo)
            vidaTank.SendMessage("ReceberDano", dano, SendMessageOptions.DontRequireReceiver);

        AtualizarEstadoVida();
    }

    private bool InvocarMetodoVida(string nomeMetodo, params object[] argumentos)
    {
        if (vidaTank == null || string.IsNullOrWhiteSpace(nomeMetodo)) return false;

        System.Type tipo = vidaTank.GetType();
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.MethodInfo[] metodos = tipo.GetMethods(flags);

        for (int i = 0; i < metodos.Length; i++)
        {
            System.Reflection.MethodInfo metodo = metodos[i];
            if (metodo.Name != nomeMetodo) continue;
            if (metodo.GetParameters().Length != argumentos.Length) continue;

            try
            {
                metodo.Invoke(vidaTank, argumentos);
                return true;
            }
            catch { }
        }

        return false;
    }

    private void MorrerTank()
    {
        if (tankMorto) return;

        tankMorto = true;
        emCombate = false;
        temAlvoNaVisao = false;
        desvioAtivo = false;
        sensorDetectandoObstaculo = false;
        emRe = false;
        tempoSemMover = 0f;
        estadoAtual = EstadoTank.Morto;

        velocidadeAtual = 0f;
        anguloDirecaoAtual = 0f;
        anguloDirecaoAlvo = 0f;
        anguloVisualDirecaoRodas = 0f;

        PararECentralizarRodas();

        if (rb != null)
        {
            DefinirVelocidadeRigidbody(Vector3.zero);
            rb.angularVelocity = Vector3.zero;

            if (pararTankQuandoMorrer && tornarRigidbodyKinematicAoMorrer)
                rb.isKinematic = true;
        }

        if (desativarVisaoQuandoMorrer && tankVisao != null)
            tankVisao.enabled = false;

        if (desativarAtaqueQuandoMorrer && tankAtaque != null)
            tankAtaque.enabled = false;

        if (destruirTankAoMorrer)
            Destroy(gameObject, Mathf.Max(0f, tempoParaDestruirAposMorrer));
    }

    private void AtualizarAntiStuck()
    {
        if (tankMorto) return;

        // A ré precisa terminar mesmo durante combate ou se o anti-stuck for desligado
        // no Inspector, pois colisões também podem iniciá-la.
        if (emRe)
        {
            if (Time.time >= reTerminaAte)
            {
                emRe = false;
                tempoSemMover = 0f;

                // Força uma curva acentuada para sair da parede
                ultimoLadoCurva = ladoCurvaAposRe;
                anguloDirecaoAlvo = ladoCurvaAposRe * Mathf.Clamp(anguloViradaAposRe, 0f, anguloMaximoDirecao);
                manterDesvioAte = Time.time + Mathf.Max(tempoManterDesvio, 0.5f);
                desvioAtivo = true;
                ladoDesvioTravado = false;
            }
            return;
        }

        if (!usarAntiStuck || emCombate) return;

        // Mede se o tank está se movendo de fato
        float velocidadeReal = Mathf.Abs(CalcularVelocidadeHorizontalRealDoVeiculo());
        bool deveriaMover = velocidadeAtual > limiarMovimentoStuck || desvioAtivo;

        if (deveriaMover && velocidadeReal < limiarMovimentoStuck)
            tempoSemMover += Time.fixedDeltaTime;
        else
            tempoSemMover = 0f;

        if (tempoSemMover >= tempoParaDetectarStuck)
        {
            IniciarRe();
        }
    }

    private void IniciarRe()
    {
        emRe = true;
        reTerminaAte = Time.time + duracaoRe;
        ladoCurvaAposRe = -ultimoLadoCurva;
        tempoSemMover = 0f;

        // Limpa o embalo guardado pelo controlador; caso contrário, a velocidade
        // alta pode consumir toda a janela de ré apenas desacelerando.
        velocidadeAtual = 0f;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocidade = ObterVelocidadeRigidbody();
            DefinirVelocidadeRigidbody(new Vector3(0f, velocidade.y, 0f));
        }

        // Cancela qualquer desvio em curso
        desvioAtivo = false;
        ladoDesvioTravado = false;
        manterDesvioAte = 0f;
        sensorDetectandoObstaculo = false;
    }


    private void ManterTankParadoQuandoMorto()
    {
        estadoAtual = EstadoTank.Morto;
        velocidadeAtual = 0f;
        anguloDirecaoAtual = 0f;
        anguloDirecaoAlvo = 0f;
        anguloVisualDirecaoRodas = 0f;

        PararECentralizarRodas();

        if (rb != null && pararTankQuandoMorrer)
        {
            DefinirVelocidadeRigidbody(Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void AtualizarIACombate()
    {
        BuscarModulosDoTank();

        temAlvoNaVisao = tankVisao != null && tankVisao.TemAlvo;
        bool devePararParaAlvo = temAlvoNaVisao && DevePararParaTipoDoAlvoAtual();

        if (devePararParaAlvo)
            manterCombateAte = Time.time + tempoContinuarParadoAposPerderAlvo;

        emCombate = pararQuandoTemAlvoNaVisao && Time.time <= manterCombateAte;
        estadoAtual = emCombate ? EstadoTank.EmCombate : (desvioAtivo ? EstadoTank.DesviandoObstaculo : EstadoTank.Patrulhando);

        AtualizarEstadoDoTankAtaque();
    }

    private bool DevePararParaTipoDoAlvoAtual()
    {
        if (tankVisao == null || !tankVisao.TemAlvo) return false;

        if (tankVisao.TipoAlvoAtual == TankVisao.TipoAlvoTank.Terrestre)
            return pararParaAlvoTerrestre;

        if (tankVisao.TipoAlvoAtual == TankVisao.TipoAlvoTank.Aereo)
            return pararParaAlvoAereo;

        return true;
    }

    private void AtualizarEstadoDoTankAtaque()
    {
        if (!tankLeveGerenciaAtaque || tankAtaque == null) return;

        if (ativarTankAtaqueSomenteComAlvo)
            tankAtaque.enabled = temAlvoNaVisao;
        else if (!tankAtaque.enabled)
            tankAtaque.enabled = true;
    }

    private void PrepararTankParaCombate()
    {
        if (usarSensorDesvio)
        {
            AtualizarSensorDesvio();
            if (desvioAtivo)
            {
                estadoAtual = EstadoTank.EmCombate;
                return;
            }
        }
        else
        {
            desvioAtivo = false;
        }

        estadoAtual = EstadoTank.EmCombate;
        sensorDetectandoObstaculo = false;
        manterDesvioAte = 0f;
        distanciaObstaculoAtual = distanciaSensorFrontal;
        AtualizarManobraDeCombate();
        // Mantém contagemPatrulha intacta — o tank retoma a patrulha normalmente ao sair do combate
    }

    private void AtualizarManobraDeCombate()
    {
        Transform alvo = tankVisao != null ? tankVisao.AlvoAtual : null;
        if (alvo == null) return;

        Vector3 radial = transform.position - alvo.position;
        radial.y = 0f;
        if (radial.sqrMagnitude < 0.01f) return;

        float distancia = radial.magnitude;
        string equipe = ObterEquipe();
        float distanciaIdeal = TankLearningDatabase.ObterDistanciaEngajamento(equipe, distanciaPreferidaCombate);
        float zona = Mathf.Max(3f, distanciaIdeal * 0.15f);
        Vector3 direcaoDesejada;

        if (distancia > distanciaIdeal + zona)
        {
            // Ainda longe: aproxima-se enquanto continua em movimento.
            direcaoDesejada = -radial.normalized;
        }
        else if (distancia < distanciaIdeal - zona)
        {
            // Muito perto: abre espaço para manter a mira e evitar ficar colado.
            direcaoDesejada = radial.normalized;
        }
        else
        {
            // Na distancia de combate, circula o alvo; o lado muda quando recebe dano.
            int lado = TankLearningDatabase.ObterLadoManobra(equipe, distanciaPreferidaCombate);
            direcaoDesejada = Quaternion.AngleAxis(lado * 90f, Vector3.up) * radial.normalized;
        }

        float erroDirecao = Vector3.SignedAngle(ObterFrenteVeiculo(), direcaoDesejada, Vector3.up);
        anguloDirecaoAlvo = Mathf.Clamp(erroDirecao * 0.6f, -anguloMaximoDirecao, anguloMaximoDirecao);
    }

    private string ObterEquipe()
    {
        Transform atual = transform;
        while (atual != null)
        {
            if (atual.CompareTag("Azul") || atual.CompareTag("Vermelho") || atual.CompareTag("Verde"))
                return atual.tag;
            atual = atual.parent;
        }
        return "Geral";
    }

    private void AtualizarPatrulhaLivre()
    {
        contagemPatrulha -= Time.fixedDeltaTime;

        if (contagemPatrulha <= 0f)
        {
            SortearNovaCurva();
            contagemPatrulha = Mathf.Max(0.5f, tempoContagem);
        }

        // Quando a viragem estiver quase completa, zera o angulo e segue reto
        if (Mathf.Abs(anguloDirecaoAtual - anguloDirecaoAlvo) < 1f && Mathf.Abs(anguloDirecaoAlvo) > 0.5f)
            anguloDirecaoAlvo = 0f;
    }

    private void SortearNovaCurva()
    {
        // Sorteia um angulo de viragem unico para mudar de direcao e depois andar reto
        // O angulo e aplicado uma vez e zerado apos a viragem completar
        if (alternarLadoDaCurva)
            ultimoLadoCurva *= -1;
        else
            ultimoLadoCurva = UnityEngine.Random.value < 0.5f ? -1 : 1;

        float anguloMin = Mathf.Clamp(anguloMinimoCurva, 5f, anguloMaximoDirecao);
        float anguloMax = anguloMaximoDirecao;
        anguloDirecaoAlvo = ultimoLadoCurva * UnityEngine.Random.Range(anguloMin, anguloMax);
    }

    private void ForcarTrechoReto(float duracao)
    {
        anguloDirecaoAlvo    = 0f;
        desvioAtivo          = false;
        manterDesvioAte      = 0f;
        // Reinicia a contagem com a duração do trecho reto para não trocar curva logo após desviar
        contagemPatrulha = Mathf.Max(duracao, 0.5f);
    }

    private void AtualizarSensorDesvio()
    {
        bool estavaDesviando = desvioAtivo;
        sensorDetectandoObstaculo = false;
        distanciaObstaculoAtual = distanciaSensorFrontal;

        if (!usarSensorDesvio)
        {
            desvioAtivo       = false;
            ladoDesvioTravado = false;
            return;
        }

        Vector3 origem = ObterOrigemSensor();
        Vector3 frente = ObterFrenteVeiculo();
        float distanciaDeteccao = ObterDistanciaDeteccaoObstaculo();

        Collider obstaculoSobreposto = EncontrarObstaculoSobreposto(origem);
        if (obstaculoSobreposto != null)
        {
            sensorDetectandoObstaculo = true;
            pontoSensorDetectado = obstaculoSobreposto.ClosestPoint(origem);
            normalSensorDetectado = (origem - pontoSensorDetectado).sqrMagnitude > 0.001f
                ? (origem - pontoSensorDetectado).normalized
                : -frente;
            distanciaObstaculoAtual = 0f;

            if (!ladoDesvioTravado)
            {
                ladoDesvioAtual = EscolherMelhorLadoDesvio(pontoSensorDetectado, origem);
                ultimoLadoCurva = ladoDesvioAtual;
                manterDesvioAte = Time.time + tempoManterDesvio;
                ladoDesvioTravado = true;
            }
            else if (Time.time > manterDesvioAte)
            {
                ladoDesvioAtual *= -1;
                ultimoLadoCurva = ladoDesvioAtual;
                manterDesvioAte = Time.time + tempoManterDesvio;
            }

            desvioAtivo = true;
            estadoAtual = emCombate ? EstadoTank.EmCombate : EstadoTank.DesviandoObstaculo;
            anguloDirecaoAlvo = ladoDesvioAtual * Mathf.Min(90f, anguloMaximoDirecao);
            return;
        }

        RaycastHit hitFrontal;
        bool obstaculoFrente = SensorCast(origem, frente, distanciaDeteccao, raioSensorFrontal, out hitFrontal);

        if (obstaculoFrente)
        {
            sensorDetectandoObstaculo = true;
            pontoSensorDetectado      = hitFrontal.point;
            normalSensorDetectado     = hitFrontal.normal;
            distanciaObstaculoAtual   = hitFrontal.distance;

            // So define o lado na PRIMEIRA deteccao do obstaculo atual.
            // Isso impede que o lado fique alternando frame a frame (causa do giro 360).
            if (!ladoDesvioTravado)
            {
                ladoDesvioAtual   = EscolherMelhorLadoDesvio(hitFrontal.point, origem);
                ultimoLadoCurva   = ladoDesvioAtual;
                manterDesvioAte   = Time.time + tempoManterDesvio;
                ladoDesvioTravado = true;
            }
            // Se o timer expirou mas o obstaculo persiste: inverte o lado e tenta de novo.
            else if (Time.time > manterDesvioAte)
            {
                ladoDesvioAtual *= -1;
                ultimoLadoCurva  = ladoDesvioAtual;
                manterDesvioAte  = Time.time + tempoManterDesvio;
            }
        }

        // Desvio ativo enquanto timer nao expirou
        desvioAtivo = Time.time <= manterDesvioAte;
        estadoAtual = desvioAtivo ? EstadoTank.DesviandoObstaculo : EstadoTank.Patrulhando;

        // Destrava quando desvio terminar e nao ha mais obstaculo
        if (!desvioAtivo || !obstaculoFrente)
        {
            if (!obstaculoFrente)
                ladoDesvioTravado = false;
        }

        if (desvioAtivo)
        {
            // Angulo fixo de 90 graus para garantir saida sem completar voltas.
            anguloDirecaoAlvo = ladoDesvioAtual * Mathf.Min(90f, anguloMaximoDirecao);
            return;
        }

        if (estavaDesviando && !sensorDetectandoObstaculo)
        {
            // Apos desvio limpo: sorteia nova curva e forca um trecho reto
            // para o tank sair da zona do obstaculo antes de curvar de novo.
            ForcarTrechoReto(tempoRetoAposDesvio);
        }
    }

    private int EscolherMelhorLadoDesvio(Vector3 pontoContato, Vector3 origem)
    {
        // Mede espaco a 90 graus para cada lado (mais preciso que anguloSensoresLaterais)
        float espacoDir = MedirEspacoNaDirecao( 1, origem);
        float espacoEsq = MedirEspacoNaDirecao(-1, origem);

        // Se um lado tem claramente mais espaco, vai para ele
        if (Mathf.Abs(espacoDir - espacoEsq) > margemEscolhaLado)
            return espacoDir > espacoEsq ? 1 : -1;

        // Empate: desvia para o lado oposto ao ponto de contato
        Vector3 direcaoContato = pontoContato - origem;
        direcaoContato.y = 0f;

        if (direcaoContato.sqrMagnitude > 0.01f)
        {
            float ladoContato = Vector3.SignedAngle(ObterFrenteVeiculo(), direcaoContato.normalized, Vector3.up);
            if (!Mathf.Approximately(ladoContato, 0f))
                return ladoContato > 0f ? -1 : 1;
        }

        // Ultimo recurso: alterna o lado
        ultimoLadoCurva *= -1;
        return ultimoLadoCurva;
    }

    private float MedirEspacoNaDirecao(int lado, Vector3 origem)
    {
        // Usa 90 graus fixo para medir espaco lateral real
        Vector3 direcao = Quaternion.AngleAxis(lado * 90f, Vector3.up) * ObterFrenteVeiculo();
        RaycastHit hit;
        if (SensorCast(origem, direcao.normalized, ObterDistanciaDeteccaoObstaculo(), raioSensorFrontal, out hit))
            return hit.distance;
        return distanciaSensorFrontal;
    }

    private bool SensorCast(Vector3 origem, Vector3 direcao, float distancia, float raio, out RaycastHit melhorHit)
    {
        melhorHit = new RaycastHit();
        QueryTriggerInteraction triggerMode = detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        float raioSeguro      = Mathf.Max(0.05f, raio);
        float distanciaSegura = Mathf.Max(0.1f, distancia);

        bool encontrou     = false;
        float menorDistancia = float.MaxValue;

        // Raycast simples primeiro: detecta paredes mesmo quando a origem esta proxima delas.
        RaycastHit hitRay;
        if (Physics.Raycast(origem, direcao.normalized, out hitRay, distanciaSegura, camadasDetectaveis, triggerMode))
        {
            if (!DeveIgnorarHitSensor(hitRay))
            {
                melhorHit      = hitRay;
                menorDistancia = hitRay.distance;
                encontrou      = true;
            }
        }

        RaycastHit[] hits = Physics.SphereCastAll(origem, raioSeguro, direcao.normalized, distanciaSegura, camadasDetectaveis, triggerMode);
        for (int i = 0; i < hits.Length; i++)
        {
            if (DeveIgnorarHitSensor(hits[i])) continue;

            if (hits[i].distance < menorDistancia)
            {
                menorDistancia = hits[i].distance;
                melhorHit      = hits[i];
                encontrou      = true;
            }
        }

        return encontrou;
    }

    private float ObterDistanciaDeteccaoObstaculo()
    {
        float velocidade = Mathf.Abs(velocidadeAtual);
        float desaceleracao = Mathf.Max(0.5f, aceleracao);
        float distanciaParada = (velocidade * velocidade) / (2f * desaceleracao);
        return distanciaSensorFrontal + distanciaParada + velocidade * Time.fixedDeltaTime + 0.5f;
    }

    private Collider EncontrarObstaculoSobreposto(Vector3 origem)
    {
        float raio = Mathf.Max(0.05f, raioSensorFrontal);
        QueryTriggerInteraction triggerMode = detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        int quantidade = Physics.OverlapSphereNonAlloc(origem, raio, bufferSobreposicaoSensor, camadasDetectaveis, triggerMode);
        Collider maisProximo = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < quantidade; i++)
        {
            Collider candidato = bufferSobreposicaoSensor[i];
            if (candidato == null || candidato.transform == transform || candidato.transform.IsChildOf(transform))
                continue;

            Vector3 ponto = candidato.ClosestPoint(origem);
            float distancia = (ponto - origem).sqrMagnitude;
            if (distancia < menorDistancia && !DeveIgnorarHitSensor(candidato, ponto, origem))
            {
                menorDistancia = distancia;
                maisProximo = candidato;
            }
        }

        return maisProximo;
    }

    private bool DeveIgnorarHitSensor(Collider collider, Vector3 ponto, Vector3 origem)
    {
        if (collider == null) return true;
        if (tagsIgnoradasNoSensor != null)
        {
            for (int i = 0; i < tagsIgnoradasNoSensor.Length; i++)
            {
                if (!string.IsNullOrEmpty(tagsIgnoradasNoSensor[i]) && collider.CompareTag(tagsIgnoradasNoSensor[i]))
                    return true;
            }
        }

        if (ignorarChaoNoSensor)
        {
            Vector3 normal = origem - ponto;
            if (normal.sqrMagnitude > 0.001f && Vector3.Dot(normal.normalized, Vector3.up) >= normalMinimaParaChao)
                return true;
        }

        return false;
    }

    private bool DeveIgnorarHitSensor(RaycastHit hit)
    {
        if (hit.collider == null) return true;
        if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) return true;

        // Ignora objetos com tags configuradas (Terrain, Water, etc.)
        if (tagsIgnoradasNoSensor != null && tagsIgnoradasNoSensor.Length > 0)
        {
            string tagObj = hit.collider.gameObject.tag;
            for (int i = 0; i < tagsIgnoradasNoSensor.Length; i++)
            {
                if (!string.IsNullOrEmpty(tagsIgnoradasNoSensor[i]) && tagObj == tagsIgnoradasNoSensor[i])
                    return true;
            }
        }

        // Ignora chão pela normal (superfície horizontal)
        if (ignorarChaoNoSensor && hit.normal.y >= normalMinimaParaChao) return true;

        return false;
    }
    private Vector3 ObterOrigemSensor()
    {
        Vector3 baseOrigem = rb != null ? rb.worldCenterOfMass : transform.position;
        return baseOrigem + transform.up * alturaSensor + ObterFrenteVeiculo() * offsetFrenteSensor;
    }

    private Vector3 ObterFrenteVeiculo()
    {
        return transform.right.normalized;
    }

    private void TratarColisaoFisica(Collision collision)
    {
        if (!trocarCurvaAoBater) return;
        if (tankMorto || emRe) return;
        if (DeveIgnorarColisao(collision)) return;
        ForcarCurvaContraria(collision);

        // Sai da parede com ré antes de tentar manobrar. Os casts do sensor podem
        // não detectar o obstáculo enquanto a origem dele já estiver dentro dele.
        ladoCurvaAposRe = ladoDesvioAtual;
        IniciarRe();
    }

    private bool DeveIgnorarColisao(Collision collision)
    {
        if (collision == null || collision.collider == null) return true;
        if (collision.collider.transform == transform || collision.collider.transform.IsChildOf(transform)) return true;
        if (!ignorarChaoNaColisao) return false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contato = collision.GetContact(i);
            if (contato.normal.y >= normalMinimaParaChao) return true;
        }

        return false;
    }

    private void ForcarCurvaContraria(Collision collision)
    {
        Vector3 origem = transform.position;
        Vector3 pontoContato = collision.contactCount > 0 ? collision.GetContact(0).point : collision.transform.position;
        Vector3 direcaoContato = pontoContato - origem;
        direcaoContato.y = 0f;

        if (direcaoContato.sqrMagnitude > 0.01f)
        {
            float ladoContato = Vector3.SignedAngle(ObterFrenteVeiculo(), direcaoContato.normalized, Vector3.up);
            ladoDesvioAtual = ladoContato > 0f ? -1 : 1;
        }
        else
        {
            ladoDesvioAtual = ultimoLadoCurva * -1;
        }

        ultimoLadoCurva   = ladoDesvioAtual;
        anguloDirecaoAlvo = ladoDesvioAtual * anguloMaximoDirecao;
        manterDesvioAte   = Time.time + Mathf.Max(tempoManterDesvio, tempoManterDesvioAposColisao);
        // Reinicia contagem para não trocar curva logo após uma colisão
        contagemPatrulha  = Mathf.Max(tempoManterDesvioAposColisao, 1f);

        if (reduzirVelocidadeAoDesviar)
            velocidadeAtual = Mathf.Min(velocidadeAtual, velocidadeDuranteDesvio);
    }

    private void AtualizarMovimentoFisico(float deltaTime)
    {
        if (rb == null) return;

        float velocidadeDesejada = CalcularVelocidadeDesejada();
        float aceleracaoUsada = emCombate ? Mathf.Max(aceleracao, freioAoEncontrarInimigo) : aceleracao;
        velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, aceleracaoUsada * deltaTime);

        anguloDirecaoAtual = Mathf.Lerp(anguloDirecaoAtual, anguloDirecaoAlvo, Mathf.Clamp01(suavidadeDirecao * deltaTime));

        if (Mathf.Abs(anguloDirecaoAlvo) <= 0.01f && Mathf.Abs(anguloDirecaoAtual) <= 0.05f)
            anguloDirecaoAtual = 0f;

        AplicarMovimentoComFisica(deltaTime);
    }

    private float CalcularVelocidadeDesejada()
    {
        if (tankMorto || velocidadeFrente <= 0.01f) return 0f;
        if (emRe) return -Mathf.Clamp(velocidadeRe, 0.1f, velocidadeFrente);
        if (emCombate)
        {
            float baseCombate = Mathf.Max(velocidadeDuranteCombate, velocidadeMinimaDuranteCombate);
            float evasao = TankLearningDatabase.ObterMultiplicadorDeEvasao(ObterEquipe(), distanciaPreferidaCombate);
            float velocidadeCombate = Mathf.Clamp(baseCombate * evasao, velocidadeMinimaDuranteCombate, velocidadeFrente);
            if (sensorDetectandoObstaculo && reduzirVelocidadeAoDesviar)
                return CalcularVelocidadeSeguraParaObstaculo(velocidadeCombate);
            return velocidadeCombate;
        }
        
        // CORRE��O: Se n�o h� obst�culo sendo detectado AGORA, n�o reduzimos a velocidade.
        if (!sensorDetectandoObstaculo) return velocidadeFrente;
        if (!reduzirVelocidadeAoDesviar) return velocidadeFrente;

        return CalcularVelocidadeSeguraParaObstaculo(velocidadeFrente);
    }

    private float CalcularVelocidadeSeguraParaObstaculo(float velocidadeMaxima)
    {
        float velocidadeDesvioSegura = Mathf.Clamp(velocidadeDuranteDesvio, 0.05f, velocidadeMaxima);
        float distanciaParada = (velocidadeAtual * velocidadeAtual) / (2f * Mathf.Max(0.5f, aceleracao));
        float distanciaReducao = Mathf.Max(distanciaComecarReduzir, distanciaParada + 1f);
        float fatorDistancia = Mathf.InverseLerp(0f, distanciaReducao, distanciaObstaculoAtual);
        return Mathf.Lerp(0.25f, velocidadeDesvioSegura, fatorDistancia);
    }

    private void AplicarMovimentoComFisica(float deltaTime)
    {
        float distanciaEntreEixosSegura = Mathf.Max(0.1f, distanciaEntreEixos);
        float anguloDirecaoRad = anguloDirecaoAtual * Mathf.Deg2Rad;
        float grausPorSegundo = (velocidadeAtual / distanciaEntreEixosSegura) * Mathf.Tan(anguloDirecaoRad) * Mathf.Rad2Deg;

        Quaternion novaRotacao = Quaternion.AngleAxis(grausPorSegundo * deltaTime, Vector3.up) * rb.rotation;
        rb.MoveRotation(novaRotacao);

        Vector3 frenteX = novaRotacao * Vector3.right;
        Vector3 velocidadeHorizontal = frenteX.normalized * velocidadeAtual;
        Vector3 velocidadeFisicaAtual = ObterVelocidadeRigidbody();
        Vector3 novaVelocidade = new Vector3(velocidadeHorizontal.x, velocidadeFisicaAtual.y, velocidadeHorizontal.z);
        DefinirVelocidadeRigidbody(novaVelocidade);
    }

    private void AtualizarRodasSincronizadasComVeiculo(float deltaTime)
    {
        if (controleRodas == null) return;

        float velocidadeRealVeiculo = CalcularVelocidadeHorizontalRealDoVeiculo();
        float velocidadeRotacaoRodas = velocidadeRealVeiculo * rotacaoRodasPorUnidadeVelocidade;

        if (Mathf.Abs(velocidadeRotacaoRodas) <= 0.05f)
        {
            controleRodas.SendMessage("PararRodas", null, SendMessageOptions.DontRequireReceiver);
            controleRodas.SendMessage("PararGiro", null, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            controleRodas.SendMessage("AtivarGiro", null, SendMessageOptions.DontRequireReceiver);
            controleRodas.SendMessage("DefinirVelocidadeRotacao", velocidadeRotacaoRodas, SendMessageOptions.DontRequireReceiver);
        }

        AtualizarDirecaoVisualDasRodasDianteiras(deltaTime);
    }

    private void AtualizarDirecaoVisualDasRodasDianteiras(float deltaTime)
    {
        if (controleRodas == null) return;

        float anguloAlvoVisual = anguloDirecaoAtual * multiplicadorVisualDirecaoRodas;
        if (inverterDirecaoVisualRodasDianteiras) anguloAlvoVisual *= -1f;
        anguloAlvoVisual = Mathf.Clamp(anguloAlvoVisual, -anguloMaximoDirecao, anguloMaximoDirecao);

        anguloVisualDirecaoRodas = Mathf.Lerp(anguloVisualDirecaoRodas, anguloAlvoVisual, Mathf.Clamp01(suavidadeVisualDirecaoRodas * deltaTime));

        if (Mathf.Abs(anguloAlvoVisual) <= limiteCentralizarVisualRodas && Mathf.Abs(anguloVisualDirecaoRodas) <= limiteCentralizarVisualRodas)
            anguloVisualDirecaoRodas = 0f;

        controleRodas.SendMessage("DefinirAnguloDirecaoDianteira", anguloVisualDirecaoRodas, SendMessageOptions.DontRequireReceiver);
    }

    private void PararECentralizarRodas()
    {
        if (controleRodas == null) return;
        controleRodas.SendMessage("PararRodas", null, SendMessageOptions.DontRequireReceiver);
        controleRodas.SendMessage("PararGiro", null, SendMessageOptions.DontRequireReceiver);
        controleRodas.SendMessage("DefinirAnguloDirecaoDianteira", 0f, SendMessageOptions.DontRequireReceiver);
        controleRodas.SendMessage("CentralizarDirecaoDianteira", null, SendMessageOptions.DontRequireReceiver);
    }

    private float CalcularVelocidadeHorizontalRealDoVeiculo()
    {
        if (rb == null) return velocidadeAtual;

        Vector3 velocidadeFisica = ObterVelocidadeRigidbody();
        velocidadeFisica.y = 0f;
        float velocidadeHorizontal = velocidadeFisica.magnitude;

        if (velocidadeHorizontal <= 0.01f) return 0f;

        float sentido = Vector3.Dot(velocidadeFisica.normalized, ObterFrenteVeiculo()) >= 0f ? 1f : -1f;
        return velocidadeHorizontal * sentido;
    }

    private Vector3 ObterVelocidadeRigidbody()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void DefinirVelocidadeRigidbody(Vector3 novaVelocidade)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = novaVelocidade;
#else
        rb.velocity = novaVelocidade;
#endif
    }

    private void OnValidate()
    {
        velocidadeFrente = Mathf.Max(0f, velocidadeFrente);
        aceleracao = Mathf.Max(0.1f, aceleracao);
        distanciaEntreEixos = Mathf.Max(0.1f, distanciaEntreEixos);
        anguloMaximoDirecao = Mathf.Clamp(anguloMaximoDirecao, 0f, 60f);
        suavidadeDirecao = Mathf.Max(0.1f, suavidadeDirecao);
        tempoContagem = Mathf.Max(0.5f, tempoContagem);
        anguloMinimoCurva = Mathf.Clamp(anguloMinimoCurva, 0f, anguloMaximoDirecao);
        velocidadeDuranteCombate = Mathf.Max(0f, velocidadeDuranteCombate);
        freioAoEncontrarInimigo = Mathf.Max(0.1f, freioAoEncontrarInimigo);
        tempoContinuarParadoAposPerderAlvo = Mathf.Max(0f, tempoContinuarParadoAposPerderAlvo);
        tempoParaDestruirAposMorrer = Mathf.Max(0f, tempoParaDestruirAposMorrer);
        distanciaSensorFrontal = Mathf.Max(0.2f, distanciaSensorFrontal);
        raioSensorFrontal = Mathf.Max(0.05f, raioSensorFrontal);
        alturaSensor = Mathf.Max(0f, alturaSensor);
        offsetFrenteSensor = Mathf.Max(0f, offsetFrenteSensor);
        anguloSensoresLaterais = Mathf.Clamp(anguloSensoresLaterais, 5f, 85f);
        tempoManterDesvio = Mathf.Max(0.1f, tempoManterDesvio);
        tempoRetoAposDesvio = Mathf.Max(0.1f, tempoRetoAposDesvio);
        velocidadeDuranteDesvio = Mathf.Max(0f, velocidadeDuranteDesvio);
        velocidadeMinimaDesvio = Mathf.Clamp(velocidadeMinimaDesvio, 0f, velocidadeDuranteDesvio);
        distanciaComecarReduzir = Mathf.Max(0.1f, distanciaComecarReduzir);
        margemEscolhaLado = Mathf.Max(0f, margemEscolhaLado);
        normalMinimaParaChao = Mathf.Clamp01(normalMinimaParaChao);
        tempoManterDesvioAposColisao = Mathf.Max(0.1f, tempoManterDesvioAposColisao);
        tempoParaDetectarStuck = Mathf.Max(0.1f, tempoParaDetectarStuck);
        velocidadeRe = Mathf.Max(0.1f, velocidadeRe);
        duracaoRe = Mathf.Max(0.1f, duracaoRe);
        anguloViradaAposRe = Mathf.Clamp(anguloViradaAposRe, 0f, anguloMaximoDirecao);
        limiarMovimentoStuck = Mathf.Max(0.01f, limiarMovimentoStuck);
        rotacaoRodasPorUnidadeVelocidade = Mathf.Max(0f, rotacaoRodasPorUnidadeVelocidade);
        multiplicadorVisualDirecaoRodas = Mathf.Max(0f, multiplicadorVisualDirecaoRodas);
        suavidadeVisualDirecaoRodas = Mathf.Max(0.1f, suavidadeVisualDirecaoRodas);
        limiteCentralizarVisualRodas = Mathf.Max(0.01f, limiteCentralizarVisualRodas);
    }

    private void OnDrawGizmosSelected()
    {
        if (desenharFrenteNoEditor)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * 4f);
            Gizmos.color = Color.yellow;
            Vector3 direcaoCurva = Quaternion.AngleAxis(anguloDirecaoAtual, Vector3.up) * transform.right;
            Gizmos.DrawLine(transform.position, transform.position + direcaoCurva.normalized * 3f);
        }

        if (desenharSensorNoEditor)
        {
            Vector3 origem = Application.isPlaying ? ObterOrigemSensor() : transform.position + transform.up * alturaSensor + transform.right * offsetFrenteSensor;
            Vector3 frente = transform.right.normalized;
            Vector3 sensorPositivo = Quaternion.AngleAxis(anguloSensoresLaterais, Vector3.up) * frente;
            Vector3 sensorNegativo = Quaternion.AngleAxis(-anguloSensoresLaterais, Vector3.up) * frente;

            Gizmos.color = sensorDetectandoObstaculo ? Color.red : Color.green;
            Gizmos.DrawLine(origem, origem + frente * distanciaSensorFrontal);
            Gizmos.DrawWireSphere(origem + frente * distanciaSensorFrontal, raioSensorFrontal);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origem, origem + sensorPositivo * distanciaSensorFrontal);
            Gizmos.DrawLine(origem, origem + sensorNegativo * distanciaSensorFrontal);

            if (sensorDetectandoObstaculo)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pontoSensorDetectado, 0.2f);
                Gizmos.DrawLine(pontoSensorDetectado, pontoSensorDetectado + normalSensorDetectado * 1.5f);
            }
        }

        if (desenharAlvoNoEditor && emCombate && AlvoAtual != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, AlvoAtual.position);
            Gizmos.DrawWireSphere(AlvoAtual.position, 1.2f);
        }
    }
}
