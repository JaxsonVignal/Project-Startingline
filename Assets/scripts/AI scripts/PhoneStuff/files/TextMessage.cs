using UnityEngine;
using System;

[System.Serializable]
public class TextMessage
{
    public string senderName;
    public string messageContent;
    public DateTime timestamp;
    public bool isPlayerMessage;
    public MessageType messageType;
    
    public enum MessageType
    {
        WeaponRequest,
        PriceOffer,
        Acceptance,
        General
    }
    
    public TextMessage(string sender, string content, bool fromPlayer, MessageType type = MessageType.General)
    {
        senderName = sender;
        messageContent = content;
        timestamp = DateTime.Now;
        isPlayerMessage = fromPlayer;
        messageType = type;
    }
}
