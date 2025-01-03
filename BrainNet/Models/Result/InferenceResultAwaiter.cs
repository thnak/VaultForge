
namespace BrainNet.Models.Result;

public class InferenceResultAwaiter<T>(T result)
{
    private T _result = result;
    private bool _isCompleted;
    private TaskCompletionSource<T> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<T> WaitAsync()
    {
        // Return a completed ValueTask if the result is ready
        if (_isCompleted)
        {
            return new ValueTask<T>(_result);
        }

        // Otherwise, wrap the TaskCompletionSource in a ValueTask
        return new ValueTask<T>(_tcs.Task);
    }

    public void SetResult(T result)
    {
        _result = result;
        _isCompleted = true;

        // Complete the TaskCompletionSource
        _tcs.SetResult(result);
    }
}
