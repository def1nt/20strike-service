namespace _20strike;

sealed class TaskHandler
{
    readonly List<Task> _tasks;
    readonly object _lock = new object();

    public bool Empty => _tasks.Count == 0;

    public TaskHandler()
    {
        _tasks = [];
    }

    public void AddTask(Task task)
    {
        lock (_lock)
        {
            RemoveCompleted();
            _tasks.Add(task);
        }
    }

    public void AddAction(Action action)
    {
        AddTask(Task.Run(action));
    }

    public void RemoveCompleted()
    {
        lock (_lock)
        {
            _tasks.RemoveAll(task => task.IsCompleted);
        }
    }

    public bool AllReady()
    {
        RemoveCompleted();
        return Empty;
    }

    public async Task WaitAllAsync()
    {
        Task[] tasksToWait;
        lock (_lock)
        {
            tasksToWait = _tasks.ToArray();
        }

        if (tasksToWait.Length > 0)
        {
            await Task.WhenAll(tasksToWait);
        }
    }
}
