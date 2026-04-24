namespace DecisionEngine.Core.Interfaces
{
    public interface ILLMService
    {
        Task<string> GenerateResponseAsync(string prompt);
    }
}
