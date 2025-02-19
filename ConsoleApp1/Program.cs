﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    // Compteur global pour compter les requêtes HTTP
    private static int TotalHttpRequests = 0;

    private static string baseUrl = "";

    static async Task Main(string[] args)
    {
        // URL de la page à analyser
        string initialPageUrl = "https://www.lefigaro.fr";
        Uri uri = new Uri(initialPageUrl);
        baseUrl = uri.GetLeftPart(UriPartial.Authority); // Base URL (ex : https://www.microsoft.com)
        Console.WriteLine("baseUrl : " + baseUrl);

        try
        {
            // Créez une instance de HttpClient avec un délai d'expiration de 5 secondes
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5); // Configure un timeout de 5 secondes
                
                // Ajoutez l'en-tête User-Agent pour ressembler à un navigateur Chrome
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");

                // Ensemble pour garder une trace des pages visitées (insensible à la casse)
                HashSet<string> visitedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Lancer l'analyse initiale
                await CrawlPageAsync(client, initialPageUrl, visitedUrls);

                // Afficher le nombre total de requêtes HTTP effectuées
                Console.WriteLine($"\nNombre total de requêtes HTTP envoyées : {TotalHttpRequests}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception générale : {ex.Message}");
        }
    }

    // Méthode pour explorer une page et ses liens
    static async Task CrawlPageAsync(HttpClient client, string pageUrl, HashSet<string> visitedUrls)
    {
        // Supprimer les fragments avant vérification
        string cleanUrl = pageUrl.Split('#')[0];

        if (visitedUrls.Contains(cleanUrl))
        {
            // Si l'URL a déjà été visitée, on arrête pour éviter des boucles infinies
            return;
        }
        
        Console.WriteLine($"Récupération de la page : {cleanUrl}");
        visitedUrls.Add(cleanUrl); // Marquer l'URL comme visitée

        try
        {
            // Récupérer le contenu de la page HTML
            string pageContent = await client.GetStringAsync(cleanUrl);

            // Extraire les liens HTTP
            List<string> httpLinks = ExtractHttpLinks(pageContent);

            Console.WriteLine($"Nombre de liens trouvés sur {cleanUrl} : {httpLinks.Count}");

            // Envoyer une requête HTTP à chaque lien trouvé, sauf ceux à exclure
            foreach (string link in httpLinks)
            {
                // Exclure les URL ne correspondant pas au domaine de base
                if (!link.StartsWith(baseUrl))
                {
                    continue;
                }

                bool bBypass = false;
                List<string> lstExclude = new List<string> { ".woff2", ".ico", ".css", ".js" };
                foreach (string exclude in lstExclude)
                {
                    if (link.ToLower().EndsWith(exclude))
                    {
                        bBypass = true;
                        break;
                    }
                }
                    
                if (bBypass)
                {
                    continue;
                }
                
                TotalHttpRequests++; // Incrémenter le compteur
                Console.WriteLine($"[{TotalHttpRequests}] Envoi d'une requête à : {link}");

                try
                {
                    HttpResponseMessage response = await client.GetAsync(link);

                    // Si la requête est un succès
                    if (response.IsSuccessStatusCode)
                    {
                        // Récupérer l'URL finale après redirection
                        string finalUrl = response.RequestMessage.RequestUri.ToString();

                        // Vérifier si l'URL finale a déjà été visitée
                        if (!visitedUrls.Contains(finalUrl.Split('#')[0]))
                        {
                            Console.WriteLine($"Succès ({response.StatusCode}) pour : {finalUrl}");

                            // Ajouter l'URL finale et appeler récursivement
                            await CrawlPageAsync(client, finalUrl, visitedUrls);
                        }
                        else
                        {
                            Console.WriteLine($"URL finale déjà visitée : {finalUrl}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Erreur ({response.StatusCode}) pour : {link}");
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    // Gestion particulière si la tâche est annulée à cause d'un timeout
                    Console.WriteLine($"Timeout (5 secondes) atteint pour : {link}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception lors de la requête vers {link} : {ex.Message}");
                }

                // Temporisation de 0,5 seconde après chaque requête
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération de la page {pageUrl} : {ex.Message}");
        }
    }

    // Méthode pour extraire tous les liens HTTP d'une chaîne de texte
    static List<string> ExtractHttpLinks(string htmlContent)
    {
        // Expression régulière pour trouver tous les liens HTTP/HTTPS
        string pattern = @"https?://[^\s""'<>]+";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        HashSet<string> uniqueLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in regex.Matches(htmlContent))
        {
            // Ajouter sans doublons
            uniqueLinks.Add(match.Value.Split('#')[0]); // Ignorer les fragments
        }

        return new List<string>(uniqueLinks);
    }
}