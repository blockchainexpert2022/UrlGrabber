using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // URL de départ
        string startUrl = "https://doc.pcsoft.fr/fr-FR/search2.awp?q=HOuvreConnexion&mode=index&origin=searchbox";

        // Profondeur maximale de l'exploration
        int maxDepth = 2;

        try
        {
            // Lancement de l'exploration récursive
            Console.WriteLine($"Démarrage de l'exploration depuis : {startUrl} (Profondeur maximale : {maxDepth})");
            HashSet<string> visitedUrls = new HashSet<string>();
            await ExploreUrlRecursively(startUrl, visitedUrls, maxDepth, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception générale : {ex.Message}");
        }
    }

    /// <summary>
    /// Méthode pour explorer les URLs de façon récursive jusqu'à une profondeur donnée.
    /// </summary>
    /// <param name="url">L'URL actuelle à explorer.</param>
    /// <param name="visitedUrls">HashSet pour suivre les URLs visitées.</param>
    /// <param name="maxDepth">Profondeur maximale autorisée.</param>
    /// <param name="currentDepth">Profondeur actuelle.</param>
    private static async Task ExploreUrlRecursively(string url, HashSet<string> visitedUrls, int maxDepth, int currentDepth)
    {
        // Vérifie si la profondeur maximale est atteinte
        if (currentDepth > maxDepth)
        {
            Console.WriteLine($"Profondeur maximale atteinte pour : {url}");
            return;
        }

        // Vérifie si l'URL a déjà été visitée
        if (visitedUrls.Contains(url))
        {
            Console.WriteLine($"URL déjà visitée, passons : {url}");
            return;
        }

        Console.WriteLine($"Exploration (profondeur {currentDepth}) : {url}");

        try
        {
            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) })
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                // Récupère le contenu de la page
                string pageContent = await client.GetStringAsync(url);

                // Ajoute l'URL au HashSet des URLs visitées
                visitedUrls.Add(url);

                // Extraire les liens HTTP de la page
                List<string> httpLinks = ExtractHttpLinks(pageContent);

                Console.WriteLine($"Nombre de liens trouvés sur {url} : {httpLinks.Count}");

                // Boucle sur chaque lien extrait
                foreach (string link in httpLinks)
                {
                    // Exploration récursive de chaque lien trouvé
                    await ExploreUrlRecursively(link, visitedUrls, maxDepth, currentDepth + 1);

                    // Pause (optionnelle) entre les requêtes pour éviter une surcharge
                    await Task.Delay(1000);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception lors de la requête vers {url} : {ex.Message}");
        }
    }

    /// <summary>
    /// Méthode pour extraire les liens HTTP/HTTPS d'un contenu HTML.
    /// </summary>
    /// <param name="htmlContent">Contenu HTML à analyser.</param>
    /// <returns>Liste de liens trouvés.</returns>
    private static List<string> ExtractHttpLinks(string htmlContent)
    {
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