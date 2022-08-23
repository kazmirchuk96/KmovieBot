using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp8.Models
{
    [Serializable]
    internal class Genre
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("genre")]
        public string Name { get; set; }
    }
}
