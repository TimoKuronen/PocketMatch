using Cysharp.Threading.Tasks;

/// <summary>Initializes Unity Gaming Services once for every feature that uses them.</summary>
public interface IUgsInitializer
{
    /// <summary>Completes with true once services are ready, or false when initialization failed. A later call retries after a failure.</summary>
    UniTask<bool> InitializeAsync();

    /// <summary>Initializes services, then signs in anonymously once. Completes with false when either step failed; a later call retries.</summary>
    UniTask<bool> EnsureSignedInAsync();
}
