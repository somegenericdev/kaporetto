using System.Collections.Immutable;
using System.Diagnostics;
using Kaporetto.Scheduler;

namespace Scheduler;

public class SchedulerWorker
{
    ImmutableDictionary<string, Process> activeProcesses = new Dictionary<string, Process>().ToImmutableDictionary();
    private Queue<string> nonConflictingThreads = new Queue<string>();

    public void ScheduleProcesses(ImmutableList<string> newThreads) //threadId + boardAlias
    {
        int maxDegree = 10;

        var initSanitizedTuple= sanitizeActiveProcesses(activeProcesses, newThreads);
        activeProcesses = initSanitizedTuple.activeProcesses;
        maxDegree=maxDegree- initSanitizedTuple.removedCount;
        nonConflictingThreads = initSanitizedTuple.nonConflictingThreads;
        
        while (newThreads.Count != 0)
        {
            if (maxDegree != 0 && nonConflictingThreads.Any())
            {
                    var currentElem = nonConflictingThreads.Dequeue(); //dequeue it from the "non-conflicting" queue
                    newThreads = newThreads.Remove(currentElem); //remove it from the main list too
                    activeProcesses = ScheduleProcess(currentElem, activeProcesses);
                    maxDegree--;
            }
            else
            {
                while (true)
                {
                    System.Threading.Thread.Sleep(500);
                    if (activeProcesses.Any(x => x.Value.HasExited == true))
                    {
                        var sanitizedTuple = sanitizeActiveProcesses(activeProcesses, newThreads);
                        activeProcesses = sanitizedTuple.activeProcesses;
                        maxDegree = maxDegree + sanitizedTuple.removedCount;
                        nonConflictingThreads = sanitizedTuple.nonConflictingThreads;
                        break;
                    }
                }
            }
        }
    }

    private ImmutableDictionary<string, Process> ScheduleProcess(string threadNumber,
        ImmutableDictionary<string, Process> activeProcesses)
    {
        var proc = Process.Start(
            Globals.scraperPath,
            threadNumber.ToString());
        var newActiveProcesses = activeProcesses.Add(threadNumber, proc);
        return newActiveProcesses;
    }



    private SanitizedActiveProcesssesTuple sanitizeActiveProcesses(
        ImmutableDictionary<string, Process> activeProcesses,
        ImmutableList<string> newThreads) //removes processes that have exited from the dictionary
    {
        var exitedProcesses = activeProcesses.Where(x => x.Value.HasExited == true).ToImmutableDictionary();
        var numberOfExitedProcesses = exitedProcesses.Count();
        var sanitizedActiveProcesses = activeProcesses.Where(act => !exitedProcesses.Select(ex => ex.Key)
                .Contains(act.Key))
            .ToImmutableDictionary();
        return new SanitizedActiveProcesssesTuple(sanitizedActiveProcesses, numberOfExitedProcesses,
            new Queue<string>(newThreads.Where(n => !sanitizedActiveProcesses.Keys.Contains(n))));
    }
}

record SanitizedActiveProcesssesTuple(ImmutableDictionary<string, Process> activeProcesses, int removedCount,
    Queue<string> nonConflictingThreads);