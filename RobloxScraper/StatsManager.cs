﻿using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobloxScraper
{
    public class StatsManager
    {
        DateTime startTime;
        Timer timer;
        Timer snapshot;

        List<string> DownloadTasks { get; set; }
        List<string> ProcessTasks { get; set; }

        long totalDownloaded = 0;
        long totalProcessed = 0;
        long totalDownloadTime = 0;
        long totalProcessTime = 0;

        Stat downloadSnapshot;
        Stat processedSnapshot;


        public StatsManager()
        {
            startTime = DateTime.Now;
            DownloadTasks = new List<string>(TaskRunner.DownloadTasks.Keys);
            ProcessTasks = new List<string>(TaskRunner.ProcessingTasks.Keys);
            downloadSnapshot = new Stat(totalDownloaded, totalProcessed);
            processedSnapshot = new Stat(totalProcessed, totalProcessTime);

            timer = new Timer(UpdateConsole, null, 250, Timeout.Infinite);
            snapshot = new Timer(UpdateSnapshot, null, 5000, Timeout.Infinite);
        }

        private void UpdateSnapshot(object obj)
        {
            downloadSnapshot = new Stat(totalDownloaded, totalProcessed);
            processedSnapshot = new Stat(totalProcessed, totalProcessTime);
            snapshot.Change(5000, Timeout.Infinite);
        }

        private void UpdateConsole(object obj)
        {
            totalDownloaded = 0;
            totalProcessed = 0;
            totalDownloadTime = 0;
            totalProcessTime = 0;

            string statsTable = GetStatsTable();
            string downloadTable = GetDownloadTable();
            string processedTable = GetProcessedTable();

            Console.SetCursorPosition(0, 0);
            Console.Write(statsTable);
            Console.WriteLine();
            Console.Write(downloadTable);
            Console.WriteLine();
            Console.Write(processedTable);

            timer.Change(250, Timeout.Infinite);
        }

        private string GetStatsTable()
        {
            var table = new ConsoleTable("Time Elapsed", "Latest Thread", "Thread Queue", "Processing Queue", "Database Queue");
            TimeSpan elapsed = DateTime.Now.Subtract(startTime);
            int latest;
            TaskRunner.Queue.TryPeek(out latest);
            table.AddRow(elapsed.ToString(@"hh\:mm\:ss"), latest, TaskRunner.Queue.Count, TaskRunner.UnparsedThreads.Count, TaskRunner.ForumThreads.Count);
            return table.ToStringAlternative();
        }

        private string GetDownloadTable()
        {
            var table = new ConsoleTable("Worker", "Status", "Downloaded", "Avg Time(ms)");
            AddDownloadTaskRows(table);
            AddDownloadTotalsRow(table);
            return table.ToMarkDownString();
        }

        private string GetProcessedTable()
        {
            var table = new ConsoleTable("Worker", "Status", "Processed", "Avg Time(ms)");
            AddProcessedTaskRows(table);
            AddProcessedTotalsRow(table);
            AddEmptyTotalsRow(table);
            return table.ToMarkDownString();
        }

        private void AddDownloadTotalsRow(ConsoleTable table)
        {
            float time = float.NaN;
            if(totalDownloaded > 0 && DownloadTasks.Count > 0)
            {
                time = totalDownloadTime / DownloadTasks.Count / totalDownloaded;
            }

            table.AddRow("---", "Total", totalDownloaded, time);
        }

        private void AddProcessedTotalsRow(ConsoleTable table)
        {
            float time = float.NaN;
            if (totalProcessed > 0 && ProcessTasks.Count > 0)
            {
                time = totalProcessTime / ProcessTasks.Count / totalProcessed;
            }

            table.AddRow("---", "Total", totalProcessed, time);
        }

        private void AddEmptyTotalsRow(ConsoleTable table)
        {
            table.AddRow("---", "Total Empty", TaskRunner.emptyThreads, "N/A");
        }

        private void AddDownloadTaskRows(ConsoleTable table)
        {
            for (int i = 0; i < DownloadTasks.Count; i++)
            {
                long count = TaskRunner.downloadedStats[DownloadTasks[i]].Count;
                long time = TaskRunner.downloadedStats[DownloadTasks[i]].TimeTaken;
                float avg = TaskRunner.downloadedStats[DownloadTasks[i]].Average;


                string status = GetWorkerStatus(TaskRunner.DownloadTasks[DownloadTasks[i]]);

                table.AddRow($"#{i}", status, count, avg);

                totalDownloaded += count;
                totalDownloadTime += time;
            }
        }

        private void AddProcessedTaskRows(ConsoleTable table)
        {
            for (int i = 0; i < ProcessTasks.Count; i++)
            {
                long count = TaskRunner.processedStats[ProcessTasks[i]].Count;
                long time = TaskRunner.processedStats[ProcessTasks[i]].TimeTaken;
                float avg = TaskRunner.processedStats[ProcessTasks[i]].Average;


                string status = GetWorkerStatus(TaskRunner.ProcessingTasks[ProcessTasks[i]]);

                table.AddRow($"#{i}", status, count, avg);

                totalProcessed += count;
                totalProcessTime += time;
            }
        }

        private string GetWorkerStatus(Task task)
        {
            return task.Status.ToString();
            if (task.IsCompleted)
            {
                return "Completed";
            }
            else
            {
                return "Running";
            }
        }

        private float GetSnapshotAvg(string type, long count, long time)
        {
            if(type == "download")
            {
                return new Stat(count - downloadSnapshot.Count, time - downloadSnapshot.TimeTaken).Average;
            }
            else
            {
                return new Stat(count - processedSnapshot.Count, time - processedSnapshot.TimeTaken).Average;
            }
        }

    }
}