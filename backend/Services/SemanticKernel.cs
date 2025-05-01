using Microsoft.SemanticKernel;

namespace KnowledgeBank.Services;


public interface ISemanticKernel
{
    Kernel Kernel { get; }
}



public class SemanticKernel : ISemanticKernel
{
    private readonly Kernel _kernel;

    public Kernel Kernel => _kernel;

    // public SemanticKernel(IKernelConfig kernelConfig)
    // {
    //     _kernel = new KernelBuilder()
    //         .WithMemory(kernelConfig.Memory)
    //         .WithAIService(kernelConfig.AIService)
    //         .WithSkillCollection(kernelConfig.SkillCollection)
    //         .Build();
    // }

}