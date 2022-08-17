using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp8.Models
{
    internal class Movie
    {
        public string imdb_id { get; set; }
        public string title { get; set; }
        public string rating { get; set; }

        public int year { get; set; }
        public string image_url { get; set; }
    }
}
