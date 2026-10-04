using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla o fim da partida.
///
/// DERROTA (somente da equipe do jogador): sem coletor E (sem Base Soldado e sem recursos
/// para reconstruir) OU (com Base Soldado, mas sem recursos para criar um coletor).
///
/// VITORIA: TODAS as equipes inimigas ficam nessa mesma condicao.
///
/// Derrota: mostra a imagem de Game Over, toca o audio e vai para a cena de Inicio.
/// Vitoria: mostra a imagem de Vitoria, toca o audio de aviso, depois a musica, e quando
///          o ultimo audio termina vai para a cena de Inicio.
/// </summary>
[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    private static readonly string[] TagsDeEquipe = { "Azul", "Vermelho", "Verde" };

    [Header("Equipe do jogador")]
    [Tooltip("Tag da equipe controlada pelo jogador. So ela pode causar Game Over.")]
    [SerializeField] private string equipeDoJogador = "Azul";

    [Header("Imagens (no Canvas do jogo, desativadas)")]
    [SerializeField] private GameObject painelGameOver;
    [SerializeField] private GameObject painelVitoria;

    [Header("Audio - comum")]
    [Tooltip("Opcional. Se vazio, o script cria um AudioSource 2D.")]
    [SerializeField] private AudioSource fonteAudio;
    [Tooltip("Opcional. Musica/ambiente do jogo que deve parar quando a partida acabar.")]
    [SerializeField] private AudioSource musicaDeFundoParaParar;
    [Tooltip("Tempo de espera se nao houver nenhum audio atribuido.")]
    [SerializeField] private float tempoSemAudio = 3f;

    [Header("Audio - derrota")]
    [SerializeField] private AudioClip somGameOver;

    [Header("Audio - vitoria (tocam em sequencia: aviso e depois musica)")]
    [SerializeField] private AudioClip somAvisoVitoria;
    [SerializeField] private AudioClip musicaVitoria;

    [Header("Configuracoes")]
    [Tooltip("Cena para onde o jogo vai ao terminar (vitoria ou derrota). Precisa estar no Build Settings.")]
    [SerializeField] private string nomeSceneInicio = "Inicio";
    [SerializeField] private float intervaloVerificacao = 2f;
    [Tooltip("Ignora as verificacoes nos primeiros segundos (a IA e as bases ainda estao sendo criadas).")]
    [SerializeField] private float tempoMinimoAntesDeVerificar = 5f;
    [Tooltip("Quantas verificacoes seguidas a condicao precisa se manter. Evita falso positivo " +
             "durante a producao de um coletor ou a construcao de uma base.")]
    [SerializeField, Min(1)] private int verificacoesConsecutivas = 2;

    private readonly HashSet<string> equipesAtivas = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> equipesEmPosicionamento = new HashSet<string>(StringComparer.Ordinal);

    // Reutilizados a cada verificacao (evita alocar listas novas).
    private readonly Dictionary<string, IGameOverRecoveryEconomy> economias =
        new Dictionary<string, IGameOverRecoveryEconomy>(StringComparer.Ordinal);
    private readonly List<IGameOverRecoverySpawner> basesSoldado = new List<IGameOverRecoverySpawner>();
    private readonly List<MonoBehaviour> coletores = new List<MonoBehaviour>();

    private float proximaVerificacao;
    private bool partidaEncerrada;
    private int contagemDerrota;
    private int contagemVitoria;

    // =====================================================================
    // UNITY
    // =====================================================================
    private void Start()
    {
        if (painelGameOver != null) painelGameOver.SetActive(false);
        if (painelVitoria != null) painelVitoria.SetActive(false);

        if (string.IsNullOrWhiteSpace(equipeDoJogador))
            Debug.LogError("[GameOver] 'Equipe Do Jogador' esta vazia. Nenhum Game Over sera detectado.", this);
        else
            equipesAtivas.Add(equipeDoJogador);

        intervaloVerificacao = Mathf.Max(0.1f, intervaloVerificacao);
        proximaVerificacao = Time.time + Mathf.Max(intervaloVerificacao, tempoMinimoAntesDeVerificar);

        RegistrarEquipesAtivas();
    }

    private void Update()
    {
        if (partidaEncerrada || Time.time < proximaVerificacao)
            return;

        proximaVerificacao = Time.time + intervaloVerificacao;
        AvaliarPartida();
    }

    // =====================================================================
    // AVALIACAO
    // =====================================================================
    private void AvaliarPartida()
    {
        if (string.IsNullOrWhiteSpace(equipeDoJogador))
            return;

        ColetarComponentes();
        AtualizarEquipesEmPosicionamento();

        bool jogadorPerdeu = EquipeSemRecuperacao(equipeDoJogador);

        // Vitoria: existe ao menos um inimigo conhecido e TODOS estao sem recuperacao.
        bool venceu = false;
        if (!jogadorPerdeu)
        {
            int inimigos = 0;
            bool todosEliminados = true;

            foreach (string tag in equipesAtivas)
            {
                if (!EhTagDeEquipe(tag) || string.Equals(tag, equipeDoJogador, StringComparison.Ordinal))
                    continue;

                inimigos++;
                if (!EquipeSemRecuperacao(tag))
                {
                    todosEliminados = false;
                    break;
                }
            }

            venceu = inimigos > 0 && todosEliminados;
        }

        contagemDerrota = jogadorPerdeu ? contagemDerrota + 1 : 0;
        contagemVitoria = venceu ? contagemVitoria + 1 : 0;

        if (contagemDerrota >= verificacoesConsecutivas)
            AtivarGameOver();
        else if (contagemVitoria >= verificacoesConsecutivas)
            AtivarVitoria();
    }

    /// <summary>
    /// True quando a equipe nao tem mais como se recuperar:
    /// sem coletor E sem base com recursos para criar coletor E sem recursos para reconstruir a base.
    /// </summary>
    private bool EquipeSemRecuperacao(string tagEquipe)
    {
        // Base sendo posicionada agora: a equipe ainda esta jogando.
        if (equipesEmPosicionamento.Contains(tagEquipe))
            return false;

        // Um coletor vivo ainda pode recuperar os recursos da equipe.
        if (ExisteColetorDaEquipe(tagEquipe))
            return false;

        bool temBaseSoldado = ExisteBaseDaEquipe(tagEquipe);
        economias.TryGetValue(tagEquipe, out IGameOverRecoveryEconomy economiaEquipe);

        // Sem base, mas com recursos para reconstrui-la, a partida fica ativa.
        if (!temBaseSoldado && economiaEquipe != null && economiaEquipe.PodeReconstruirBaseSoldado)
            return false;

        // Com base, ainda ha recuperacao possivel se ela puder criar um coletor.
        if (temBaseSoldado && BasePodeCriarColetor(tagEquipe))
            return false;

        return true;
    }

    // =====================================================================
    // COLETA DE COMPONENTES
    // =====================================================================
    private void ColetarComponentes()
    {
        economias.Clear();
        basesSoldado.Clear();
        coletores.Clear();

        MonoBehaviour[] componentes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour componente in componentes)
        {
            if (componente == null)
                continue;

            if (componente is IGameOverRecoveryEconomy economia
                && !string.IsNullOrWhiteSpace(economia.TagEquipe))
            {
                economias[economia.TagEquipe] = economia;
                equipesAtivas.Add(economia.TagEquipe);
            }

            if (componente is IGameOverRecoverySpawner baseSoldado)
            {
                basesSoldado.Add(baseSoldado);
                RegistrarEquipeDe(componente.transform);
            }

            if (componente is Coletor || componente is ColetorAi)
            {
                coletores.Add(componente);
                RegistrarEquipeDe(componente.transform);
            }
        }
    }

    private void RegistrarEquipesAtivas()
    {
        MonoBehaviour[] componentes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour componente in componentes)
        {
            if (componente == null)
                continue;

            if (componente is IGameOverRecoveryEconomy economia
                && !string.IsNullOrWhiteSpace(economia.TagEquipe))
                equipesAtivas.Add(economia.TagEquipe);

            if (componente is IGameOverRecoverySpawner || componente is Coletor || componente is ColetorAi)
                RegistrarEquipeDe(componente.transform);
        }
    }

    private void RegistrarEquipeDe(Transform objeto)
    {
        string tagEquipe = ObterTagEquipe(objeto);
        if (!string.IsNullOrWhiteSpace(tagEquipe))
            equipesAtivas.Add(tagEquipe);
    }

    private void AtualizarEquipesEmPosicionamento()
    {
        equipesEmPosicionamento.Clear();
        BaseArea[] areas = FindObjectsByType<BaseArea>(FindObjectsSortMode.None);
        foreach (BaseArea area in areas)
        {
            if (area == null || !area.EstaPosicionandoBaseSoldado)
                continue;

            string tagEquipe = area.TagDoJogador;
            if (!string.IsNullOrWhiteSpace(tagEquipe))
            {
                equipesAtivas.Add(tagEquipe);
                equipesEmPosicionamento.Add(tagEquipe);
            }
        }
    }

    private bool ExisteBaseDaEquipe(string tagEquipe)
    {
        foreach (IGameOverRecoverySpawner baseSoldado in basesSoldado)
        {
            if (baseSoldado is MonoBehaviour componente
                && componente != null
                && PertenceAEquipe(componente.transform, tagEquipe))
                return true;
        }
        return false;
    }

    private bool ExisteColetorDaEquipe(string tagEquipe)
    {
        foreach (MonoBehaviour coletor in coletores)
        {
            if (coletor != null && PertenceAEquipe(coletor.transform, tagEquipe))
                return true;
        }
        return false;
    }

    private bool BasePodeCriarColetor(string tagEquipe)
    {
        foreach (IGameOverRecoverySpawner baseSoldado in basesSoldado)
        {
            if (baseSoldado is MonoBehaviour componente
                && componente != null
                && PertenceAEquipe(componente.transform, tagEquipe)
                && baseSoldado.TemRecursosParaCriarColetor())
                return true;
        }
        return false;
    }

    private static bool EhTagDeEquipe(string tag)
    {
        for (int i = 0; i < TagsDeEquipe.Length; i++)
            if (string.Equals(TagsDeEquipe[i], tag, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool PertenceAEquipe(Transform objeto, string tagEquipe)
    {
        return string.Equals(ObterTagEquipe(objeto), tagEquipe, StringComparison.Ordinal);
    }

    private static string ObterTagEquipe(Transform objeto)
    {
        string tagGenericaMaisAlta = null;
        Transform atual = objeto;

        while (atual != null)
        {
            string tag = atual.tag;
            if (tag == "Azul" || tag == "Vermelho" || tag == "Verde")
                return tag;

            if (tag != "Untagged" && tag != "Player" && tag != "MainCamera"
                && tag != "Respawn" && tag != "Finish" && tag != "EditorOnly"
                && tag != "GameController")
                tagGenericaMaisAlta = tag;

            atual = atual.parent;
        }

        return tagGenericaMaisAlta;
    }

    // =====================================================================
    // FIM DE PARTIDA
    // =====================================================================
    private void AtivarGameOver()
    {
        if (partidaEncerrada) return;
        partidaEncerrada = true;

        Debug.Log($"[GameOver] DERROTA: a equipe {equipeDoJogador} ficou sem condicao de recuperacao.");
        Time.timeScale = 0f;

        if (painelGameOver != null)
            painelGameOver.SetActive(true);
        else
            Debug.LogError("[GameOver] Imagem de Game Over nao atribuida no Inspector!", this);

        StartCoroutine(RotinaFinal(somGameOver));
    }

    private void AtivarVitoria()
    {
        if (partidaEncerrada) return;
        partidaEncerrada = true;

        Debug.Log("[GameOver] VITORIA: todas as equipes inimigas ficaram sem condicao de recuperacao.");
        Time.timeScale = 0f;

        if (painelVitoria != null)
            painelVitoria.SetActive(true);
        else
            Debug.LogError("[GameOver] Imagem de Vitoria nao atribuida no Inspector!", this);

        StartCoroutine(RotinaFinal(somAvisoVitoria, musicaVitoria));
    }

    /// <summary>Toca os clipes em sequencia e, quando o ultimo termina, vai para a cena de Inicio.</summary>
    private IEnumerator RotinaFinal(params AudioClip[] clips)
    {
        PrepararFonteAudio();

        if (musicaDeFundoParaParar != null)
            musicaDeFundoParaParar.Stop();

        bool tocouAlgum = false;

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            fonteAudio.PlayOneShot(clip);
            tocouAlgum = true;

            // Tempo real: o jogo esta com timeScale = 0.
            yield return new WaitForSecondsRealtime(clip.length);
        }

        if (!tocouAlgum)
        {
            Debug.LogWarning("[GameOver] Nenhum AudioClip atribuido; usando tempo fixo.", this);
            yield return new WaitForSecondsRealtime(tempoSemAudio);
        }

        IrParaInicio();
    }

    private void PrepararFonteAudio()
    {
        if (fonteAudio == null)
            fonteAudio = gameObject.AddComponent<AudioSource>();

        fonteAudio.playOnAwake = false;
        fonteAudio.loop = false;
        fonteAudio.spatialBlend = 0f; // 2D, sem atenuacao por distancia
    }

    private void IrParaInicio()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(nomeSceneInicio))
            SceneManager.LoadScene(nomeSceneInicio);
        else
            Debug.LogError($"[GameOver] Cena '{nomeSceneInicio}' nao esta no Build Settings.", this);
    }
}