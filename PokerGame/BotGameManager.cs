using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using PokerBot.DatabaseManagment;
using System.Windows.Forms;
using PokerBot.Definitions;

namespace PokerBot.BotGame
{
    public class pokerGameThreadManagment
    {
        BotvsBot game;
        Thread gameThread;

        public pokerGameThreadManagment(pokerGameType gameType, databaseCacheClient clientCache, bool endOnFirstDeath, bool useAiServer, string aiServerIP, string[] playerNames, decimal startingStack, int maxHandsToPlay, int actionPause, List<Control> neuralTrainingOutputFields, List<Control> neuralPlayerActionLog)
        {

            //We may or may not want to use an external AI servers
            if (useAiServer && gameType == pokerGameType.BotVsBot)
                game = new BotvsBot(gameType, clientCache, playerNames, startingStack, endOnFirstDeath, aiServerIP, maxHandsToPlay, actionPause);
            else if (!useAiServer && gameType == pokerGameType.BotVsBot)
                game = new BotvsBot(gameType, clientCache, playerNames, startingStack, endOnFirstDeath, "", maxHandsToPlay, actionPause);
            else if (gameType == pokerGameType.NeuralManualBotTraining)
                game = new BotvsBot(gameType, clientCache, playerNames, startingStack, endOnFirstDeath, "", maxHandsToPlay, actionPause, neuralTrainingOutputFields, neuralPlayerActionLog);

            gameThread = new Thread(game.playGame);
            gameThread.Start();
        }

        public void endGameThread()
        {
            try
            {
                game.EndGame = true;
            }
            catch (Exception)
            {

            }
        }

        public void waitForGameThreadToClose()
        {
            try
            {
                gameThread.Join();
            }
            catch (Exception)
            {

            }
        }

        public bool EndGame
        {
            get { return game.EndGame; }
        }

        public void SetManualDecision(Play decision, byte raiseType)
        {
            game.SetManualDecision(decision, raiseType);
        }
    }
}
