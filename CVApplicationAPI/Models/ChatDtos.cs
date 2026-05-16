using System.Collections.Generic;

namespace CVApplicationAPI.Models
{
    /// <summary>
    /// DTO сообщения в чате.
    /// </summary>
    public sealed class ChatMessageDto
    {
        /// <summary>
        /// Роль автора сообщения ("system", "user", "assistant").
        /// </summary>
        public string Role { get; init; } = string.Empty;

        /// <summary>
        /// Текст сообщения.
        /// </summary>
        public string Text { get; init; } = string.Empty;
    }

    /// <summary>
    /// DTO запроса для чата, содержит историю сообщений клиента.
    /// </summary>
    public sealed class ChatRequestDto
    {
        /// <summary>
        /// Список сообщений, где порядок соответствует хронологии (старые->новые).
        /// </summary>
        public IList<ChatMessageDto> Messages { get; init; } = new List<ChatMessageDto>();
    }
}
