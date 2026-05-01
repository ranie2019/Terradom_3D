using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TankLeve : MonoBehaviour
{
    [Header("Movimento fisico do veiculo")]
    [SerializeField] private float velocidadeFrente = 4f;
    [SerializeField] private float aceleracao = 6f;
    [SerializeField] private float distanciaEntreEixos = 3f;
    [SerializeField] private float anguloMaximoDirecao = 32f;
    [SerializeField] private float suavidadeDirecao = 3.5f;

    [Header("Patrulha livre")]
    [SerializeField] private float tempoMinimoMesmaCurva = 2f;
    [SerializeField] private float tempoMaximoMesmaCurva = 5f;
    [SerializeField] private float chanceDeAndarReto = 0.35f;
    [SerializeField] private float anguloMinimoCurva = 8f;
    [SerializeField] private bool alternarLadoDaCurva = true;
    [SerializeField] private bool forcarRetoEntreCurvas = true;
    [SerializeField] private float tempoMinimoRetoEntreCurvas = 0.8f;
    [SerializeField] private float tempoMaximoRetoEntreCurvas = 1.6f;

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

    [Header("Colisao fisica")]
    [SerializeField] private bool configurarRigidbodyAutomaticamente = true;
    [SerializeField] private bool congelarTombamento = true;
    [SerializeField] private bool trocarCurvaAoBater = true;
    [SerializeField] private bool ignorarChaoNaColisao = true;
    [SerializeField] private float tempoManterDesvioAposColisao = 1.2f;

    [Header("Rodas - giro Z e direcao Y")]
    [SerializeField] private Roda controleRodas;
    [SerializeField] private float rotacaoRodasPorUnidadeVelocidade = 160f;
    [SerializeField] private bool inverterDirecaoVisualRodasDianteiras = false;
    [SerializeField] private float multiplicadorVisualDirecaoRodas = 1f;
    [SerializeField] private float suavidadeVisualDirecaoRodas = 12f;
    [SerializeField] private float limiteCentralizarVisualRodas = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool desenharFrenteNoEditor = true;
    [SerializeField] private bool desenharSensorNoEditor = true;

    private Rigidbody rb;

    private float velocidadeAtual;
    private float anguloDirecaoAtual;
    private float anguloDirecaoAlvo;
    private float anguloVisualDirecaoRodas;
    private float proximaTrocaDeCurva;
    private float manterDesvioAte;
    private float distanciaObstaculoAtual;

    private int ultimoLadoCurva = 1;
    private int ladoDesvioAtual = 1;

    private bool sensorDetectandoObstaculo;
    private bool desvioAtivo;

    private Vector3 pontoSensorDetectado;
    private Vector3 normalSensorDetectado;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        AplicarConfiguracaoRigidbody();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (controleRodas == null)
            controleRodas = GetComponentInChildren<Roda>();

        AplicarConfiguracaoRigidbody();
        SortearNovaCurva();
    }

    private void FixedUpdate()
    {
        AtualizarSensorDesvio();

        if (!desvioAtivo)
            AtualizarPatrulhaLivre();

        AtualizarMovimentoFisico(Time.fixedDeltaTime);
        AtualizarRodasSincronizadasComVeiculo(Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TratarColisaoFisica(collision);
    }

    private void OnDisable()
    {
        if (controleRodas != null)
        {
            controleRodas.PararRodas();
            controleRodas.CentralizarDirecaoDianteira();
        }

        if (rb != null)
            DefinirVelocidadeRigidbody(Vector3.zero);

        anguloVisualDirecaoRodas = 0f;
    }

    private void AplicarConfiguracaoRigidbody()
    {
        if (!configurarRigidbodyAutomaticamente || rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (congelarTombamento)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void AtualizarPatrulhaLivre()
    {
        if (Time.time < proximaTrocaDeCurva)
            return;

        if (forcarRetoEntreCurvas && Mathf.Abs(anguloDirecaoAlvo) > 0.5f)
        {
            float tempoReto = Random.Range(tempoMinimoRetoEntreCurvas, tempoMaximoRetoEntreCurvas);
            ForcarTrechoReto(tempoReto);
            return;
        }

        SortearNovaCurva();
    }

    private void SortearNovaCurva()
    {
        float tempoMin = Mathf.Max(0.2f, tempoMinimoMesmaCurva);
        float tempoMax = Mathf.Max(tempoMin, tempoMaximoMesmaCurva);

        proximaTrocaDeCurva = Time.time + Random.Range(tempoMin, tempoMax);

        if (Random.value <= chanceDeAndarReto)
        {
            anguloDirecaoAlvo = 0f;
            return;
        }

        int ladoCurva;

        if (alternarLadoDaCurva)
        {
            ultimoLadoCurva *= -1;
            ladoCurva = ultimoLadoCurva;
        }
        else
        {
            ladoCurva = Random.value < 0.5f ? -1 : 1;
        }

        float anguloMin = Mathf.Clamp(anguloMinimoCurva, 0f, anguloMaximoDirecao);
        float anguloMax = Mathf.Max(anguloMin, anguloMaximoDirecao);

        anguloDirecaoAlvo = ladoCurva * Random.Range(anguloMin, anguloMax);
    }

    private void ForcarTrechoReto(float duracao)
    {
        anguloDirecaoAlvo = 0f;
        desvioAtivo = false;
        manterDesvioAte = 0f;
        proximaTrocaDeCurva = Time.time + Mathf.Max(0.1f, duracao);
    }

    private void AtualizarSensorDesvio()
    {
        bool estavaDesviando = desvioAtivo;

        sensorDetectandoObstaculo = false;
        distanciaObstaculoAtual = distanciaSensorFrontal;

        if (!usarSensorDesvio)
        {
            desvioAtivo = false;
            return;
        }

        RaycastHit hitFrontal;
        bool encontrouObstaculoFrontal = SensorCast(
            ObterOrigemSensor(),
            ObterFrenteVeiculo(),
            distanciaSensorFrontal,
            raioSensorFrontal,
            out hitFrontal
        );

        if (encontrouObstaculoFrontal)
        {
            sensorDetectandoObstaculo = true;
            pontoSensorDetectado = hitFrontal.point;
            normalSensorDetectado = hitFrontal.normal;
            distanciaObstaculoAtual = hitFrontal.distance;

            ladoDesvioAtual = EscolherMelhorLadoDesvio(hitFrontal);
            ultimoLadoCurva = ladoDesvioAtual;
            manterDesvioAte = Time.time + tempoManterDesvio;
        }

        desvioAtivo = Time.time <= manterDesvioAte;

        if (desvioAtivo)
        {
            anguloDirecaoAlvo = ladoDesvioAtual * anguloMaximoDirecao;
            proximaTrocaDeCurva = Time.time + tempoManterDesvio;
            return;
        }

        if (estavaDesviando && !sensorDetectandoObstaculo)
            ForcarTrechoReto(tempoRetoAposDesvio);
    }

    private int EscolherMelhorLadoDesvio(RaycastHit hitFrontal)
    {
        Vector3 origem = ObterOrigemSensor();

        float espacoLadoPositivo = MedirEspacoNaDirecao(1, origem);
        float espacoLadoNegativo = MedirEspacoNaDirecao(-1, origem);

        if (Mathf.Abs(espacoLadoPositivo - espacoLadoNegativo) > margemEscolhaLado)
            return espacoLadoPositivo > espacoLadoNegativo ? 1 : -1;

        Vector3 direcaoObjeto = hitFrontal.point - origem;
        direcaoObjeto.y = 0f;

        if (direcaoObjeto.sqrMagnitude > 0.01f)
        {
            float ladoObjeto = Vector3.SignedAngle(ObterFrenteVeiculo(), direcaoObjeto.normalized, Vector3.up);

            if (!Mathf.Approximately(ladoObjeto, 0f))
                return ladoObjeto > 0f ? -1 : 1;
        }

        ultimoLadoCurva *= -1;
        return ultimoLadoCurva;
    }

    private float MedirEspacoNaDirecao(int lado, Vector3 origem)
    {
        Vector3 direcao = Quaternion.AngleAxis(lado * anguloSensoresLaterais, Vector3.up) * ObterFrenteVeiculo();

        RaycastHit hit;
        if (SensorCast(origem, direcao.normalized, distanciaSensorFrontal, raioSensorFrontal, out hit))
            return hit.distance;

        return distanciaSensorFrontal;
    }

    private bool SensorCast(Vector3 origem, Vector3 direcao, float distancia, float raio, out RaycastHit melhorHit)
    {
        melhorHit = new RaycastHit();

        QueryTriggerInteraction triggerMode = detectarTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        RaycastHit[] hits = Physics.SphereCastAll(
            origem,
            Mathf.Max(0.05f, raio),
            direcao.normalized,
            Mathf.Max(0.1f, distancia),
            camadasDetectaveis,
            triggerMode
        );

        bool encontrou = false;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider colisor = hits[i].collider;

            if (colisor == null)
                continue;

            if (colisor.transform == transform || colisor.transform.IsChildOf(transform))
                continue;

            if (ignorarChaoNoSensor && hits[i].normal.y >= normalMinimaParaChao)
                continue;

            if (hits[i].distance < menorDistancia)
            {
                menorDistancia = hits[i].distance;
                melhorHit = hits[i];
                encontrou = true;
            }
        }

        return encontrou;
    }

    private Vector3 ObterOrigemSensor()
    {
        Vector3 baseOrigem = rb != null ? rb.worldCenterOfMass : transform.position;
        return baseOrigem + transform.up * alturaSensor + ObterFrenteVeiculo() * offsetFrenteSensor;
    }

    private Vector3 ObterFrenteVeiculo()
    {
        // Frente real do modelo = eixo local X positivo.
        return transform.right.normalized;
    }

    private void TratarColisaoFisica(Collision collision)
    {
        if (!trocarCurvaAoBater)
            return;

        if (DeveIgnorarColisao(collision))
            return;

        ForcarCurvaContraria(collision);
    }

    private bool DeveIgnorarColisao(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return true;

        if (collision.collider.transform == transform || collision.collider.transform.IsChildOf(transform))
            return true;

        if (!ignorarChaoNaColisao)
            return false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contato = collision.GetContact(i);

            if (contato.normal.y >= normalMinimaParaChao)
                return true;
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

        ultimoLadoCurva = ladoDesvioAtual;
        anguloDirecaoAlvo = ladoDesvioAtual * anguloMaximoDirecao;
        manterDesvioAte = Time.time + Mathf.Max(tempoManterDesvio, tempoManterDesvioAposColisao);
        proximaTrocaDeCurva = Time.time + Mathf.Max(tempoMinimoMesmaCurva, tempoManterDesvioAposColisao);

        if (reduzirVelocidadeAoDesviar)
            velocidadeAtual = Mathf.Min(velocidadeAtual, velocidadeDuranteDesvio);
    }

    private void AtualizarMovimentoFisico(float deltaTime)
    {
        if (rb == null)
            return;

        float velocidadeDesejada = CalcularVelocidadeDesejada();

        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual,
            velocidadeDesejada,
            aceleracao * deltaTime
        );

        anguloDirecaoAtual = Mathf.Lerp(
            anguloDirecaoAtual,
            anguloDirecaoAlvo,
            Mathf.Clamp01(suavidadeDirecao * deltaTime)
        );

        if (Mathf.Abs(anguloDirecaoAlvo) <= 0.01f && Mathf.Abs(anguloDirecaoAtual) <= 0.05f)
            anguloDirecaoAtual = 0f;

        AplicarMovimentoComFisica(deltaTime);
    }

    private float CalcularVelocidadeDesejada()
    {
        if (!desvioAtivo)
            return velocidadeFrente;

        if (!reduzirVelocidadeAoDesviar)
            return velocidadeFrente;

        if (!sensorDetectandoObstaculo)
            return velocidadeDuranteDesvio;

        float distanciaReducao = Mathf.Max(0.1f, distanciaComecarReduzir);
        float fatorDistancia = Mathf.InverseLerp(0f, distanciaReducao, distanciaObstaculoAtual);

        return Mathf.Lerp(velocidadeMinimaDesvio, velocidadeDuranteDesvio, fatorDistancia);
    }

    private void AplicarMovimentoComFisica(float deltaTime)
    {
        float distanciaEntreEixosSegura = Mathf.Max(0.1f, distanciaEntreEixos);
        float anguloDirecaoRad = anguloDirecaoAtual * Mathf.Deg2Rad;

        float grausPorSegundo =
            (velocidadeAtual / distanciaEntreEixosSegura) *
            Mathf.Tan(anguloDirecaoRad) *
            Mathf.Rad2Deg;

        Quaternion novaRotacao = Quaternion.AngleAxis(grausPorSegundo * deltaTime, Vector3.up) * rb.rotation;
        rb.MoveRotation(novaRotacao);

        // Frente real do modelo = eixo local X positivo.
        Vector3 frenteX = novaRotacao * Vector3.right;
        Vector3 velocidadeHorizontal = frenteX.normalized * velocidadeAtual;

        Vector3 velocidadeFisicaAtual = ObterVelocidadeRigidbody();
        Vector3 novaVelocidade = new Vector3(
            velocidadeHorizontal.x,
            velocidadeFisicaAtual.y,
            velocidadeHorizontal.z
        );

        DefinirVelocidadeRigidbody(novaVelocidade);
    }

    private void AtualizarRodasSincronizadasComVeiculo(float deltaTime)
    {
        if (controleRodas == null)
            return;

        float velocidadeRealVeiculo = CalcularVelocidadeHorizontalRealDoVeiculo();
        float velocidadeRotacaoRodas = velocidadeRealVeiculo * rotacaoRodasPorUnidadeVelocidade;

        if (Mathf.Abs(velocidadeRotacaoRodas) <= 0.05f)
            controleRodas.PararRodas();
        else
            controleRodas.DefinirVelocidadeRotacao(velocidadeRotacaoRodas);

        AtualizarDirecaoVisualDasRodasDianteiras(deltaTime);
    }

    private void AtualizarDirecaoVisualDasRodasDianteiras(float deltaTime)
    {
        float anguloAlvoVisual = anguloDirecaoAtual * multiplicadorVisualDirecaoRodas;

        if (inverterDirecaoVisualRodasDianteiras)
            anguloAlvoVisual *= -1f;

        anguloAlvoVisual = Mathf.Clamp(anguloAlvoVisual, -anguloMaximoDirecao, anguloMaximoDirecao);

        anguloVisualDirecaoRodas = Mathf.Lerp(
            anguloVisualDirecaoRodas,
            anguloAlvoVisual,
            Mathf.Clamp01(suavidadeVisualDirecaoRodas * deltaTime)
        );

        if (Mathf.Abs(anguloAlvoVisual) <= limiteCentralizarVisualRodas && Mathf.Abs(anguloVisualDirecaoRodas) <= limiteCentralizarVisualRodas)
            anguloVisualDirecaoRodas = 0f;

        controleRodas.DefinirAnguloDirecaoDianteira(anguloVisualDirecaoRodas);
    }

    private float CalcularVelocidadeHorizontalRealDoVeiculo()
    {
        if (rb == null)
            return velocidadeAtual;

        Vector3 velocidadeFisica = ObterVelocidadeRigidbody();
        velocidadeFisica.y = 0f;

        float velocidadeHorizontal = velocidadeFisica.magnitude;

        if (velocidadeHorizontal <= 0.01f)
            return 0f;

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

        tempoMinimoMesmaCurva = Mathf.Max(0.2f, tempoMinimoMesmaCurva);
        tempoMaximoMesmaCurva = Mathf.Max(tempoMinimoMesmaCurva, tempoMaximoMesmaCurva);
        chanceDeAndarReto = Mathf.Clamp01(chanceDeAndarReto);
        anguloMinimoCurva = Mathf.Clamp(anguloMinimoCurva, 0f, anguloMaximoDirecao);
        tempoMinimoRetoEntreCurvas = Mathf.Max(0.1f, tempoMinimoRetoEntreCurvas);
        tempoMaximoRetoEntreCurvas = Mathf.Max(tempoMinimoRetoEntreCurvas, tempoMaximoRetoEntreCurvas);

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

        if (!desenharSensorNoEditor)
            return;

        Vector3 origem = Application.isPlaying
            ? ObterOrigemSensor()
            : transform.position + transform.up * alturaSensor + transform.right * offsetFrenteSensor;

        Vector3 frente = transform.right.normalized;
        Vector3 sensorPositivo = Quaternion.AngleAxis(anguloSensoresLaterais, Vector3.up) * frente;
        Vector3 sensorNegativo = Quaternion.AngleAxis(-anguloSensoresLaterais, Vector3.up) * frente;

        Gizmos.color = sensorDetectandoObstaculo ? Color.red : Color.cyan;
        Gizmos.DrawLine(origem, origem + frente * distanciaSensorFrontal);
        Gizmos.DrawWireSphere(origem + frente * distanciaSensorFrontal, raioSensorFrontal);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origem, origem + sensorPositivo.normalized * distanciaSensorFrontal);
        Gizmos.DrawLine(origem, origem + sensorNegativo.normalized * distanciaSensorFrontal);

        if (sensorDetectandoObstaculo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pontoSensorDetectado, 0.25f);
            Gizmos.DrawLine(pontoSensorDetectado, pontoSensorDetectado + normalSensorDetectado);
        }
    }
}
