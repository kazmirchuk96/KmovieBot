using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ConsoleApp8.Models
{
    internal class Movie
    {
        public string imdb_id { get; set; }
        public string? title { get; set; }
        public string? rating { get; set; }
        public int? year { get; set; }
        public string? banner { get; set; }

        [JsonProperty("gen")]
        public List<Genre> Genres {get;set;}

        public string GenListToString ()
        {
            if (Genres != null)
            {
                string str = string.Empty;
                foreach (var item in Genres)
                {
                    str += item.Name + ", ";
                }
                return str.Remove(str.Length - 2);
            }
            return "no information";
        }
    }
}
