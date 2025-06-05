using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Yafes.Managers
{
    public class GameSearchManager
    {
        private readonly List<Yafes.Models.GameData> _allGames;
        private readonly Func<Yafes.Models.GameData, Task<System.Windows.Controls.Border>> _createGameCardFunc;

        public GameSearchManager(List<Yafes.Models.GameData> allGames,
            Func<Yafes.Models.GameData, Task<System.Windows.Controls.Border>> createGameCardFunc)
        {
            _allGames = allGames ?? new List<Yafes.Models.GameData>();
            _createGameCardFunc = createGameCardFunc ?? throw new ArgumentNullException(nameof(createGameCardFunc));
        }

        public async Task PerformSearchAsync(string searchText, UniformGrid gamesGrid)
        {
            try
            {
                if (gamesGrid == null) return;

                List<Yafes.Models.GameData> filteredGames;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    filteredGames = _allGames;
                }
                else
                {
                    filteredGames = _allGames
                        .Where(game => IsExactMatch(game, searchText))
                        .OrderByDescending(game => CalculateExactMatchPriority(game, searchText))
                        .ThenBy(game => game.Name)
                        .ToList();
                }

                gamesGrid.Children.Clear();

                foreach (var game in filteredGames)
                {
                    try
                    {
                        var gameCard = await _createGameCardFunc(game);
                        if (gameCard != null)
                        {
                            gamesGrid.Children.Add(gameCard);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GameCard creation error: {ex.Message}");
                    }
                }

                gamesGrid.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PerformSearchAsync error: {ex.Message}");
            }
        }

        public List<Yafes.Models.GameData> FilterGames(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return _allGames;
            }

            return _allGames
                .Where(game => IsExactMatch(game, searchText))
                .OrderByDescending(game => CalculateExactMatchPriority(game, searchText))
                .ThenBy(game => game.Name)
                .ToList();
        }

        private bool IsExactMatch(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return false;

            var search = searchText.Trim().ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";
            var imageName = game.ImageName?.ToLowerInvariant() ?? "";

            if (gameName == search) return true;
            if (gameName.Contains(search)) return true;
            if (category == search) return true;
            if (ContainsAllWords(gameName, search)) return true;
            if (imageName.Contains(search)) return true;

            return false;
        }

        private bool ContainsAllWords(string gameName, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || string.IsNullOrWhiteSpace(gameName))
                return false;

            var searchWords = searchText.Split(new char[] { ' ', '-', '_', '.' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in searchWords)
            {
                if (!gameName.Contains(word.ToLowerInvariant()))
                    return false;
            }

            return searchWords.Length > 0;
        }

        private int CalculateExactMatchPriority(Yafes.Models.GameData game, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return 0;

            var search = searchText.Trim().ToLowerInvariant();
            var gameName = game.Name?.ToLowerInvariant() ?? "";
            var category = game.Category?.ToLowerInvariant() ?? "";

            if (gameName == search) return 1000;
            if (gameName.StartsWith(search)) return 800;
            if (ContainsAllWords(gameName, search)) return 600;
            if (category == search) return 400;
            if (gameName.Contains(search)) return 200;

            return 100;
        }

        public void UpdateGamesList(List<Yafes.Models.GameData> newGames)
        {
            _allGames.Clear();
            if (newGames != null)
            {
                _allGames.AddRange(newGames);
            }
        }
    }
}