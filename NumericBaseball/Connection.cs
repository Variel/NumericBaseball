using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NumericBaseball
{
    public class Connection
    {
        public string Id { get; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }

        public Connection(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}