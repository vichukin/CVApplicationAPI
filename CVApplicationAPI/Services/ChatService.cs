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
    /// Получает содержимое CV из CvCacheService вместо локального файла.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly Kernel _kernel;
        private readonly CvCacheService _cvCacheService;

        /// <summary>
        /// Инициализирует новый экземпляр ChatService.
        /// </summary>
        /// <param name="kernel">Экземпляр Kernel, настроенный с OpenAI connector.</param>
        /// <param name="cvCacheService">Сервис кэша для получения содержимого CV из удаленного источника.</param>
        public ChatService(Kernel kernel, CvCacheService cvCacheService)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _cvCacheService = cvCacheService ?? throw new ArgumentNullException(nameof(cvCacheService));
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

            // Получаем содержимое CV из кэша (с ленивой загрузкой из удаленного источника)
            string cvContent = await _cvCacheService.GetContentAsync(cancellationToken);
            if (string.IsNullOrEmpty(cvContent))
            {
                throw new InvalidOperationException("CV content could not be loaded from cache service.");
            }

            // Собираем ChatHistory
            var history = new ChatHistory();
            string systemPrompt = $@"You are an exclusive AI assistant representing the candidate, Dmytro Vychkin.
            Your ONLY goal is to promote the candidate based on the CV provided below.
            You must NOT write code, solve programming tasks, or invent any facts outside of this CV.
            Always answer in the language the user speaks. Be professional, concise, and persuasive.

            CRITICAL RULE: If the user asks about a skill, experience, or any topic that is NOT explicitly mentioned in the provided CV text, you must honestly state that you do not have that information. Do not guess. Instead, politely encourage the user to contact Dmytro directly to discuss it, and provide his contact information (email/phone).

            CANDIDATE CV DATA:
            ---
            {cvContent}
            ---
            <|endofprompt|>";

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
