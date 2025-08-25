namespace _20strike;

class TaskHandler
{
    readonly List<Task> Tasks;

    public bool Empty { get; set; }

    public TaskHandler()
    {
        Tasks = [];
        Empty = true;
    }

    public void AddTask(Task task)
    {
        Tasks.Add(task);
        Empty = false;
    }

    public void AddAction(Action action)
    {
        AddTask(Task.Run(action));
    }

    public void CheckCompletion()
    {
        for (int i = 0; i < Tasks.Count; i++)
        {
            if (Tasks[i].IsCompleted) { Tasks.RemoveAt(i); i -= 1; }
        }
        if (Tasks.Count == 0) Empty = true;
    }

    public void Delete()
    {

    }

    public bool AllReady()
    {
        CheckCompletion();
        return Empty;
    }

    public Task<bool> WaitAll()
    {
        while (!AllReady()) Thread.Sleep(100);
        return Task.FromResult(true);
    }
}
