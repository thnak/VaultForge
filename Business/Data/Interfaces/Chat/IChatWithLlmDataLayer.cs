using BusinessModels.People;

namespace Business.Data.Interfaces.Chat;

public interface IChatWithLlmDataLayer : IMongoDataInitializer, IDataLayerRepository<ChatWithChatBotMessageModel>
{
    
}