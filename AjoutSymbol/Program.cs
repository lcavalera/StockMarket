using System.Text.RegularExpressions;

string inputFilePath = "../../../../Bourse/TSX.txt";
string outputFilePath = "../../../../TSX.txt";
//string filePath = "chemin/vers/votre/fichier.txt";

// Lire le fichier et filtrer les lignes dont le symbole contient un trait d'union
var lines = File.ReadLines(inputFilePath)
    .Where(line => !line.Split('\t')[0].Contains("-"))  // Filtrer les lignes dont le symbole contient un trait d'union
    .ToList();  // Convertir en liste pour pouvoir réécrire tout le contenu

// Réécrire le fichier avec les lignes filtrées
File.WriteAllLines(outputFilePath, lines);

Console.WriteLine("Les lignes contenant un trait d'union dans le symbole ont été supprimées.");

//// Modifier tous les symboles dans un fichier TXT avec deux trait d'union

////Chemin du file txt d'origine
//string inputFilePath = "../../../../Bourse/TSX.txt";
//string outputFilePath = "../../../../TSX.txt";

//// Lire chaque ligne du fichier d'entrée
//var lines = File.ReadAllLines(inputFilePath);

//// Préparer la liste des lignes modifiées
//var modifiedLines = new List<string>();

//foreach (var line in lines)
//{
//    // Séparer le nom du reste de la ligne (avant la première tabulation)
//    var parts = line.Split('\t');
//    if (parts.Length == 0) continue; // Sauter les lignes vides

//    string symbol = parts[0]; // La première partie est le nom (symbole)

//    // Vérifier s'il y a deux traits d'union dans le symbole
//    int firstDashIndex = symbol.IndexOf('-');
//    int secondDashIndex = symbol.IndexOf('-', firstDashIndex + 1);

//    if (secondDashIndex > -1)
//    {
//        // Si "PR" se trouve avant le deuxième trait d'union
//        if (symbol.Substring(firstDashIndex + 1, 2) == "PR")
//        {
//            // Supprimer "R-" (rechercher et supprimer exactement "R-")
//            symbol = symbol.Remove(secondDashIndex - 1, 2);
//        }
//        else
//        {
//            // Sinon, supprimer juste le deuxième trait d'union
//            symbol = symbol.Remove(secondDashIndex, 1);
//        }
//    }

//    // Reconstruire la ligne avec le symbole modifié et le reste des parties
//    modifiedLines.Add(symbol + "\t" + string.Join("\t", parts.Skip(1)));
//}

//// Écrire les lignes modifiées dans un nouveau fichier
//File.WriteAllLines(outputFilePath, modifiedLines);

//Console.WriteLine("Modifications appliquées avec succès !");

//// Modifier tous les symboles dans un fichier TXT depuis des fichier TXT

////Chemin du file txt d'origine
//string inputFilePath = "../../../../Bourse/TSX.txt"; // Remplacez par le chemin de votre fichier
//string outputFilePath = "../../../../TSX.txt"; // Chemin pour le fichier modifié

//// Lire le contenu du fichier ligne par ligne
//string[] lines = await File.ReadAllLinesAsync(inputFilePath);

//for (int i = 0; i < lines.Length; i++)
//{
//    // Séparer chaque ligne en deux colonnes (avant et après la tabulation)
//    string[] columns = lines[i].Split('\t');

//    if (columns.Length > 0)
//    {
//        // Remplacer les points dans la première colonne uniquement s'il y a plus d'un point
//        if (columns[0].Count(c => c == '.') > 1)
//        {
//            columns[0] = Regex.Replace(columns[0].Substring(0, columns[0].Length - 3), @"\.", "-") + ".TO";
//        }

//        // Reformer la ligne avec la tabulation
//        lines[i] = string.Join("\t", columns);
//    }
//}

//// Écrire les lignes modifiées dans un nouveau fichier
//await File.WriteAllLinesAsync(outputFilePath, lines);

//Console.WriteLine("Les modifications ont été effectuées avec succès.");


// Ajouter tous les symboles dans un fichier CSV depuis des fichier TXT

//Chemin du file txt d'origine
//using (var reader = new StreamReader("../../../../Bourse/AMEX.txt"))
//{
//    while (!reader.EndOfStream)
//    {
//        var line = reader.ReadLine();

//        if (!string.IsNullOrEmpty(line))
//        {
//            string[] parts = line.Split('\t');

//            // Chemin du fichier CSV
//            string fichierCsv = "../../../../Bourse/symbols_valid_meta.csv";

//            // Lire toutes les lignes du fichier CSV
//            List<string> lignes = File.ReadAllLines(fichierCsv).ToList();

//            // Vérifier si le symbole est déjà présent dans la première colonne
//            bool symbolePresent = lignes.Any(ligne =>
//            {
//                // Diviser la ligne en colonnes
//                string[] colonnes = ligne.Split(',');

//                string symb = colonnes[1];

//                // Vérifier que la ligne n'est pas vide et qu'il y a au moins une colonne
//                return colonnes.Length > 0 && colonnes[1] == parts[0];
//            });

//            if (!symbolePresent)
//            {
//                // Construire la nouvelle ligne (remplace les colonnes avec les valeurs appropriées)
//                string nouvelleLigne = $"Y,{parts[0]},{parts[1]},N, ,N,100.0,N,,{parts[0]},{parts[0]},N";

//                // Ajouter la nouvelle ligne au fichier CSV
//                File.AppendAllText(fichierCsv, nouvelleLigne + Environment.NewLine);

//                Console.WriteLine($"Le symbole '{parts[0]}' a été ajouté.");
//            }
//            else
//            {
//                Console.WriteLine($"Le symbole '{parts[0]}' est déjà présent.");
//            }
//        }
//    }
//}