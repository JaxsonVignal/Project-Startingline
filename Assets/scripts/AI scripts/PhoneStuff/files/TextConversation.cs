using System.Collections.Generic;

[System.Serializable]
public class TextConversation
{
    public string npcName;
    public List<TextMessage> messages;
    public bool hasUnreadMessages;
    
    public TextConversation(string name)
    {
        npcName = name;
        messages = new List<TextMessage>();
        hasUnreadMessages = false;
    }
    
    public void AddMessage(TextMessage message)
    {
        messages.Add(message);
        if (!message.isPlayerMessage)
        {
            hasUnreadMessages = true;
        }
    }
    
    public void MarkAsRead()
    {
        hasUnreadMessages = false;
    }
}
