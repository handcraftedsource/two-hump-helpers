using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Text;

namespace TwoHumpHelpers.Skills;

/// <summary>
/// <para>Semantic skill that enables text generation.</para>
/// </summary>
/// <example>
/// <code>
/// var context = kernel.CreateNewContext();
/// 
/// context["input"] = "your context information";
/// context["task"] = "the task to perform";
/// 
/// var skill = new TextGenerationSkill(kernel);
/// kernel.ImportSkill(skill);
/// 
/// var result = await skill.SimpleTextGenerationTaskAsync(input, context);
/// </code>
/// </example>
public class TextGenerationSkill
{
    private readonly ISKFunction _simpleTextGenerationTaskFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextGenerationSkill"/> class.
    /// </summary>
    /// <param name="kernel">Kernel instance</param>
    /// <param name="temperature">Temperature parameter passed to LLM. A value between 0 and 1.0. A higher temperature leads to more varied and creative outputs, while a lower temperature results in more predictable responses.</param>
    public TextGenerationSkill(IKernel kernel, double temperature = 0.7)
    {
        _simpleTextGenerationTaskFunction = kernel.CreateSemanticFunction(
            Prompts.SimpleTextGenerationTask,
            skillName: nameof(TextGenerationSkill),
            description: "Generates text based on a task and a given context.",
            maxTokens: 1000,
            temperature: temperature,
            topP: 0.5);
    }

    /// <summary>
    /// Generates text based on a task and a given context.
    /// </summary>
    /// <param name="input">A context for the task to perform.</param>
    /// <param name="context">The SKContext for function execution.</param>
    [SKFunction("Generates text based on a task and a given context.")]
    [SKFunctionName("SimpleTextGenerationTask")]
    [SKFunctionInput(Description = "The context to perform the task with.")]
    [SKFunctionContextParameter(Name = "task", Description = "The task that describes what text should be generated.")]
    public Task<SKContext> SimpleTextGenerationTaskAsync(string input, SKContext context)
    {
        return _simpleTextGenerationTaskFunction.InvokeAsync(input, context);
    }

    private static class Prompts
    {
        public const string SimpleTextGenerationTask = @"This is your context:
---
{{$input}}
---

This is your task:
{{$task}}

Answer:
";
    }
}