using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using SignalR.Hubs;

namespace SignalRMonkeys
{
    [HubName("monkeys")]
    public class MonkeyHub : Hub
    {
        private static Task currentTask = Task.FromResult(0);
        private static CancellationTokenSource cancellation = new CancellationTokenSource();

        public void StartTyping(string targetText, bool parallel)
        {
            var settings = new GeneticAlgorithmSettings { PopulationSize = 200 };
            var token = cancellation.Token;

            
            currentTask = currentTask.ContinueWith((previous) =>
            {
                // Create the new genetic algorithm
                var ga = new TextMatchGeneticAlgorithm(parallel, targetText, settings);
                TextMatchGenome? bestGenome = null;
                DateTime startedAt = DateTime.Now;

                Hub.GetClients<MonkeyHub>().started(targetText);

                // Iterate until a solution is found or until cancellation is requested
                for (int generation = 1; ; generation++)
                {
                    if (token.IsCancellationRequested)
                    {
                        Hub.GetClients<MonkeyHub>().cancelled();
                        break;
                    }

                    // Move to the next generation
                    ga.MoveNext();

                    // If we've found the best solution thus far, update the UI
                    if (bestGenome == null ||
                        ga.CurrentBest.Fitness < bestGenome.Value.Fitness)
                    {
                        bestGenome = ga.CurrentBest;
                        
                        int generationsPerSecond = generation / Math.Max(1, (int)((DateTime.Now - startedAt).TotalSeconds));
                        Hub.GetClients<MonkeyHub>().updateProgress(bestGenome.Value.Text, generation, generationsPerSecond);

                        if (bestGenome.Value.Fitness == 0)
                        {
                            Hub.GetClients<MonkeyHub>().complete();
                            break;
                        }
                    }
                }                
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void StopTyping()
        {
            cancellation.Cancel();
            cancellation = new CancellationTokenSource();
            currentTask = Task.FromResult(0);
        }
        
    }
}