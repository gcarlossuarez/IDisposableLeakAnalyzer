using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource
{
    public enum Language
    {
        Spanish,
        English
    }

    public class MultilangMessage
    {
        public string Spanish { get; }
        public string English { get; }

        public MultilangMessage(string spanish, string english)
        {
            Spanish = spanish;
            English = english;
        }
    }


}
