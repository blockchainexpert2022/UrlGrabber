﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // URL de la page à analyser
        string pageUrl = "https://www.lefigaro.fr/";

        // Ensemble pour stocker les URLs déjà traitées
        HashSet<string> visitedUrls = new HashSet<string>();

        try
        {
            // Créez une instance de HttpClient
            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) })
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                // Étape 1 : Récupérer le contenu de la page HTML
                Console.WriteLine($"Récupération de la page : {pageUrl}");
                string pageContent = await client.GetStringAsync(pageUrl);

                // Étape 2 : Extraire les liens HTTP avec une expression régulière
                List<string> httpLinks = ExtractHttpLinks(pageContent);

                Console.WriteLine($"Nombre de liens trouvés : {httpLinks.Count}");

                // Étape 3 : Envoyer une requête HTTP à chaque lien trouvé
                foreach (string link in httpLinks)
                {
                    // Vérifie si l'URL a déjà été traitée
                    if (visitedUrls.Contains(link))
                    {
                        Console.WriteLine($"Lien déjà traité, passons : {link}");
                        continue; // Saute les liens déjà visités
                    }

                    // Ajoute l'URL au HashSet
                    visitedUrls.Add(link);

                    Console.WriteLine($"Envoi d'une requête à : {link}");

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(link);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Succès ({response.StatusCode}) pour : {link}");
                        }
                        else
                        {
                            Console.WriteLine($"Erreur ({response.StatusCode}) pour : {link}");
                        }

                        // Pause entre les requêtes
                        await Task.Delay(1500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception lors de la requête vers {link} : {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception générale : {ex.Message}");
        }
    }

    // Méthode pour extraire tous les liens HTTP d'une chaîne de texte
    static List<string> ExtractHttpLinks(string htmlContent)
    {
        // Expression régulière pour trouver tous les liens HTTP/HTTPS
        string pattern = @"https?://[^\s""'<>]+";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        MatchCollection matches = regex.Matches(htmlContent);

        List<string> links = new List<string>();
        foreach (Match match in matches)
        {
            links.Add(match.Value);
        }

        return links;
    }
}