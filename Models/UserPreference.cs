using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp8.Models
{

    [Serializable]
    internal class UserPreference
    {
        public long ChatId { get; set; }
        public Dictionary<string, int> CategoriesGrades{ get; set; }

        public List<string> AlreadySuggested { get; set; }//список id фильмов которые пользователь уже оценивал (не будем предлагать повторно)

        public UserPreference(long chatID)
        {
            ChatId = chatID;
            CategoriesGrades = new Dictionary<string, int>();
            AlreadySuggested = new List<string>();
        }

    }
}
