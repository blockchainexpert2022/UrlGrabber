using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    // Compteur global pour compter les requêtes HTTP
    private static int TotalHttpRequests = 0;

    static async Task Main(string[] args)
    {
        // URL de la page à analyser
        string initialPageUrl = "https://www.pcsoft.fr/";

        try
        {
            // Créez une instance de HttpClient avec un délai d'expiration de 5 secondes
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5); // Configure un timeout de 5 secondes

                // Ensemble pour garder une trace des pages visitées
                HashSet<string> visitedUrls = new HashSet<string>();

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
        if (visitedUrls.Contains(pageUrl))
        {
            // Si l'URL a déjà été visitée, on arrête pour éviter des boucles infinies
            return;
        }

        Console.WriteLine($"Récupération de la page : {pageUrl}");
        visitedUrls.Add(pageUrl); // Marquer l'URL comme visitée

        try
        {
            // Récupérer le contenu de la page HTML
            string pageContent = await client.GetStringAsync(pageUrl);

            // Extraire les liens HTTP
            List<string> httpLinks = ExtractHttpLinks(pageContent);

            Console.WriteLine($"Nombre de liens trouvés sur {pageUrl} : {httpLinks.Count}");

            // Envoyer une requête HTTP à chaque lien trouvé, sauf ceux à exclure
            foreach (string link in httpLinks)
            {
                // Exclure les liens dont la base est www.w3.org
                if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri) && uri.Host == "www.w3.org")
                {
                    Console.WriteLine($"Lien exclu (w3.org) : {link}");
                    continue;
                }

                TotalHttpRequests++; // Incrémenter le compteur
                Console.WriteLine($"[{TotalHttpRequests}] Envoi d'une requête à : {link}");

                try
                {
                    HttpResponseMessage response = await client.GetAsync(link);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Succès ({response.StatusCode}) pour : {link}");

                        // Appeler récursivement si la page est bien accessible
                        await CrawlPageAsync(client, link, visitedUrls);
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

        MatchCollection matches = regex.Matches(htmlContent);

        List<string> links = new List<string>();
        foreach (Match match in matches)
        {
            links.Add(match.Value);
        }

        return links;
    }
}