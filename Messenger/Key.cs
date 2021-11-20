using System;

namespace Messenger
{
    /// <summary>
    /// JSON PublicKey object. Holds a string email and a string base64 encoded key.
    /// </summary>
    internal class PublicKey
    {
        public String email { get; set; }
        
        public String key { get; set; }
    }

    /// <summary>
    /// JSON PrivateKey object. Holds a string list of emails and a string base64 encoded key. 
    /// </summary>
    internal class PrivateKey
    {
        public String[] emails { get; set; }
        
        public String key { get; set; }
    }
    
    /// <summary>
    /// JSON Message object. Holds a string email and string base64 encoded string message.
    /// </summary>
    internal class Message
    {
        public String email { get; set; }
        
        public String content { get; set; }
    }
}