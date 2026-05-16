using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CVApplicationAPI.Models;
using CVApplicationAPI.Services;

namespace CVApplicationAPI.Controllers
{
    /// <summary>
    /// Контроллер для чат-интерфейса интерактивного CV-бота.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        /// <summary>
        /// Инициализация ChatController.
        /// </summary>
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// POST api/chat
        /// Принять историю сообщений и вернуть потоковую генерацию ответа.
        /// Ответ возвращается как Server-Sent Events stream (text/event-stream), где каждый чанк формируется как: data: {text}\n\n.
        /// </summary>
        [HttpPost]
        public async Task StreamChat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream; charset=utf-8";

            await foreach (var chunk in _chatService.StreamChatAsync(request, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var payload = $"data: {chunk.Replace("\n", "\\n")}\n\n";
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}
