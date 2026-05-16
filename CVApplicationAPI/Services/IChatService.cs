using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CVApplicationAPI.Models;

namespace CVApplicationAPI.Services
{
    /// <summary>
    /// Сервис чата, возвращающий потоковую генерацию ответов.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Выполнить чат-запрос и вернуть поток текстовых фрагментов ответа.
        /// </summary>
        /// <param name="request">Запрос чата с историей сообщений.</param>
        /// <returns>Асинхронный перечисляемый поток строк (фрагментов ответа).</returns>
        IAsyncEnumerable<string> StreamChatAsync(ChatRequestDto request, CancellationToken cancellationToken);
    }
}
