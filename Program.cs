using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using ConsoleApp8.Models;
using RestSharp.Serialization.Json;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Utf8Json.Formatters;
using File = System.IO.File;


namespace ConsoleApp8
{
    internal class Program
    {
        static TelegramBotClient bot = new("5413864028:AAFgY8RbjQtiKSBhDcacFmTXc4tOlwFO1JQ");
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            Movie randomMovie = new Movie();
            long chatId;
            string genre = String.Empty; //название первого жанра фильма (будем передавать его через нажатие кнопки с оценкой)
            List<UserPreference> listUsersPreference = new List<UserPreference>(); //список предпочтений всех пользователей
            List<string> top3CategoryList = new List<string>();//список ТОП 3 категорий конкретного пользователя, категория попадает в список, если у неё полож. оценка
            string fileName = @"userpreferenses.json"; //файл в который записываем предпочтения пользователя

            bool top3CategoryExists = false;
            bool checkPreference = false;//?
            bool movieGenreEquelPrefereble = false;//?

            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                chatId = message.Chat.Id;


                string
                    data = ReadingDataFromFile(fileName); //получаем информацию из файла c предпочтениями пользователя

                if (message.Text != null && message.Text.ToLower() == "/start") //Button "Start" was pressed
                {
                    //достаём предпочтения всех пользователей из чата
                    if (data != String.Empty)
                    {
                        listUsersPreference = JsonConvert.DeserializeObject<List<UserPreference>>(data);
                        var userPreference =
                            listUsersPreference.FirstOrDefault(x => x.ChatId == chatId); //ищем текущего пользователя

                        //проверка есть ли в списке предопчтений минимум 3 категории с положительны значением
                        if (userPreference != null &&
                            userPreference.CategoriesGrades.Where(x => x.Value > 0).ToList().Count >= 3)
                        {
                            //сортируем категории по спаданию оценок
                            var top3CategoryDict = userPreference.CategoriesGrades.OrderByDescending(pair => pair.Value)
                                .Take(3);

                            //Записываем топ категории в List
                            foreach (var variable in top3CategoryDict)
                            {
                                top3CategoryList.Add(variable.Key);
                            }

                            top3CategoryExists = true; //топ 3 категории найдены
                        }
                        else
                        {
                            top3CategoryExists =
                                false; //не получилось найти топ 3 категории пользователя (м-ало оценок)
                        }
                    }

                    do
                    {
                        //Рандомим страницу на которой будем искать фильм от 1 до 33 (на 33-й странице рейтинг 7.5)
                        Random random = new Random();
                        int page = random.Next(1, 33);

                        //Делаем запрос по ip берем 50 результатов из страницы, которую рандомили выше. Максимум из страницы можно достать 50 фильмов
                        var client =
                            new RestClient(
                                $"https://moviesminidatabase.p.rapidapi.com/movie/order/byRating/?page_size=50&page={page}");
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("X-RapidAPI-Key", "00269e84d5msh3a6a436ff9522e8p1f5489jsn93854c5bbb0e");
                        request.AddHeader("X-RapidAPI-Host", "moviesminidatabase.p.rapidapi.com");
                        IRestResponse response = client.Execute(request);

                        //десерилизация полученного результата
                        var deserialize = new JsonDeserializer();
                        var output = deserialize.Deserialize<Dictionary<string, string>>(response);
                        var list = JsonConvert.DeserializeObject<List<Movie>>(output["results"]);

                        //из листа из 50-ти флиьмов рандомим 1
                        randomMovie = list[random.Next(0, list.Count)];

                        //достаем инфу про фильм
                        client = new RestClient(
                            $"https://moviesminidatabase.p.rapidapi.com/movie/id/{randomMovie.imdb_id}/");
                        request = new RestRequest(Method.GET);
                        request.AddHeader("X-RapidAPI-Key", "24a71e0ea3msh55c8e1302d48751p19be5bjsn4deef94609db");
                        request.AddHeader("X-RapidAPI-Host", "moviesminidatabase.p.rapidapi.com");
                        response = client.Execute(request);

                        //десерилизация полученного результата
                        output = deserialize.Deserialize<Dictionary<string, string>>(response);
                        randomMovie = JsonConvert.DeserializeObject<Movie>(output["results"]);

                    } while (randomMovie.year < 2012 ||
                             (top3CategoryExists &&
                              !top3CategoryList.Contains(randomMovie.Genres[0]
                                  .Name))); //если есть список из 3 предпочитаемых категорий, но текущий фильм не совпадает с этими категориями

                    InlineKeyboardButton[][] array = new InlineKeyboardButton[1][];
                    array[0] = new[]
                    {
                        InlineKeyboardButton.WithUrl("Рейтинг kinopoisk",
                            $"https://www.google.com/search?q={randomMovie.title}+{randomMovie.year}+%D0%BA%D0%B8%D0%BD%D0%BE%D0%BF%D0%BE%D0%B8%D1%81%D0%BA&sxsrf=ALiCzsYjrUvCyZzCE25i8w-qj8dt4q2x8Q%3A1666198515496&ei=8ytQY8neHY-nrgTOlq8I&oq={randomMovie.title}+{randomMovie.year}+%D0%BA%D0%B8%D0%BD%D0%BE&gs_lcp=Cgdnd3Mtd2l6EAMYADIFCCEQoAEyBQghEKABOgoIABBHENYEELADOgQIIRAVSgQIQRgASgQIRhgAULEGWKQNYKkcaAFwAXgAgAHBAYgB2QSSAQMwLjSYAQCgAQHIAQPAAQE&sclient=gws-wiz"),
                        InlineKeyboardButton.WithUrl("Переглянути",
                            $"https://www.google.com/search?q={randomMovie.title}+{randomMovie.year}+%D1%81%D0%BC%D0%BE%D1%82%D1%80%D0%B5%D1%82%D1%8C+%D0%BE%D0%BD%D0%BB%D0%B0%D0%B9%D0%BD&sxsrf=ALiCzsayODZ0C_VwPTF9TBwSuUFkdbSUdg%3A1660508066429&ei=olf5YqziGYbOrgTp2KCACQ&ved=0ahUKEwisu8jLksf5AhUGp4sKHWksCJAQ4dUDCA4&uact=5&oq={randomMovie.title}+{randomMovie.year}+%D1%81%D0%BC%D0%BE%D1%82%D1%80%D0%B5%D1%82%D1%8C+%D0%BE%D0%BD%D0%BB%D0%B0%D0%B9%D0%BD&gs_lcp=Cgdnd3Mtd2l6EAM6BwgjELADECc6BwgAEEcQsAM6BAgjECc6BggjECcQEzoFCAAQgAQ6CggAEMsBEEYQ_wE6BQguEMsBOgUIABDLAToLCC4QxwEQrwEQywE6CwguEMcBENEDEMsBOgYIABAeEBY6BQguEIAESgQIQRgASgQIRhgAUD1Y24oEYOWMBGgCcAF4AIABjAGIAdUQkgEEMjIuMpgBAKABAaABAsgBCsABAQ&sclient=gws-wiz")
                    };

                    if (GetBannerSize(randomMovie) < 5)
                    {

                        await botClient.SendPhotoAsync(message.Chat, randomMovie.banner,
                            $"Назва фільму: {randomMovie.title}\nЖанр: {randomMovie.GenListToString()} \nРік виходу: {randomMovie.year}\nРейтинг IMDb: {randomMovie.rating}\n",
                            replyMarkup: new InlineKeyboardMarkup(array));
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat,
                            $"Назва фільму: {randomMovie.title}\nЖанр: {randomMovie.GenListToString()} \nРік виходу: {randomMovie.year}\nРейтинг IMDb: {randomMovie.rating}\n",
                            replyMarkup: new InlineKeyboardMarkup(array));
                    }


                    /*Берём первый жанр фильма и дальше передаем его через нажатие кнопки с оценкой,
                     по первому жанру фильма будем ставить оценку*/
                    genre = randomMovie.Genres[0].Name;
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Погано❌", $"-1,{chatId},{genre}"),
                            InlineKeyboardButton.WithCallbackData("Добре✅", $"1,{chatId},{genre}")
                        },
                    });
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Для покращення видачі, будь ласка, оціни рекомендацію🍿", cancellationToken: cancellationToken,
                        replyMarkup: keyboard);
                }
            }

            if (update.CallbackQuery != null) //если была нажата одна из кнопок с отзывом
                {
                    string[] callBackDataArray = update.CallbackQuery.Data.Split(','); //оценка, chatId, жанр

                    int grade = int.Parse(callBackDataArray[0]);
                    chatId = long.Parse(callBackDataArray[1]);
                    genre = callBackDataArray[2];

                    var currentUserPreference =
                        new UserPreference(chatId); //предполагаем что записи в файле с текущим chatID нет 

                    string data = ReadingDataFromFile(fileName);

                    listUsersPreference = (data != string.Empty)
                        ? JsonConvert.DeserializeObject<List<UserPreference>>(data)
                        : new List<UserPreference>(); //достаем предпочтения всех пользователей с файла

                    if (listUsersPreference.Count != 0 && listUsersPreference.Exists(x => x.ChatId == chatId))
                    {
                        currentUserPreference =
                            listUsersPreference.Where(x => x.ChatId == chatId)
                                .First(); //запись с текущим chatId уже есть
                        listUsersPreference.Remove(
                            currentUserPreference); //удаляем текущий элемент из листа, в дальше сменим ему оценку и снова запишем
                    }

                    //получаем значение из словаря по ключу название категории
                    if (currentUserPreference.CategoriesGrades.ContainsKey(genre)) //если значение по ключу существует
                    {
                        currentUserPreference.CategoriesGrades[genre] += grade;
                    }
                    else
                    {
                        currentUserPreference.CategoriesGrades.Add(genre, grade);
                    }

                    listUsersPreference.Add(currentUserPreference);

                    string json = JsonConvert.SerializeObject(listUsersPreference);
                    System.IO.File.WriteAllText(fileName, json);

                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: update.CallbackQuery.Id,
                        text: $"Дякую! Я врахую твій відгук під час наступних рекоменадацій😊",
                        showAlert: true,
                        cancellationToken: cancellationToken);
                }
        }

        public static string ReadingDataFromFile(string fileName)
        {
            string data = String.Empty;
            if (!System.IO.File.Exists(fileName)) //если файл не существует, то создаем его, в файл будем записывать chat id и оценки жанров
            {
                System.IO.File.WriteAllText(fileName, string.Empty);
            }
            else
            {
                data = System.IO.File.ReadAllText(fileName);//получаем информацию из файла
            }
            return data;
        }

        //получение размера баннера
        public static long GetBannerSize(Movie movie)
        {
            string filePath = @"banner.jpg";//место куда будем сохранять картинку

            //загрузка баннера по url в папку с программой
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(movie.banner, @"banner.jpg");
            }
            long sizeInBytes = new FileInfo(filePath).Length; //get file size in bytes
            long sizeInMbytes = sizeInBytes / (1024 *1024);//get file size in Mbytes
            File.Delete(filePath);//удаляем файл из папкм
            return sizeInMbytes;
        }
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            return Task.CompletedTask;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions();
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            //Hiding Program Icon from Taskbar
            /*--------------------------------------*/
            [DllImport("kernel32.dll")]
            static extern IntPtr GetConsoleWindow();

            [DllImport("user32.dll")]
            static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            const int SW_HIDE = 0;
            //const int SW_SHOW = 5;

            var handle = GetConsoleWindow();

            // Hide
            ShowWindow(handle, SW_HIDE);
            /*--------------------------------------*/
            Console.ReadLine();
        }
    }
}