using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameControllerRecursos : MonoBehaviour
{
    public static GameControllerRecursos Instance;

    [Header("Recursos")]
    public int pedra = 0;
    public int madeira = 0;
    public int metal = 0;

    [Header("Custo")]
    [SerializeField] private int custoBaseMetal = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text textoPedra;
    [SerializeField] private TMP_Text textoMadeira;
    [SerializeField] private TMP_Text textoMetal;

    [Header("Botões")]
    [SerializeField] private Button botaoBase;

    private void Awake()
    {
        Instance = this;
        AtualizarUI();
    }

    public void AdicionarRecurso(string tagDono, string tipoRecurso, int quantidade)
    {
        if (quantidade <= 0) return;
        if (tagDono != "Azul") return;

        if (tipoRecurso == "Pedra")
            pedra += quantidade;
        else if (tipoRecurso == "Arvore" || tipoRecurso == "Madeira")
            madeira += quantidade;
        else if (tipoRecurso == "Metal")
            metal += quantidade;

        AtualizarUI();
    }

    public bool PodeCriarBase()
    {
        return metal >= custoBaseMetal;
    }

    public bool GastarMetalParaBase()
    {
        if (!PodeCriarBase())
            return false;

        metal -= custoBaseMetal;
        AtualizarUI();
        return true;
    }

    private void AtualizarUI()
    {
        if (textoPedra != null)
            textoPedra.text = pedra.ToString();

        if (textoMadeira != null)
            textoMadeira.text = madeira.ToString();

        if (textoMetal != null)
            textoMetal.text = metal.ToString();

        if (botaoBase != null)
            botaoBase.interactable = PodeCriarBase();
    }
}