using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Guarda estatisticas e ajustes adaptativos dos tanques em JSON local.
/// Os dados ficam em Application.persistentDataPath e sobrevivem a novas partidas.
/// </summary>
public static class TankLearningDatabase
{
    [Serializable]
    private sealed class Banco
    {
        public int versao = 1;
        public List<Perfil> perfis = new List<Perfil>();
    }

    [Serializable]
    private sealed class Perfil
    {
        public string equipe;
        public int tiros;
        public int acertos;
        public int erros;
        public int danosRecebidos;
        public float distanciaEngajamento = 28f;
        public int ladoManobra = 1;
    }

    private static Banco banco;
    private static bool sujo;
    private static float salvarApos;
    private static string Caminho => Path.Combine(Application.persistentDataPath, "terradom-tank-learning.json");

    public static float ObterDistanciaEngajamento(string equipe, float padrao)
    {
        Perfil perfil = ObterPerfil(equipe, padrao);
        return Mathf.Clamp(perfil.distanciaEngajamento, 12f, 60f);
    }

    public static int ObterLadoManobra(string equipe, float padrao)
    {
        return ObterPerfil(equipe, padrao).ladoManobra >= 0 ? 1 : -1;
    }

    public static float ObterMultiplicadorDeEvasao(string equipe, float padrao)
    {
        Perfil perfil = ObterPerfil(equipe, padrao);
        return Mathf.Clamp(1f + perfil.danosRecebidos * 0.015f, 1f, 1.4f);
    }

    public static void RegistrarDisparo(string equipe, bool acertou, float distancia, float padrao)
    {
        Perfil perfil = ObterPerfil(equipe, padrao);
        perfil.tiros++;

        if (acertou)
        {
            perfil.acertos++;
            if (distancia > 0f)
                perfil.distanciaEngajamento = Mathf.Lerp(perfil.distanciaEngajamento,
                    Mathf.Clamp(distancia, 12f, 60f), 0.1f);
        }
        else
        {
            perfil.erros++;
            // Se os disparos estao errando, aproxima gradualmente o proximo combate.
            perfil.distanciaEngajamento = Mathf.Max(12f, perfil.distanciaEngajamento - 1f);
        }

        MarcarAlteracao();
    }

    public static void RegistrarDanoRecebido(string equipe, float padrao)
    {
        Perfil perfil = ObterPerfil(equipe, padrao);
        perfil.danosRecebidos++;
        perfil.ladoManobra *= -1;
        MarcarAlteracao();
    }

    public static void SalvarSeNecessario()
    {
        if (sujo && Time.unscaledTime >= salvarApos)
            Salvar();
    }

    public static void Salvar()
    {
        if (!sujo || banco == null) return;

        try
        {
            string json = JsonUtility.ToJson(banco, true);
            File.WriteAllText(Caminho, json);
            sujo = false;
        }
        catch (Exception excecao)
        {
            Debug.LogWarning($"[TankLearningDatabase] Nao foi possivel salvar os dados: {excecao.Message}");
        }
    }

    private static Perfil ObterPerfil(string equipe, float padrao)
    {
        Carregar();
        string chave = string.IsNullOrWhiteSpace(equipe) ? "Geral" : equipe;

        for (int i = 0; i < banco.perfis.Count; i++)
        {
            if (banco.perfis[i].equipe == chave)
            {
                if (banco.perfis[i].distanciaEngajamento <= 0f)
                    banco.perfis[i].distanciaEngajamento = Mathf.Clamp(padrao, 12f, 60f);
                return banco.perfis[i];
            }
        }

        Perfil novo = new Perfil
        {
            equipe = chave,
            distanciaEngajamento = Mathf.Clamp(padrao, 12f, 60f)
        };
        banco.perfis.Add(novo);
        MarcarAlteracao();
        return novo;
    }

    private static void Carregar()
    {
        if (banco != null) return;

        banco = new Banco();
        try
        {
            if (File.Exists(Caminho))
            {
                Banco carregado = JsonUtility.FromJson<Banco>(File.ReadAllText(Caminho));
                if (carregado != null)
                    banco = carregado;
            }
        }
        catch (Exception excecao)
        {
            Debug.LogWarning($"[TankLearningDatabase] Dados anteriores invalidos; iniciando perfil novo: {excecao.Message}");
        }

        if (banco.perfis == null)
            banco.perfis = new List<Perfil>();
    }

    private static void MarcarAlteracao()
    {
        if (!sujo)
            salvarApos = Time.unscaledTime + 5f;
        sujo = true;
    }
}
