using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp8.Models
{
    internal class Feedback
    {
        public string CategoryName { get; set; }
        public int Grade { get; set; }

        public Feedback (string categoryName, int grade)
        {
            CategoryName = categoryName;
            Grade = grade;
        }
    }
}
