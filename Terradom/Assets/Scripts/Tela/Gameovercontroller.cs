using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    [Header("Imagem de Game Over (no Canvas do jogo, desativada)")]
    [SerializeField] private GameObject painelGameOver;

    [Header("Audio")]
    [SerializeField] private AudioSource fonteAudio;
    [SerializeField] private AudioClip somGameOver;
    [Tooltip("Tempo de espera se nao houver audio atribuido.")]
    [SerializeField] private float tempoSemAudio = 3f;

    [Header("Configuracoes")]
    [SerializeField] private string nomeSceneGameOver = "GameOver";
    [SerializeField] private float intervaloVerificacao = 2f;

    private readonly HashSet<string> equipesAtivas = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> equipesEmPosicionamento = new HashSet<string>(StringComparer.Ordinal);
    private float proximaVerificacao;
    private bool gameOverAtivado;
    private string equipeEmDerrota;

    private void Start()
    {
        if (painelGameOver != null)
            painelGameOver.SetActive(false);

        intervaloVerificacao = Mathf.Max(0.1f, intervaloVerificacao);
        proximaVerificacao = Time.time + intervaloVerificacao;
        RegistrarEquipesAtivas();
    }

    private void Update()
    {
        if (gameOverAtivado || Time.time < proximaVerificacao)
            return;

        proximaVerificacao = Time.time + intervaloVerificacao;
        if (VerificarDerrota())
            AtivarGameOver();
    }

    private bool VerificarDerrota()
    {
        MonoBehaviour[] componentes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        Dictionary<string, IGameOverRecoveryEconomy> economias = new Dictionary<string, IGameOverRecoveryEconomy>(StringComparer.Ordinal);
        List<IGameOverRecoverySpawner> basesSoldado = new List<IGameOverRecoverySpawner>();
        List<MonoBehaviour> coletores = new List<MonoBehaviour>();

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
                string tagEquipe = ObterTagEquipe(componente.transform);
                if (!string.IsNullOrWhiteSpace(tagEquipe))
                    equipesAtivas.Add(tagEquipe);
            }

            if (componente is Coletor || componente is ColetorAi)
            {
                coletores.Add(componente);
                string tagEquipe = ObterTagEquipe(componente.transform);
                if (!string.IsNullOrWhiteSpace(tagEquipe))
                    equipesAtivas.Add(tagEquipe);
            }
        }

        AtualizarEquipesEmPosicionamento();

        foreach (string tagEquipe in equipesAtivas)
        {
            if (equipesEmPosicionamento.Contains(tagEquipe))
                continue;

            bool temBaseSoldado = ExisteBaseDaEquipe(tagEquipe, basesSoldado);
            bool temColetor = ExisteColetorDaEquipe(tagEquipe, coletores);

            // Um coletor vivo ainda pode recuperar os recursos da equipe.
            if (temColetor)
                continue;

            economias.TryGetValue(tagEquipe, out IGameOverRecoveryEconomy economiaEquipe);

            // Sem base, mas com recursos para reconstrui-la, a partida fica ativa.
            if (!temBaseSoldado && economiaEquipe != null && economiaEquipe.PodeReconstruirBaseSoldado)
                continue;

            // Com base, ainda ha recuperacao possivel se ela puder criar um coletor.
            if (temBaseSoldado && BasePodeCriarColetor(tagEquipe, basesSoldado))
                continue;

            equipeEmDerrota = tagEquipe;
            Debug.Log($"[GameOver] Equipe {tagEquipe} ficou sem uma condicao de recuperacao.");
            return true;
        }

        return false;
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
            {
                string tagEquipe = ObterTagEquipe(componente.transform);
                if (!string.IsNullOrWhiteSpace(tagEquipe))
                    equipesAtivas.Add(tagEquipe);
            }
        }
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

    private static bool ExisteBaseDaEquipe(string tagEquipe, List<IGameOverRecoverySpawner> bases)
    {
        foreach (IGameOverRecoverySpawner baseSoldado in bases)
        {
            if (baseSoldado is MonoBehaviour componente
                && componente != null
                && PertenceAEquipe(componente.transform, tagEquipe))
                return true;
        }

        return false;
    }

    private static bool ExisteColetorDaEquipe(string tagEquipe, List<MonoBehaviour> coletores)
    {
        foreach (MonoBehaviour coletor in coletores)
        {
            if (coletor != null && PertenceAEquipe(coletor.transform, tagEquipe))
                return true;
        }

        return false;
    }

    private static bool BasePodeCriarColetor(string tagEquipe, List<IGameOverRecoverySpawner> bases)
    {
        foreach (IGameOverRecoverySpawner baseSoldado in bases)
        {
            if (baseSoldado is MonoBehaviour componente
                && componente != null
                && PertenceAEquipe(componente.transform, tagEquipe)
                && baseSoldado.TemRecursosParaCriarColetor())
                return true;
        }

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

    private void AtivarGameOver()
    {
        gameOverAtivado = true;
        Debug.Log($"[GameOver] GAME OVER! Equipe eliminada: {equipeEmDerrota}.");

        // Congela o jogo; o audio continua tocando e a espera usa tempo real.
        Time.timeScale = 0f;

        if (painelGameOver != null)
            painelGameOver.SetActive(true);
        else
            Debug.LogError("[GameOver] Imagem de Game Over nao atribuida no Inspector!");

        StartCoroutine(RotinaGameOver());
    }

    private IEnumerator RotinaGameOver()
    {
        float espera = tempoSemAudio;

        if (somGameOver != null)
        {
            if (fonteAudio == null)
                fonteAudio = gameObject.AddComponent<AudioSource>();

            fonteAudio.playOnAwake = false;
            fonteAudio.spatialBlend = 0f; // 2D, sem atenuacao por distancia
            fonteAudio.PlayOneShot(somGameOver);
            espera = somGameOver.length;
        }
        else
        {
            Debug.LogWarning("[GameOver] Nenhum AudioClip atribuido; usando tempo fixo.");
        }

        yield return new WaitForSecondsRealtime(espera);

        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(nomeSceneGameOver))
            SceneManager.LoadScene(nomeSceneGameOver);
        else
            Debug.LogError($"[GameOver] Cena '{nomeSceneGameOver}' nao esta no Build Settings.");
    }
}