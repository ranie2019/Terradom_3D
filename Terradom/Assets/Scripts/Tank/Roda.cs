using UnityEngine;

[DisallowMultipleComponent]
public class Roda : MonoBehaviour
{
    [Header("Rodas que giram no eixo Z")]
    [SerializeField] private Transform roda1;
    [SerializeField] private Transform roda2;
    [SerializeField] private Transform roda3;
    [SerializeField] private Transform roda4;
    [SerializeField] private Transform roda5;
    [SerializeField] private Transform roda6;

    [Header("Inversao do giro Z por roda")]
    [SerializeField] private bool inverterRoda1 = true;
    [SerializeField] private bool inverterRoda2 = false;
    [SerializeField] private bool inverterRoda3 = true;
    [SerializeField] private bool inverterRoda4 = false;
    [SerializeField] private bool inverterRoda5 = false;
    [SerializeField] private bool inverterRoda6 = true;

    [Header("Rodas dianteiras que viram no eixo Y")]
    [SerializeField] private bool roda1EhDianteiraDirecional = true;
    [SerializeField] private bool roda2EhDianteiraDirecional = true;
    [SerializeField] private bool roda3EhDianteiraDirecional = false;
    [SerializeField] private bool roda4EhDianteiraDirecional = false;
    [SerializeField] private bool roda5EhDianteiraDirecional = false;
    [SerializeField] private bool roda6EhDianteiraDirecional = false;

    private float velocidadeRotacaoAtual;
    private float anguloGiroAtual;
    private float anguloDirecaoDianteiraAtual;
    private bool rodasAtivas;
    private bool rotacoesOriginaisCapturadas;

    private Quaternion rotOriginalRoda1;
    private Quaternion rotOriginalRoda2;
    private Quaternion rotOriginalRoda3;
    private Quaternion rotOriginalRoda4;
    private Quaternion rotOriginalRoda5;
    private Quaternion rotOriginalRoda6;

    private void Awake()
    {
        CapturarRotacoesOriginais();
    }

    private void OnEnable()
    {
        if (!rotacoesOriginaisCapturadas)
            CapturarRotacoesOriginais();
    }

    private void Update()
    {
        if (rodasAtivas && Mathf.Abs(velocidadeRotacaoAtual) > 0.01f)
        {
            anguloGiroAtual += velocidadeRotacaoAtual * Time.deltaTime;

            if (anguloGiroAtual > 360f || anguloGiroAtual < -360f)
                anguloGiroAtual = Mathf.Repeat(anguloGiroAtual, 360f);
        }

        AplicarVisualDasRodas();
    }

    private void CapturarRotacoesOriginais()
    {
        rotOriginalRoda1 = roda1 != null ? roda1.localRotation : Quaternion.identity;
        rotOriginalRoda2 = roda2 != null ? roda2.localRotation : Quaternion.identity;
        rotOriginalRoda3 = roda3 != null ? roda3.localRotation : Quaternion.identity;
        rotOriginalRoda4 = roda4 != null ? roda4.localRotation : Quaternion.identity;
        rotOriginalRoda5 = roda5 != null ? roda5.localRotation : Quaternion.identity;
        rotOriginalRoda6 = roda6 != null ? roda6.localRotation : Quaternion.identity;

        rotacoesOriginaisCapturadas = true;
    }

    private void AplicarVisualDasRodas()
    {
        AplicarVisualRoda(roda1, rotOriginalRoda1, inverterRoda1, roda1EhDianteiraDirecional);
        AplicarVisualRoda(roda2, rotOriginalRoda2, inverterRoda2, roda2EhDianteiraDirecional);
        AplicarVisualRoda(roda3, rotOriginalRoda3, inverterRoda3, roda3EhDianteiraDirecional);
        AplicarVisualRoda(roda4, rotOriginalRoda4, inverterRoda4, roda4EhDianteiraDirecional);
        AplicarVisualRoda(roda5, rotOriginalRoda5, inverterRoda5, roda5EhDianteiraDirecional);
        AplicarVisualRoda(roda6, rotOriginalRoda6, inverterRoda6, roda6EhDianteiraDirecional);
    }

    private void AplicarVisualRoda(Transform roda, Quaternion rotacaoOriginal, bool inverterGiro, bool rodaDirecional)
    {
        if (roda == null)
            return;

        float sentidoGiro = inverterGiro ? -1f : 1f;
        float giroZ = anguloGiroAtual * sentidoGiro;

        Quaternion rotacaoDirecaoY = rodaDirecional
            ? Quaternion.Euler(0f, anguloDirecaoDianteiraAtual, 0f)
            : Quaternion.identity;

        Quaternion rotacaoGiroZ = Quaternion.Euler(0f, 0f, giroZ);

        roda.localRotation = rotacaoOriginal * rotacaoDirecaoY * rotacaoGiroZ;
    }

    public void DefinirVelocidadeRotacao(float velocidadeEmGrausPorSegundo)
    {
        velocidadeRotacaoAtual = velocidadeEmGrausPorSegundo;
        rodasAtivas = Mathf.Abs(velocidadeRotacaoAtual) > 0.01f;
    }

    public void DefinirAnguloDirecaoDianteira(float anguloEmGraus)
    {
        anguloDirecaoDianteiraAtual = anguloEmGraus;
    }

    public void CentralizarDirecaoDianteira()
    {
        anguloDirecaoDianteiraAtual = 0f;
        AplicarVisualDasRodas();
    }

    public void PararRodas()
    {
        velocidadeRotacaoAtual = 0f;
        rodasAtivas = false;
    }

    public void AtivarGiro()
    {
        rodasAtivas = true;
    }

    public void PararGiro()
    {
        PararRodas();
    }
}
