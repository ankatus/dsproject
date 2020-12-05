using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal class View
    {
        private const int VISIBLE_CARDS = 5;
        private const int HAND_OFFSET_LEFT = 30;
        private const int PLAYERNAME_MAX_VISIBLE_LENGTH = 20;
        private const string PLAYER_LIST_SEPARATOR = " | ";
        private const string PLAYER_LIST_TURN_INDICATOR = "(playing)";

        private readonly Display _display;

        public int CurrentTurnPlayerId { get; set; }
        public string LocalPlayerName { get; set; }
        public int LocalPlayerId { get; set; }
        public List<PlayerInfo> Players { get; set; }
        public List<UnoCard> Hand { get; set; }
        public UnoCard TopCard { get; set; }
        public int VisibleIndex { get; private set; }
        public string Message { get; set; }
        public ConsoleColor MessageColor { get; set; }


        public View(Display display)
        {
            _display = display;
            Hand = new List<UnoCard>();
            Players = new List<PlayerInfo>();
        }

        public void IncreaseVisibleIndex()
        {
            var max = Hand.Count / 5 - (Hand.Count % 5 == 0 ? 1 : 0);
            if (VisibleIndex == max) return;

            VisibleIndex++;
        }

        public void DecreaseVisibleIndex()
        {
            if (VisibleIndex == 0) return;

            VisibleIndex--;
        }

        public void ResetVisibleIndex() => VisibleIndex = 0;

        public void Draw()
        {
            // Draw top card
            if (TopCard is not null) _display.InsertArray(TopCard.GetGraphic(), 0, 60, Utils.CardToConsoleColor(TopCard.Color));

            // Draw visible hand
            var cardsAfterIndex = Hand.Count - (VISIBLE_CARDS * VisibleIndex);
            for (var i = 0; i < (cardsAfterIndex < VISIBLE_CARDS ? cardsAfterIndex : VISIBLE_CARDS); i++)
            {
                var card = Hand[i + VisibleIndex * VISIBLE_CARDS];

                // Draw card
                _display.InsertArray(card.GetGraphic(), 30, HAND_OFFSET_LEFT + i * CardGraphics.CARDGRAPHIC_WIDTH + i, Utils.CardToConsoleColor(card.Color));

                // Draw selection number
                _display.WriteString("" + (i + 1), 30, HAND_OFFSET_LEFT + i * CardGraphics.CARDGRAPHIC_WIDTH + i + 5);
            }

            // Draw message
            _display.WriteString(Message, 45, 0, MessageColor);

            // Draw players
            DrawPlayers();

            // Draw local player name
            var drawnName = LocalPlayerName.Length > PLAYERNAME_MAX_VISIBLE_LENGTH
                ? LocalPlayerName.Substring(0, PLAYERNAME_MAX_VISIBLE_LENGTH)
                : LocalPlayerName;
            _display.WriteString("Playing as:", 0, 0);
            _display.WriteString(drawnName, 1, 0, ConsoleColor.DarkCyan);
        }

        private void DrawPlayers()
        {
            // Collect player strings
            List<(int id, string content)> players = Players.Select(
                    (player, i) => (player.PlayerID, i +
                                                     1 +
                                                     ". " +
                                                     player.PlayerName +
                                                     " (" +
                                                     player.Hand.Count +
                                                     " cards)" +
                                                     (player.PlayerID == CurrentTurnPlayerId
                                                         ? " " + PLAYER_LIST_TURN_INDICATOR + " "
                                                         : ""))).ToList();

            // Allocate player strings to rows
            var playerRows = new List<List<(int id, string content)>> { new() };
            var rowIndex = 0;
            var playerIndex = 0;
            while (true)
            {
                if (playerIndex == players.Count) break;

                var nextPlayer = players[^(playerIndex + 1)];
                // The length calculation is not 100% correct, since PLAYER_LIST_SEPARATOR does not appear at the end of the last player string, but it's good enough
                if (playerRows[rowIndex].Sum(s => s.content.Length + PLAYER_LIST_SEPARATOR.Length) + nextPlayer.content.Length <= Display.DisplayWidth)
                {
                    // String fits on this row
                    playerRows[^(rowIndex + 1)].Insert(0, nextPlayer);
                    playerIndex++;
                }
                else
                {
                    // Start new row
                    playerRows.Insert(0, new List<(int id, string content)>());
                    rowIndex++;
                }
            }


            for (var i = playerRows.Count - 1; i >= 0; i--)
            {
                var displayRowIndex = Display.DisplayHeight - 1 - playerRows.Count + i;
                var playerRow = playerRows[i];
                var columnIndex = 0;

                for (var j = 0; j < playerRows[i].Count; j++)
                {
                    _display.WriteString(playerRow[j].content, displayRowIndex, columnIndex,
                        playerRow[j].id == LocalPlayerId ? ConsoleColor.Cyan : ConsoleColor.White);

                    columnIndex += playerRow[j].content.Length;

                    if (j < playerRows[i].Count - 1)
                    {
                        _display.WriteString(PLAYER_LIST_SEPARATOR, displayRowIndex, columnIndex);
                    }

                    columnIndex += PLAYER_LIST_SEPARATOR.Length;
                }
            }
        }
    }
}
