namespace KnowledgeOps.Application.Chat.Prompting;

public interface IGroundedPromptBuilder
{
    GroundedPromptBuildResult Build(GroundedPromptBuildRequest request);
}
