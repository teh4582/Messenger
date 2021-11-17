using System;

namespace Messenger
{
    internal class PublicKey
    {
        public String email { get; set; }
        
        public String key { get; set; }
    }

    internal class PrivateKey
    {
        public String[] emails { get; set; }
        
        public String key { get; set; }
    }
}