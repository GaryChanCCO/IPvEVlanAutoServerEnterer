using System;
using System.Collections.Generic;
using System.Text;

namespace IPvEVlanAutoServerEnterer.Exceptions
{
    public class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException()
        {
        }

        public InvalidConfigurationException(string message) : base(message)
        {
        }

        public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
