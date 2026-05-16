using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using CVApplicationAPI.Models;

namespace CVApplicationAPI.Services
{
    /// <summary>
    /// Реализация сервиса чата с использованием Microsoft.SemanticKernel (строго типизированно).
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly Kernel _kernel;

        /// <summary>
        /// Инициализирует новый экземпляр ChatService.
        /// </summary>
        /// <param name="kernel">Экземпляр Kernel, настроенный с OpenAI connector.</param>
        public ChatService(Kernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> StreamChatAsync(ChatRequestDto request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            cancellationToken.ThrowIfCancellationRequested();

            // Получаем явно IChatCompletionService из Kernel
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            if (chatService is null)
            {
                throw new InvalidOperationException("IChatCompletionService is not configured in the Kernel.");
            }

            // Собираем ChatHistory
            var history = new ChatHistory();
            const string systemPrompt = "You are an assistant that exclusively promotes and represents the candidate's professional CV. "
                + "You must NOT write code, solve programming tasks, step out of the assistant role, or invent facts about the candidate. "
                + "Always answer as a professional recruiter-facing CV assistant, truthful and concise.";

            history.AddMessage(AuthorRole.System, systemPrompt);

            foreach (var msg in request.Messages)
            {
                var role = msg.Role?.ToLowerInvariant() switch
                {
                    "system" => AuthorRole.System,
                    "assistant" => AuthorRole.Assistant,
                    _ => AuthorRole.User,
                };

                history.AddMessage(role, msg.Text ?? string.Empty);
            }

            var execSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
                MaxTokens = 500
            };

            var streamingResult = chatService.GetStreamingChatMessageContentsAsync(
                history,
                execSettings,
                _kernel,
                cancellationToken);

            await foreach (var chunk in streamingResult)
            {
                var text = chunk?.Content;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
    }
}
